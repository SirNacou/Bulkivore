// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Ingestion.Ports;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Ingestion.InspectHeaders;

public class InspectHeadersEndpoint(IStreamingHeaderReader headerReader)
    : Ep.Req<InspectHeadersRequest>.Res<InspectHeadersResponse>
{
    public override void Configure()
    {
        Group<IngestionGroup>();
        Post("inspect-headers");
        AllowAnonymous();
        AllowFileUploads();
    }

    public override async Task HandleAsync(InspectHeadersRequest req, CancellationToken ct)
    {
        await using var stream = req.File.OpenReadStream();
        var headers = await headerReader.ExtractHeadersAsync(stream, Path.GetExtension(req.File.FileName), ct);

        await Send.OkAsync(new InspectHeadersResponse(headers, headers.Count), ct);
    }
}
