using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

public sealed record ColumnMapping(
    string SourceHeader,
    string TargetColumn
)
{
    public static ErrorOr<ColumnMapping> Create(string sourceHeader, string targetColumn)
    {
        List<Error> errors = [];
        if (string.IsNullOrWhiteSpace(sourceHeader))
            errors.Add(Error.Validation(description: "Source header cannot be empty."));

        if (string.IsNullOrWhiteSpace(targetColumn))
            errors.Add(Error.Validation(description: "Target column cannot be empty."));

        if (errors.Count > 0)
            return errors;

        return new ColumnMapping(sourceHeader.Trim(), targetColumn.Trim());
    }

    public void Deconstruct(out string sourceHeader, out string targetColumnName)
    {
        sourceHeader = SourceHeader;
        targetColumnName = TargetColumn;
    }
}
