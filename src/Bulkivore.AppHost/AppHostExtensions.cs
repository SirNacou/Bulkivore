using System.Diagnostics.CodeAnalysis;

namespace Bulkivore.AppHost;

public static class AppHostExtensions
{
    /// <summary>
    /// Deploys a local S3 container (Ministack) and creates the default target bucket via AWS-CLI.
    /// </summary>
    public static (IResourceBuilder<ContainerResource> Storage, IResourceBuilder<ContainerResource> Init)
        AddLocalS3Storage(this IDistributedApplicationBuilder builder, string name, string bucketName)
    {
        var storage = builder
            .AddContainer(name, "ministackorg/ministack")
            .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "s3")
            .WithEnvironment("SERVICES", "s3")
            .WithEnvironment("MINISTACK_REGION", "us-east-1")
            .WithEnvironment("PERSIST_STATE", "1")
            .WithVolume("ministack-s3-data", "/tmp/ministack-data/s3");

        var storageInit = builder
            .AddContainer($"{name}-init", "amazon/aws-cli")
            .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
            .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
            .WithArgs($"--endpoint-url=http://{name}:4566", "s3", "mb", $"s3://{bucketName}")
            .WaitFor(storage);

        return (storage, storageInit);
    }

    /// <summary>
    /// Injects standard S3 configuration keys and ensures the service waits for the bucket initialization container.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithS3Storage(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ContainerResource> storage,
        IResourceBuilder<ContainerResource> storageInit,
        string bucketName)
    {
        var s3Endpoint = storage.GetEndpoint("s3");

        return project
            .WithReference(s3Endpoint)
            .WaitFor(storage)
            .WaitForCompletion(storageInit) // Prevents starting before bucket exists
            .WithEnvironment("Storage__BucketName", bucketName)
            .WithEnvironment("Storage__AccessKey", "test")
            .WithEnvironment("Storage__SecretKey", "test")
            .WithEnvironment("Storage__ForcePathStyle", "true")
            .WithEnvironment("Storage__Region", "us-east-1")
            .WithEnvironment("Storage__ServiceUrl", s3Endpoint);
    }

    /// <summary>
    /// Bundles Docker Compose and Container Registry deployment settings.
    /// </summary>
    [Experimental("ASPIRECOMPUTE003")]
    public static IResourceBuilder<ProjectResource> AsGhcrService(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ContainerRegistryResource> registry,
        string name,
        string tag = "latest")
    {
        return project
            .PublishAsDockerComposeService((_, service) => service.Name = name)
            .WithContainerRegistry(registry)
            .WithRemoteImageName(name)
            .WithRemoteImageTag(tag);
    }
}
