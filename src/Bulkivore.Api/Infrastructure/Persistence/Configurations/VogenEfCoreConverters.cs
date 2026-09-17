using Bulkivore.Api.Domain.Imports;
using Vogen;

namespace Bulkivore.Api.Infrastructure.Persistence.Configurations;

[EfCoreConverter<ImportSessionId>]
[EfCoreConverter<SourceFile>]
internal partial class VogenEfCoreConverters;
