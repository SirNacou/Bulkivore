using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

[ValueObject<(string, string)>]
public readonly partial record struct SourceFileName
{
    public static ErrorOr<SourceFileName> Create(string fileName)
    {
        var trimmed = fileName.Trim();
        var extension = Path.GetExtension(trimmed).ToLowerInvariant();

        var res = TryFrom((trimmed, extension));
        if (res.IsSuccess) return res.ValueObject;
        return Error.Validation(res.Error.ErrorMessage);
    }

    public string Name => Value.Item1;
    public string Extension => Value.Item2;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".csv", ".xlsx" };

    private static Validation Validate((string, string) input)
    {
        var (rawFileName, extension) = input;
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return Validation
                .Invalid("File name cannot be empty.")
                .WithData(
                    nameof(rawFileName),
                    rawFileName
                );
        }

        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return Validation
                .Invalid($"Unsupported extension '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}.")
                .WithData(
                    nameof(extension),
                    extension
                );
        }

        return Validation.Ok;
    }

    public static implicit operator string(SourceFileName sourceFileName) => sourceFileName.Name;
}
