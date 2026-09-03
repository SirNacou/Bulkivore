namespace Bulkivore.Api.Endpoints.Common;

public static class ValidationExtensions
{
    private static readonly string[] AllowedSpreadsheetExtensions = [".csv", ".xlsx"];

    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilder<T, string> AllowedExtensions(params string[] allowedExtensions)
        {
            return ruleBuilder.Must(fileName =>
            {
                var extension = Path.GetExtension(fileName);
                return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            }).WithMessage($"File extension must be one of the following: {string.Join(", ", allowedExtensions)}.");
        }

        public IRuleBuilder<T, string> MustBeCsvOrXlsx()
        {
            return ruleBuilder
                .AllowedExtensions(AllowedSpreadsheetExtensions);
        }
    }
}
