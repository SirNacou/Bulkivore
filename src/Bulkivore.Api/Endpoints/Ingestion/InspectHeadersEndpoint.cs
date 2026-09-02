// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Bulkivore.Api.Domain.Ingestion;
using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Ingestion;

public class InspectHeadersEndpoint(IStreamingHeaderReader headerReader)
    : Ep.Req<InspectHeadersRequest>.Res<InspectHeadersResponse>
{
    public override void Configure()
    {
        Post("/ingestion/inspect-headers");
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

public sealed class InspectHeadersRequest
{
    public IFormFile File { get; set; } = null!;
}

public sealed record InspectHeadersResponse(IReadOnlyList<string> Headers, int TotalColumns);

public sealed class InspectHeadersValidator : Validator<InspectHeadersRequest>
{
    private const int MaxFileSize = 100 * 1024 * 1024;
    private static readonly string[] s_allowedExtensions = [".csv", ".xlsx"];

    public InspectHeadersValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.")
            .Must(file => file.Length <= MaxFileSize)
            .WithMessage($"File size cannot exceed {MaxFileSize / (1024 * 1024)} MB.")
            .Must(BeValidExtension)
            .WithMessage($"File extension must be one of the following: {string.Join(", ", s_allowedExtensions)}.");
    }

    private bool BeValidExtension(IFormFile file)
    {
        return s_allowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase);
    }
}
