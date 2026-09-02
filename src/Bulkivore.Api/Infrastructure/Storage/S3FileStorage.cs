using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Bulkivore.Api.Infrastructure.Storage;

public class S3FileStorage() : IFileStorage
{
    public Task<string> GenerateUploadUrlAsync(string storageKey, TimeSpan expiresIn, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
