using Aspire.Hosting;
using Bulkivore.AppHost;
using Microsoft.Extensions.Hosting;

#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("env");

var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "sirnacou/bulkivore");

// 1. Storage & Bucket Initialization
var (storage, storageInit) = builder.AddLocalS3Storage("ministack", bucketName: "bulkivore-imports");

// 2. Database & Migrations
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithDbx(b => b.WithContainerName("dbx").WithHostPort(port: 4444));

var db = postgres.AddDatabase("bulkivore-db");
var testDb = postgres.AddDatabase("bulkivore-test-db");

IResourceBuilder<IResource> migrationRunner;
if (builder.Environment.IsDevelopment() || builder.ExecutionContext.IsRunMode)
{
    migrationRunner = builder
        .AddProject<Projects.Bulkivore_MigrationService>("migration-service")
        .WithReference(db)
        .WaitFor(db);
}
else
{
    migrationRunner = builder
        .AddDockerfile(
            name: "migration-bundle",
            contextPath: Path.Combine("..", ".."),
            dockerfilePath: "src/Bulkivore.MigrationService/Dockerfile")
        .WithReference(db)
        .WithArgs("--connection", db.Resource.ConnectionStringExpression)
        .WaitFor(db);
}

// 3. API Application
var api = builder
    .AddProject<Projects.Bulkivore_Api>("api")
    .AsGhcrService(registry, name: "api", tag: "latest")
    .WithReference(db)
    .WithReference(testDb)
    .WithEnvironment("TEST_DB_CONN", testDb.Resource.UriExpression)
    .WithS3Storage(storage, storageInit, bucketName: "bulkivore-imports")
    .WaitForCompletion(migrationRunner)
    .WithHttpHealthCheck("/health");

IResourceBuilder<IResource> frontend;

if (builder.Environment.IsDevelopment() && builder.ExecutionContext.IsRunMode)
{
    // Fast local development: Run Nuxt dev server via Bun with hot reload (HMR)
    frontend = builder.AddJavaScriptApp("frontend", "../frontend")
        .WithBun()
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints()
        .WithEnvironment("NUXT_PUBLIC_API_BASE", api.GetEndpoint("http"))
        .WaitFor(api);
}
else
{
    // Production / Publish / Docker Compose: Package into the Nginx container
    frontend = builder.AddDockerfile("frontend", "../frontend")
        .WithHttpEndpoint(targetPort: 80, name: "http")
        .WithExternalHttpEndpoints()
        .WithEnvironment("API_URL", api.GetEndpoint("http"))
        .WaitFor(api);
}

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
