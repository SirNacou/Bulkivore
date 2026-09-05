using Bulkivore.Api.Domain.Ingestion;
using Vogen;

namespace Bulkivore.Api.Infrastructure.Persistence.Configurations;

[EfCoreConverter<ImportSessionId>]
[EfCoreConverter<SourceFile>]
internal partial class VogenEfCoreConverters;
