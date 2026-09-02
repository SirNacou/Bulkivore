// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using Bulkivore.Api.Domain.Ingestion;
using MiniExcelLibs;

namespace Bulkivore.Api.Infrastructure.Adapters;

public class MiniExcelHeaderReader : IStreamingHeaderReader
{
    public async Task<IReadOnlyList<string>> ExtractHeadersAsync(Stream fileStream, string fileExtension,
        CancellationToken ct = default)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{fileExtension}");
        try
        {
            await using (var output = File.Create(tempFile))
            {
                await fileStream.CopyToAsync(output, ct);
            }

            var columns = await MiniExcel.GetColumnsAsync(tempFile, true, cancellationToken: ct);
            return columns.Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
