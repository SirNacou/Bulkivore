// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Bulkivore.Api.Domain.Ingestion.Ports;

public interface IFileStorage
{
    string GenerateUploadUrl(string storageKey, TimeSpan expiresIn);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
