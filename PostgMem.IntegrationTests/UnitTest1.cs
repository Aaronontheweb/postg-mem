using System;
using System.Threading.Tasks;
using Xunit;
using Testcontainers.PostgreSql;
using Npgsql;
using PostgMem.Services;

public class PostgresTestCollection : ICollectionFixture<PostgresTestFixture> { }

public class PostgresTestFixture : IAsyncLifetime
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

[Collection("PostgresTestCollection")]
public class PostgresContainerTests
{
    private readonly PostgresTestFixture _fixture;
    public PostgresContainerTests(PostgresTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanConnectAndUsePgvector()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO memories (id, type, content, source, embedding, tags, confidence, created_at, updated_at) VALUES (gen_random_uuid(), 'test', '{}'::jsonb, 'test', '[1,2,3]'::vector, ARRAY['tag'], 1.0, now(), now()) RETURNING id;";
        var id = await cmd.ExecuteScalarAsync();
        Assert.NotNull(id);
    }
}
