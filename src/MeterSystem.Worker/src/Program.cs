using MeterSystem.Shared.Configuration;
using MeterSystem.Worker.Repository;
using MeterSystem.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<PostgresOptions>(
    builder.Configuration.GetSection("Postgres"));

builder.Services.AddSingleton<IReadingsRepository, PostgresReadingsRepository>();

builder.Services.AddHostedService<RabbitMqReadingConsumer>();
var host = builder.Build();
host.Run();
