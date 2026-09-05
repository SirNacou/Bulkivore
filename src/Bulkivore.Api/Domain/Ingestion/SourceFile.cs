using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

public enum FileFormat { Csv, Xlsx }

[ValueObject<string>]
public readonly partial struct SourceFile
{
    public string Name => Value;
    public string Extension => Path.GetExtension(Value).ToLowerInvariant();

    public FileFormat Format =>
        Extension switch
        {
            ".csv" => FileFormat.Csv,
            ".xlsx" => FileFormat.Xlsx,
            _ => throw new InvalidOperationException("Unsupported file format.")
        };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".csv", ".xlsx" };

    private static Validation Validate(string rawFileName)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return Validation
                .Invalid("File name cannot be empty.")
                .WithData(
                    nameof(rawFileName),
                    rawFileName
                );
        }

        var extension = Path.GetExtension(rawFileName).ToLowerInvariant();
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

    private static string NormalizeInput(string input)
    {
        return input.Trim();
    }
}
