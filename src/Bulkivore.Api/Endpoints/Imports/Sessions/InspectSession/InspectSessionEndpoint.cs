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
        var session = await dbContext.ImportSessions.FirstOrDefaultAsync(x => x.Id == req.SessionId, ct);
        if (session == null) return Error.NotFound();

        if (!await fileStorage.ExistsAsync(session.StorageKey, ct))
        {
            return Error.NotFound(
                description: "Uploaded file was not found in storage. Please upload the file before inspecting."
            );
        }

        session.ConfirmUpload();

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

        var targetColumns = (await schemaInspector.InspectTableAsync(session.TargetTable, ct: ct))
            .Values.ToList();
        var suggestedMappings = matcher.AutoMatch(headers, targetColumns);

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
