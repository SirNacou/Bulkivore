using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Bulkivore.Api.Infrastructure.Storage;

public class S3FileStorage(IAmazonS3 s3Client, IOptions<StorageOptions> storageOptions) : IFileStorage
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;

    public string GenerateUploadUrl(string storageKey, TimeSpan expiresIn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _storageOptions.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.PUT,
        };

        return s3Client.GetPreSignedURL(request);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(_storageOptions.BucketName, storageKey, ct);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var response = await s3Client.GetObjectAsync(_storageOptions.BucketName, storageKey, ct);
        return response.ResponseStream;
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        return s3Client.DeleteObjectAsync(_storageOptions.BucketName, storageKey, ct);
    }
}
