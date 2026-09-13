using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Endpoints.Imports.ListImports;

public class ListImportsEndpoint(AppDbContext dbContext)
    : Ep.Req<ListImportsRequest>.Res<ListImportsResponse>
{
    public override void Configure()
    {
        Get("");
        Group<ImportsGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListImportsRequest req, CancellationToken ct)
    {
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var page = Math.Max(1, req.Page);

        var query = dbContext.ImportSessions.AsNoTracking();
        if (req.Status.HasValue)
            query = query.Where(x => x.Status == req.Status.Value);

        var totalCount = await query.CountAsync(ct);

        var sessions = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        await Send.OkAsync(
            new ListImportsResponse(
                page,
                pageSize,
                totalCount,
                [.. sessions.Select(ImportSummaryDto.FromSession)]
            ),
            ct
        );
    }
}
