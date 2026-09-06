using Bulkivore.Api.Infrastructure.Persistence;
using Bulkivore.MigrationService;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("bulkivore-db");

builder.Services.AddHostedService<Worker>();
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(typeof(Worker).Namespace!));

var host = builder.Build();
host.Run();
