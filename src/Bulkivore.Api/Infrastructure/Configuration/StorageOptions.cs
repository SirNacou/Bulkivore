using System.ComponentModel.DataAnnotations;

namespace Bulkivore.Api.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(ErrorMessage = "Storage:BucketName is required.")]
    public string BucketName { get; init; } = string.Empty;

    public string? ServiceUrl { get; init; }
    public string Region { get; init; } = "us-east-1";
    public bool ForcePathStyle { get; init; } = false;

    [Required(ErrorMessage = "Storage:AccessKey is required.")]
    public string AccessKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "Storage:SecretKey is required.")]
    public string SecretKey { get; init; } = string.Empty;
}
