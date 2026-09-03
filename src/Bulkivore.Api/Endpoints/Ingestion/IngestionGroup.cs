using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Ingestion;

public sealed class IngestionGroup : Group
{
    public IngestionGroup()
    {
        Configure("ingestion", ep =>
        {
            ep.Description(x => x.Produces(401).WithTags("Ingestion"));
        });
    }
}
