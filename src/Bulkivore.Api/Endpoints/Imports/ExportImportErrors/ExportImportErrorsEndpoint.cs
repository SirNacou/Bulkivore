using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace Bulkivore.Api.Endpoints.Imports.ExportImportErrors;

public class ExportImportErrorsEndpoint(AppDbContext dbContext) : Ep.Req<ExportImportErrorsRequest>.NoRes
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Get("{Id}/errors/export");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ExportImportErrorsRequest req, CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (session.RowErrors.Count == 0)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var exportData = session.RowErrors.Select(e => new
            {
                Row = e.RowNumber,
                e.Column,
                InvalidValue = e.RawValue ?? "(null)",
                e.Reason
            }
        );

        MemoryStream memoryStream = new();
        await memoryStream.SaveAsAsync(exportData, cancellationToken: ct);
        memoryStream.Position = 0;

        var contentType = session.File.Format switch
        {
            FileFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv"
        };

        var fileName = $"import-{session.Id:N}-errors{session.File.Extension}";

        await Send.StreamAsync(
            stream: memoryStream,
            fileName: fileName,
            fileLengthBytes: memoryStream.Length,
            contentType: contentType,
            cancellation: ct
        );
    }
}
