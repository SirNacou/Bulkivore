using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports;

public sealed class ImportsGroup : Group
{
    public ImportsGroup()
    {
        Configure("imports", ep =>
            ep.Description(x => x.Produces(401).WithTags("Imports"))
        );
    }
}
