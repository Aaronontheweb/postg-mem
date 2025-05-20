using PostgMem.Services;
using Testcontainers.PostgreSql;

namespace PostgMem.IntegrationTests;

[CollectionDefinition(nameof(PostgresIntegrationSpecs))]
public class PostgresIntegrationSpecs : ICollectionFixture<PostgresFixture> { }
public class PostgresFixture : IAsyncLifetime
{
    public readonly PostgreSqlContainer Container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithCleanUp(true)
        .WithPortBinding(5432, true)
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithDatabase("testdb")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        await SchemaMigrator.MigrateAsync(ConnectionString);
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}