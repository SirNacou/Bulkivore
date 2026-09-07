using Bulkivore.Api.Domain.Common.Resilience;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Infrastructure.Persistence;
using Bulkivore.Api.Infrastructure.Resilience;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using MiniExcel = MiniExcelLibs.MiniExcel;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public class InspectSessionEndpoint(
    AppDbContext dbContext,
    IFileStorage fileStorage,
    ISchemaInspector schemaInspector,
    IColumnMatcher matcher,
    IRetryService retryService
)
    : Ep.Req<InspectSessionRequest>.Res<ErrorOr<InspectSessionResponse>>
{
    public override void Configure()
    {
        Get("{SessionId}/inspect");
        Group<ImportsGroup>();
        AllowAnonymous();
    }

    public override async Task<ErrorOr<InspectSessionResponse>> ExecuteAsync(
        InspectSessionRequest req,
        CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.SessionId, ct);
        if (session == null)
            return Error.NotFound();

        var retryOptions = new RetryOptions()
        {
            MaxAttempts = 3,
            InitialDelay = TimeSpan.FromMilliseconds(400),
            MaxDelay = TimeSpan.FromMilliseconds(1500),
        };

        var exists = await retryService.ExecuteUntilAsync(
            token => fileStorage.ExistsAsync(session.StorageKey, token),
            result => !result,
            retryOptions,
            ct
        );

        if (!exists)
        {
            return Error.NotFound(
                description: "Uploaded file was not found in storage. Please upload the file before inspecting."
            );
        }

        List<string> headers = [];
        List<Dictionary<string, object>> previewRows = [];

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{session.File.Extension}");

        try
        {
            await using (var s3Stream = await fileStorage.OpenReadAsync(session.StorageKey, ct))
            {
                await using var fileStream = File.Create(tempFilePath);
                await s3Stream.CopyToAsync(fileStream, ct);
            }

            var rows = MiniExcel.QueryAsync(tempFilePath, useHeaderRow: true, cancellationToken: ct);

            await foreach (var rawRow in rows)
            {
                if (rawRow is not IDictionary<string, object> dict)
                    continue;

                if (headers.Count == 0)
                {
                    headers.AddRange(dict.Keys.Where(k => !string.IsNullOrWhiteSpace(k)));
                }

                previewRows.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));

                if (previewRows.Count >= 20)
                    break;
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        var errorOrTableSchema = await schemaInspector.InspectTableAsync(session.TargetTable, ct: ct);
        if (errorOrTableSchema.IsError)
            return errorOrTableSchema.Errors;
        var tableSchema = errorOrTableSchema.Value;

        var targetColumns = tableSchema.Columns.ToList();
        var suggestedMappings = matcher.Match(headers, targetColumns);

        return new InspectSessionResponse(
            session.Id,
            session.Status,
            headers,
            suggestedMappings,
            targetColumns,
            previewRows
        );
    }
}
