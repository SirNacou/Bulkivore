using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Endpoints.Imports.GetImportSession;

public class GetImportSessionEndpoint(AppDbContext dbContext)
    : Ep.Req<GetImportSessionRequest>.Res<GetImportSessionResponse>
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Get("{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetImportSessionRequest req, CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session is null)
            await Send.NotFoundAsync(ct);
        else
            await Send.OkAsync(new GetImportSessionResponse(session), ct);
    }
}
