using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Bulkivore.Api.Domain.Common.Resilience;
using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Ingestion.Ports;
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
        public IServiceCollection AddInfrastructure()
        {
            // Storage Options & Validation
            services
                .AddOptions<StorageOptions>()
                .BindConfiguration(StorageOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // EFCore
            services.AddDbContextPool<AppDbContext>((sp, b) =>
                {
                    b.UseNpgsql("bulkivore-db")
                        .UseBulkivoreDbDefaults();
                }
            );

            // AWS / S3 Client
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
                    }

                    var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                    return new AmazonS3Client(credentials, s3Config);
                }
            );

            // Ports & Adapters
            services.AddSingleton<IFileStorage, S3FileStorage>();
            services.AddSingleton<ISchemaInspector, PostgresSchemaInspector>();
            services.AddSingleton<IStreamingHeaderReader, MiniExcelHeaderReader>();
            services.AddSingleton<IColumnMatcher, FuzzyColumnMatcher>();
            services.AddSingleton<IRetryService, RetryService>();

            return services;
        }
    }
}
