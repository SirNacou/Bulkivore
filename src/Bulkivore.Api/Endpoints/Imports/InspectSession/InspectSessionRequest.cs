using Bulkivore.Api.Domain.Ingestion;

namespace Bulkivore.Api.Endpoints.Imports.InspectSession;

public sealed record InspectSessionRequest(ImportSessionId SessionId);
