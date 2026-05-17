# Meter Readings Backend

## Overview

This project implements an asynchronous meter-readings ingestion pipeline:

**API → RabbitMQ → Worker → PostgreSQL**

A client submits readings to an ASP.NET Core API, which validates the request and publishes a message to RabbitMQ. A background worker consumes those messages, upserts the meter record, and persists readings in PostgreSQL. Duplicate readings for the same meter and timestamp are ignored at the database layer.

| Component | Role |
|-----------|------|
| **MeterSystem.Api** | HTTP ingestion, Swagger, optional protobuf decoding |
| **RabbitMQ** | Decouples ingestion from persistence |
| **MeterSystem.Worker** | Consumes queue messages and writes to PostgreSQL |
| **PostgreSQL** | Stores meters and readings with uniqueness constraints |
| **Kubernetes / Minikube** | Local deployment of all services |

Data access uses **Dapper** with raw SQL (no ORM). **Idempotent inserts** rely on a composite primary key on `(meter_id, value_at)` and `ON CONFLICT DO NOTHING`.

## Project Structure

```
├── database/          # schema.sql and PostgreSQL Kubernetes manifest
├── queue/             # RabbitMQ Kubernetes manifest
├── deploy.sh          # End-to-end Minikube deployment script
└── src/
    ├── MeterSystem.Api/      # ASP.NET Core Web API
    ├── MeterSystem.Worker/   # Background worker service
    └── MeterSystem.Shared/   # Shared models and configuration
```

### MeterSystem.Api

ASP.NET Core Web API exposed outside the cluster (NodePort service). Accepts JSON and optional Base64-encoded protobuf payloads, publishes `ReadingMessage` JSON to the `meter-readings` queue, and returns `202 Accepted` on success.

### MeterSystem.Worker

ASP.NET Core generic host with a `BackgroundService` that consumes from RabbitMQ and persists readings via `PostgresReadingsRepository`. It has no HTTP endpoints.

### MeterSystem.Shared

Shared request/queue models (`ReadingRequest`, `RawReadingRequest`, `ReadingMessage`) and configuration types (`RabbitMqOptions`, `PostgresOptions`) used by both the API and worker.

## Technologies Used

- .NET 10
- ASP.NET Core
- RabbitMQ (`RabbitMQ.Client`)
- PostgreSQL 18
- Dapper
- Kubernetes / Minikube
- Docker (container images via `dotnet publish -t:PublishContainer`)
- Protocol Buffers (optional raw endpoint; `meter_data.proto`)

## API Endpoints

### POST /api/readings

Accepts a batch of readings for one meter. Valid requests return **202 Accepted** and the payload is published to RabbitMQ.

**Example request**

```json
{
  "meter_number": 12345,
  "readings": {
    "2026-03-18T10:15:00Z": 1234.56,
    "2026-03-18T10:00:00Z": 1234.51
  }
}
```

Readings are keyed by ISO 8601 timestamps. They may arrive out of order; ordering is not enforced at the API.

### POST /api/readings/raw

Optional endpoint that accepts a Base64-encoded protobuf `MeterData` message. Returns **202 Accepted** when the payload parses successfully, otherwise **400 Bad Request**.

**Example request**

```json
{
  "meter_number": 12345,
  "data": "ChEKBgik9unNBhEK16NwPUqTQAoRCgYIoO/pzQYR16NwPQpKk0A="
}
```

The protobuf schema is defined in `src/MeterSystem.Api/Protos/meter_data.proto`. After decoding, the API publishes the same `ReadingMessage` format as the JSON endpoint.

Swagger UI is enabled in all environments at `/swagger`.

## Idempotency

Deduplication is based on `(meter_id, value_at)`:

1. **Database constraint** — `meter_readings` has `PRIMARY KEY (meter_id, value_at)` in `database/schema.sql`.
2. **`ON CONFLICT DO NOTHING`** — the worker inserts readings with:

   ```sql
   INSERT INTO meter_readings (meter_id, value_at, value)
   VALUES (@MeterId, @ValueAt, @Value)
   ON CONFLICT (meter_id, value_at) DO NOTHING;
   ```

The first reading for a given meter and timestamp is kept; later duplicates are silently ignored. Meters are upserted with `ON CONFLICT (meter_number) DO UPDATE` so a meter row exists before readings are inserted.

## Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- [Minikube](https://minikube.sigs.k8s.io/docs/start/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- Bash (for `deploy.sh`; use Git Bash or WSL on Windows)

Start Minikube and ensure `kubectl` points at the Minikube context:

```bash
minikube start
kubectl config use-context minikube
```

### Deploy

From the repository root:

```bash
./deploy.sh
```

The script:

1. Pulls `postgres:18` and `rabbitmq:3-management` into Minikube
2. Deploys RabbitMQ and PostgreSQL
3. Applies `database/schema.sql`
4. Builds and loads API and worker container images
5. Deploys API and worker manifests

### Access Swagger

```bash
minikube service metersystem-api --url
```

Open `{url}/swagger` in a browser (the API listens on port 8080 inside the cluster).

## Verifying Persistence

Forward PostgreSQL to your machine:

```bash
kubectl port-forward svc/postgres 5432:5432
```

Connect with:

```
Host=localhost;Port=5432;Database=meters;Username=postgres;Password=postgres
```

Example queries:

```sql
SELECT * FROM meters;
SELECT * FROM meter_readings ORDER BY value_at;
```

## Tradeoffs / Simplifications

Given the ~4 hour assignment timebox, several production concerns were intentionally deferred:

- **Minimal Kubernetes** — single-replica deployments, no Helm, no resource limits or network policies
- **No persistent volumes** — PostgreSQL and RabbitMQ data are ephemeral inside the cluster
- **No retry or dead-letter queues** — failed messages are logged; the consumer uses `autoAck: true`
- **Non-durable queue** — `durable: false` on queue declaration
- **Simplified validation** — the JSON readings endpoint does not perform extensive request validation; the raw endpoint validates meter number, Base64, and protobuf structure
- **Protobuf as optional enhancement** — implemented after the core JSON flow

## Future Improvements

- Retry policies and dead-letter queues for poison messages
- Kubernetes liveness/readiness probes on API and worker
- Metrics and structured logging (OpenTelemetry, Prometheus)
- Durable queues and persistent messages
- Authentication and authorization on API endpoints
- Automated integration tests (API, worker, database)
- Helm charts for parameterized deployments

## Notes

- **Configuration** is supplied via Kubernetes environment variables (e.g. `RabbitMQ__HostName`, `Postgres__ConnectionString`), not `appsettings.json`, in deployed environments.
- **Worker has no HTTP surface** — it only runs as an internal background processor consuming RabbitMQ.
- **Queue name** — both API and worker use `meter-readings` (see deploy manifests and `RabbitMqOptions`).
