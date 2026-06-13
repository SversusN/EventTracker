using Testcontainers.PostgreSql;

namespace EventTrackerApi.IntegrationTests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = null!;

    public string GetConnectionString() => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder()
            .WithDatabase("eventtracker_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
        await Container.DisposeAsync();
    }
}
