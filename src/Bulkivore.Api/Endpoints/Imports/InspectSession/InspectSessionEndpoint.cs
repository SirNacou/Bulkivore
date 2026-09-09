using Bulkivore.Api.Domain.Common.Resilience;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Endpoints.Common;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public class InspectSessionEndpoint(
    AppDbContext dbContext,
    IFileStorage fileStorage,
    ISchemaInspector schemaInspector,
    IColumnMatcher matcher,
    IRetryService retryService,
    ILogger<InspectSessionEndpoint> logger
)
    : Ep.Req<InspectSessionRequest>.Res<InspectSessionResponse>
{
    public override void Configure()
    {
        Get("{Id}/inspect");
        Group<ImportsGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        InspectSessionRequest req,
        CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session == null)
        {
            logger.LogWarning("Import session not found: {SessionId}", req.Id);
            await Send.NotFoundAsync(ct);
            return;
        }

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
            ct);

        if (!exists)
        {
            logger.LogWarning("File not found in storage for import session: {SessionId}", session.Id);
            await Send.NotFoundAsync(ct);
            return;
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

            var rows = MiniExcel.QueryAsync(tempFilePath, useHeaderRow: true, cancellationToken: ct)
                .Cast<IDictionary<string, object>>();

            await foreach (var dict in rows)
            {
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
        {
            logger.LogError(
                "Error occurred while inspecting table schema for session {SessionId}: {Errors}",
                session.Id,
                errorOrTableSchema.Errors);
            await Send.ErrorOrResultAsync(errorOrTableSchema.Errors, ct);
            return;
        }

        var tableSchema = errorOrTableSchema.Value;

        var targetColumns = tableSchema.Columns.ToList();
        var suggestedMappings = matcher.Match(headers, targetColumns);

        await Send.OkAsync(
            new InspectSessionResponse(
                session.Id,
                session.Status,
                headers,
                suggestedMappings,
                targetColumns,
                previewRows),
            ct);
    }
}
