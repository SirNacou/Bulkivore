using Bulkivore.Api.Domain.Schema;
using Dunet;

namespace Bulkivore.Api.Domain.Ingestion;

[Union]
public partial record ParsedCell
{
    public partial record ParsedSuccess(object Value, ColumnDataType DataType);

    public partial record ParsedNull;

    public partial record ParsedError(string Message);
}
