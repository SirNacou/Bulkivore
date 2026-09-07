using System.Diagnostics.CodeAnalysis;
using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Domain.Schema;

public sealed record TableSchema
{
    private readonly Dictionary<string, ColumnMetadata> _lookup;

    public string TableName { get; }
    public string SchemaName { get; }
    public IReadOnlyList<ColumnMetadata> Columns { get; }

    private TableSchema(string tableName, string schemaName, IEnumerable<ColumnMetadata> columns)
    {
        TableName = tableName;
        SchemaName = schemaName;
        Columns = columns.ToList().AsReadOnly();
        _lookup = Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static ErrorOr<TableSchema> Create(string tableName, string schemaName, IEnumerable<ColumnMetadata> columns)
    {
        List<Error> errors = [];
        if (string.IsNullOrWhiteSpace(tableName))
            errors.Add(Error.Validation(description: "Table name cannot be empty."));

        if (string.IsNullOrWhiteSpace(schemaName))
            errors.Add(Error.Validation(description: "Schema name cannot be empty."));

        if (errors.Count > 0)
            return errors;

        return new TableSchema(tableName.Trim(), schemaName.Trim(), columns);
    }

    public bool ContainsColumn(string columnName) => _lookup.ContainsKey(columnName);

    public bool TryGetColumn(string columnName, [NotNullWhen(true)] out ColumnMetadata? metadata) =>
        _lookup.TryGetValue(columnName, out metadata);

    public ColumnMetadata this[string columnName] => _lookup[columnName];

    // Uses domain rules defined on ColumnMetadata
    public IEnumerable<ColumnMetadata> WritableColumns => Columns.Where(c => c.IsWritable);

    public IEnumerable<ColumnMetadata> RequiredColumns => Columns.Where(c => c.IsRequired);

    public (bool IsValid, List<string> Errors) ValidateMappings(IEnumerable<ColumnMapping> mappings)
    {
        var errors = new List<string>();
        var mappedTargets = mappings
            .Select(m => m.TargetColumn)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1. Check existence and write permissions
        foreach (var target in mappedTargets)
        {
            if (!_lookup.TryGetValue(target, out var col))
            {
                errors.Add($"Target column '{target}' does not exist on table '{TableName}'.");
            }
            else if (!col.IsWritable)
            {
                errors.Add($"Column '{target}' is read-only or computed and cannot be mapped.");
            }
        }

        // 2. Ensure all mandatory fields are mapped
        foreach (var required in RequiredColumns)
        {
            if (!mappedTargets.Contains(required.Name))
            {
                errors.Add($"Required column '{required.Name}' is missing from the column mappings.");
            }
        }

        return (errors.Count == 0, errors);
    }
}
