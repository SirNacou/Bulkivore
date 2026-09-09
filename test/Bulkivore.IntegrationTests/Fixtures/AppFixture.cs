using TUnit.Aspire;

namespace Bulkivore.IntegrationTests.Fixtures;

public class AppFixture : AspireFixture<Projects.Bulkivore_AppHost>
{
    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(3);
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override IEnumerable<string> ResourcesToWaitFor() =>
    [
        "api", "bulkivore-db", "bulkivore-test-db", "ministack"
    ];

    protected override IEnumerable<string> ResourcesToRemove() => ["ghcr"];
}
