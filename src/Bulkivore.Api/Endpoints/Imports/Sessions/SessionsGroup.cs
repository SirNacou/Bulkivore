using FastEndpoints;

namespace Bulkivore.Api.Endpoints.Imports.Sessions;

public class SessionsGroup : SubGroup<ImportsGroup>
{
    public SessionsGroup()
    {
        Configure("sessions", ep =>
            ep.Description(x => x.Produces(401).WithTags("Sessions"))
        );
    }
}
