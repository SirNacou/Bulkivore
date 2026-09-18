using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Bulkivore.Api.Domain.Common.Resilience;
using Bulkivore.Api.Domain.Imports;
using Bulkivore.Api.Domain.Imports.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Infrastructure.Configuration;
using Bulkivore.Api.Infrastructure.Persistence;
using Bulkivore.Api.Infrastructure.Resilience;
using Bulkivore.Api.Infrastructure.Services;
using Bulkivore.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bulkivore.Api.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// The full production composition: persistence + storage + reconciliation.
        /// </summary>
        public IServiceCollection AddInfrastructure()
        {
            services.AddPersistence();
            services.AddStorage();
            services.AddReconciliation();

            return services;
        }

        /// <summary>
        /// EF Core: pooled DbContext for scoped consumers plus IDbContextFactory
        /// for singleton consumers (e.g. the reconciliation job).
        /// </summary>
        public IServiceCollection AddPersistence()
        {
            services.AddPooledDbContextFactory<AppDbContext>((sp, b) =>
            {
                var conn = sp.GetRequiredService<IConfiguration>().GetConnectionString("bulkivore-db");
                b.UseNpgsql(conn)
                    .UseBulkivoreDbDefaults();
            });

            return services;
        }

        /// <summary>
        /// AWS S3 client and storage/schema adapters.
        /// </summary>
        public IServiceCollection AddStorage()
        {
            // Storage Options & Validation
            services
                .AddOptions<StorageOptions>()
                .BindConfiguration(StorageOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
                var s3Config = new AmazonS3Config
                {
                    ForcePathStyle = options.ForcePathStyle
                };

                if (string.IsNullOrWhiteSpace(options.ServiceUrl))
                {
                    s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
                }
                else
                {
                    s3Config.ServiceURL = options.ServiceUrl;
                    s3Config.AuthenticationRegion = options.Region;
                    if (options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        s3Config.UseHttp = true;
                    }
                }

                var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                return new AmazonS3Client(credentials, s3Config);
            });

            services.AddSingleton<IFileStorage, S3FileStorage>();
            services.AddSingleton<ISchemaInspector, PostgresSchemaInspector>();
            services.AddSingleton<IColumnMatcher, FuzzyColumnMatcher>();
            services.AddSingleton<IRetryService, RetryService>();

            return services;
        }

        /// <summary>
        /// Stale import session reconciliation: the injectable service plus its
        /// periodic hosted job. Split out so tests can register the reconciler
        /// without starting the background sweep.
        /// </summary>
        public IServiceCollection AddReconciliation()
        {
            services
                .AddOptions<ImportReconciliationOptions>()
                .BindConfiguration(ImportReconciliationOptions.SectionName)
                .ValidateOnStart();
            services.AddSingleton<ImportSessionReconciler>();
            services.AddHostedService<ImportSessionReconcilerJob>();

            return services;
        }
    }
}
