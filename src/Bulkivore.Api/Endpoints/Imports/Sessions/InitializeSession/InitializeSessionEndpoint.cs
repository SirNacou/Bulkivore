using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Endpoints.Common;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports.Sessions.InitializeSession;

public class InitializeSessionEndpoint(IFileStorage fileStorage)
    : Ep.Req<InitializeSessionRequest>.Res<InitializeSessionResponse>
{
    public override void Configure()
    {
        Group<SessionsGroup>();
        Post("");
        AllowAnonymous();
    }

    public override Task HandleAsync(InitializeSessionRequest req, CancellationToken ct)
    {
        var extension = Path.GetExtension(req.FileName);
        var storageKey = $"uploads/{Guid.NewGuid()}{extension}";
        var expiresIn = TimeSpan.FromMinutes(15);

        var uploadUrl = fileStorage.GenerateUploadUrl(storageKey, expiresIn);

        var session = ImportSession.Initialize(
            req.TenantId,
            req.TargetTable,
            req.FileName,
            storageKey
        );

        return base.HandleAsync(req, ct);
    }
}

public class InitializeSessionValidator : Validator<InitializeSessionRequest>
{
    public InitializeSessionValidator()
    {
        RuleFor(x => x.TargetTable).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MustBeCsvOrXlsx();
    }
}
