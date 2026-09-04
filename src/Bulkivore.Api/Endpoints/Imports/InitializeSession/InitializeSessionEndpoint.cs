using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Endpoints.Common;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports.InitializeSession;

public class InitializeSessionEndpoint(IFileStorage fileStorage, BulkivoreDbContext dbContext)
    : Ep.Req<InitializeSessionRequest>.Res<ErrorOr<InitializeSessionResponse>>
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Post("");
        AllowAnonymous();
    }

    public override async Task<ErrorOr<InitializeSessionResponse>> ExecuteAsync(
        InitializeSessionRequest req,
        CancellationToken ct)
    {
        var extension = Path.GetExtension(req.FileName);
        var storageKey = $"uploads/{Guid.NewGuid()}{extension}";
        var expiresIn = TimeSpan.FromMinutes(15);

        var uploadUrl = fileStorage.GenerateUploadUrl(storageKey, expiresIn);

        var errorOrImportSession = ImportSession.Initialize(
            req.TenantId,
            req.TargetTable,
            req.FileName,
            storageKey
        );
        if (errorOrImportSession.IsError) return errorOrImportSession.Errors;
        var importSession = errorOrImportSession.Value;

        dbContext.ImportSessions.Add(importSession);
        await dbContext.SaveChangesAsync(ct);

        return new InitializeSessionResponse(
            importSession.Id.Value,
            uploadUrl,
            storageKey,
            DateTimeOffset.UtcNow.Add(expiresIn)
        );
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
