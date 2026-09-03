using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Ingestion.InspectHeaders;

public sealed class InspectHeadersValidator : Validator<InspectHeadersRequest>
{
    private const int MaxFileSize = 100 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".csv", ".xlsx"];

    public InspectHeadersValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.")
            .Must(file => file.Length <= MaxFileSize)
            .WithMessage($"File size cannot exceed {MaxFileSize / (1024 * 1024)} MB.")
            .Must(BeValidExtension)
            .WithMessage($"File extension must be one of the following: {string.Join(", ", AllowedExtensions)}.");
    }

    private bool BeValidExtension(IFormFile file)
    {
        return AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase);
    }
}
