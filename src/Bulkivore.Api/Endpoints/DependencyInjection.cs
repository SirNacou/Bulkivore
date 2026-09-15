using Bulkivore.Api.Endpoints.Common.Configuration;

namespace Bulkivore.Api.Endpoints;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCustomCors(
            IConfiguration configuration,
            string policyName)
        {
            // Bind the options class for runtime DI injection elsewhere if needed
            services.AddOptions<CorsSettings>(CorsSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>();

            return services.AddCors(options =>
            {
                options.AddPolicy(
                    name: policyName,
                    policy =>
                    {
                        if (corsSettings?.AllowedOrigins?.Length > 0)
                        {
                            policy.WithOrigins(corsSettings.AllowedOrigins)
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        }
                    });
            });
        }
    }
}
