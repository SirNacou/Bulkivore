using TUnit.Aspire;

namespace Bulkivore.IntegrationTests;

public class AppFixture : AspireFixture<Projects.Bulkivore_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(3);
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override IEnumerable<string> ResourcesToWaitFor() => ["api", "bulkivore-db", "ministack"];

    protected override IEnumerable<string> ResourcesToRemove() => ["dbx", "ghcr"];

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        base.ConfigureBuilder(builder);

        builder.Services.ConfigureHttpClientDefaults(client =>
            {
                client.AddStandardResilienceHandler();
            }
        );
    }
}
