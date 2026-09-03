using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.Sessions.InspectSession;

public sealed record InspectSessionRequest(ImportSessionId SessionId);
