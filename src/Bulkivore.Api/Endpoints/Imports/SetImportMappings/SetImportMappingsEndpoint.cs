using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Endpoints.Imports.SetImportMappings;

public class SetImportMappingsEndpoint(BulkivoreDbContext dbContext, ISchemaInspector schemaInspector)
    : Ep.Req<SetImportMappingsRequest>.Res<ErrorOr<SetImportMappingsResponse>>
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Post("{Id}/mappings");
        AllowAnonymous();
    }

    public override async Task<ErrorOr<SetImportMappingsResponse>> ExecuteAsync(
        SetImportMappingsRequest req,
        CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session == null) return Error.NotFound();

        var tableSchema = await schemaInspector.InspectTableAsync(session.TargetTable, ct: ct);

        var invalidTargets = req.Mappings.Values.Except(tableSchema.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        if (invalidTargets.Count > 0)
            return Error.Validation(
                description:
                $"Target column(s) do not exist in table '{session.TargetTable}': {string.Join(", ", invalidTargets)}"
            );

        var unwritableTargets = req
            .Mappings.Values
            .Where(target => tableSchema.TryGetValue(target, out var column) && !column.IsWritable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unwritableTargets.Count > 0)
        {
            return Error.Validation(
                description: $"Cannot map to read-only or computed column(s): {string.Join(", ", unwritableTargets)}"
            );
        }

        var requiredColumns = tableSchema
            .Values
            .Where(c => c.IsRequired)
            .Select(c => c.Name)
            .ToList();

        var errorOrSuccess = session.ApplyMappings(req.Mappings, requiredColumns);
        if (errorOrSuccess.IsError) return errorOrSuccess.Errors;

        await dbContext.SaveChangesAsync(ct);

        return new SetImportMappingsResponse(
            session.Id,
            session.Status.ToString(),
            req.Mappings.Count
        );
    }
}
