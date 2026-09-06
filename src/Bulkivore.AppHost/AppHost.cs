using Bulkivore.AppHost;

#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("env");

var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "sirnacou/bulkivore");

// 1. Storage & Bucket Initialization
var (storage, storageInit) = builder.AddLocalS3Storage("ministack", bucketName: "bulkivore-imports");

// 2. Database & Migrations
var postgres = builder.AddPostgres("postgres").WithDataVolume().WithDbx();
var db = postgres.AddDatabase("bulkivore-db");
var testDb = postgres.AddDatabase("bulkivore-test-db");

var migrationService = builder
    .AddProject<Projects.Bulkivore_MigrationService>("migration-service")
    .WithReference(db)
    .WaitFor(db);

// 3. API Application
var api = builder
    .AddProject<Projects.Bulkivore_Api>("api")
    .AsGhcrService(registry, name: "api", tag: "latest")
    .WithReference(db)
    .WithReference(testDb)
    .WithEnvironment("TEST_DB_CONN", testDb.Resource.UriExpression)
    .WithS3Storage(storage, storageInit, bucketName: "bulkivore-imports")
    .WaitForCompletion(migrationService)
    .WithHttpHealthCheck("/health");

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
