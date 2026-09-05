using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Endpoints.Imports.GetImportErrors;

public class GetImportErrorsEndpoint(AppDbContext dbContext)
    : Ep.Req<GetImportErrorsRequest>.Res<GetImportErrorsResponse>
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Get("{Id}/errors");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetImportErrorsRequest req, CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var page = Math.Max(1, req.Page);

        var pagedErrors = session.RowErrors.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        await Send.OkAsync(
            new GetImportErrorsResponse(
                session.Id,
                session.FailedRowCount,
                page,
                pageSize,
                pagedErrors
            ),
            ct
        );
    }
}
