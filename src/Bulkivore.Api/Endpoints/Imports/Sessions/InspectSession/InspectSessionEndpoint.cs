using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Endpoints.Ingestion.Services;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using MiniExcel = MiniExcelLibs.MiniExcel;

namespace Bulkivore.Api.Endpoints.Imports.Sessions.InspectSession;

public class InspectSessionEndpoint(
    BulkivoreDbContext dbContext,
    IFileStorage fileStorage,
    ISchemaInspector schemaInspector,
    FuzzyColumnMatcher matcher
)
    : Ep.Req<InspectSessionRequest>.Res<ErrorOr<InspectSessionResponse>>
{
    public override void Configure()
    {
        Group<SessionsGroup>();
        Post("{SessionId}/inspect");
        AllowAnonymous();
    }

    public override async Task<ErrorOr<InspectSessionResponse>> ExecuteAsync(
        InspectSessionRequest req,
        CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.FirstOrDefaultAsync(
            x => x.Id == req.SessionId,
            cancellationToken: ct
        );
        if (session == null) return Error.NotFound();

        List<string> headers = [];
        List<Dictionary<string, object>> previewRows = [];

        await using (var stream = await fileStorage.OpenReadAsync(session.StorageKey, ct))
        {
            var rows = MiniExcel.QueryAsync(stream, useHeaderRow: true, cancellationToken: ct);

            await foreach (var rawRow in rows)
            {
                if (rawRow is not IDictionary<string, object> dict) continue;

                if (headers.Count == 0)
                {
                    headers.AddRange(dict.Keys.Where(k => !string.IsNullOrWhiteSpace(k)));
                }

                previewRows.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));

                if (previewRows.Count >= 20) break;
            }
        }

        var targetTableSchema = await schemaInspector.InspectTableAsync(session.TargetTable, ct: ct);
        var targetColumns = targetTableSchema.Values.ToList();
        var suggestedMappings = matcher.AutoMatch(headers, targetColumns);

        session.MarkUploaded();
        await dbContext.SaveChangesAsync(ct);

        return new InspectSessionResponse(
            session.Id,
            headers,
            suggestedMappings,
            targetColumns,
            previewRows
        );
    }
}
