using Vogen;

namespace Bulkivore.Api.Domain.Ingestion;

[ValueObject<ValueTuple<int, string, string, string>>]
public readonly partial record struct RowError
{
    public static RowError Create(int rowNumber, string column, string? rawValue, string reason) =>
        From((rowNumber, column, rawValue ?? "", reason));

    public int RowNumber => Value.Item1;
    public string Column => Value.Item2;
    public string? RawValue => string.IsNullOrEmpty(Value.Item3) ? null : Value.Item3;
    public string Reason => Value.Item4;
}
