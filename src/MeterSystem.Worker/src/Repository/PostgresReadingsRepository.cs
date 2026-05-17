using Dapper;
using MeterSystem.Shared.Configuration;
using MeterSystem.Shared.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MeterSystem.Worker.Repository;

public class PostgresReadingsRepository: IReadingsRepository
{
    private readonly PostgresOptions _options;

    public PostgresReadingsRepository(IOptions<PostgresOptions> options)
    {
        _options = options.Value;
    }

    public async Task SaveAsync(ReadingMessage message, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var meterId = await connection.QuerySingleAsync<long>(
            """
            INSERT INTO meters (meter_number)
            VALUES (@MeterNumber)
            ON CONFLICT (meter_number) DO UPDATE
            SET meter_number = EXCLUDED.meter_number
            RETURNING meter_id;
            """,
            new { message.MeterNumber },
            transaction);

        foreach (var reading in message.Readings)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO meter_readings (meter_id, value_at, value)
                VALUES (@MeterId, @ValueAt, @Value)
                ON CONFLICT (meter_id, value_at) DO NOTHING;
                """,
                new
                {
                    MeterId = meterId,
                    ValueAt = reading.ValueAt,
                    reading.Value
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
