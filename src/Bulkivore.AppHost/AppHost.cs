#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003

var builder = DistributedApplication.CreateBuilder(args);
var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "sirnacou/bulkivore");

builder.AddDockerComposeEnvironment("env");

var storage = builder.AddContainer("ministack", "ministackorg/ministack")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "s3")
    .WithEnvironment("SERVICES", "s3")
    .WithEnvironment("MINISTACK_REGION", "us-east-1")
    .WithEnvironment("PERSIST_STATE ", "1")
    .WithVolume("ministack-s3-data", "/tmp/ministack-data/s3");

builder.AddContainer("storage-init", "amazon/aws-cli")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithArgs("--endpoint-url=http://ministack:4566", "s3", "mb", "s3://bulkivore-imports")
    .WaitFor(storage);

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
    .WithReference(storage.GetEndpoint("s3"))
    .WaitFor(db)
    .WaitFor(storage)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Storage__BucketName", "bulkivore-imports")
    .WithEnvironment("Storage__AccessKey", "test")
    .WithEnvironment("Storage__SecretKey", "test")
    .WithEnvironment("Storage__ForcePathStyle", "true")
    .WithEnvironment("Storage__Region", "us-east-1")
    .WithEnvironment(ctx =>
    {
        ctx.EnvironmentVariables["Storage__ServiceUrl"] = storage.GetEndpoint("s3");
        ctx.EnvironmentVariables["TEST_DB_CONN"] = testDb.Resource.UriExpression;
    });

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
