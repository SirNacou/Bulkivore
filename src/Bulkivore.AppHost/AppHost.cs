#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003


var builder = DistributedApplication.CreateBuilder(args);
var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "sirnacou/bulkivore");

builder.AddDockerComposeEnvironment("env");

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithDbx();
var db = postgres.AddDatabase("bulkivore-db");
var testDb = postgres.AddDatabase("bulkivore-test-db");

var api = builder.AddProject<Projects.Bulkivore_Api>("api")
    .PublishAsDockerComposeService((resource, service) => { service.Name = "api"; })
    .WithContainerRegistry(registry)
    .WithRemoteImageName("api")
    .WithRemoteImageTag("latest")
    .WithReference(db)
    .WithReference(testDb)
    .WaitFor(db)
    .WithHttpHealthCheck("/health")
    .WithEnvironment(ctx => { ctx.EnvironmentVariables["TEST_DB_CONN"] = testDb.Resource.UriExpression; });

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
