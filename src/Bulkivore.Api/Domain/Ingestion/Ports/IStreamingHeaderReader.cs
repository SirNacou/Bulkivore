// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Bulkivore.Api.Domain.Ingestion;

public interface IStreamingHeaderReader
{
    Task<IReadOnlyList<string>> ExtractHeadersAsync(
        Stream fileStream,
        string fileExtension,
        CancellationToken ct = default);
}
