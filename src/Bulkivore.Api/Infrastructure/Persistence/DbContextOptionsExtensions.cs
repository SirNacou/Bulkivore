using Microsoft.EntityFrameworkCore;

namespace Bulkivore.Api.Infrastructure.Persistence;

public static class DbContextOptionsExtensions
{
    extension(DbContextOptionsBuilder options)
    {
        public DbContextOptionsBuilder UseBulkivoreDbDefaults() => options.UseSnakeCaseNamingConvention();
    }
}
