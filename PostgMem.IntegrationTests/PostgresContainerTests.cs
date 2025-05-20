using Npgsql;

namespace PostgMem.IntegrationTests;

/// <summary>
/// Validates that the schema exists can be accessed
/// </summary>
[Collection(nameof(PostgresIntegrationSpecs))]
public class PostgresContainerTests
{
    private readonly PostgresFixture _fixture;
    public PostgresContainerTests(PostgresFixture fixture) => _fixture = fixture;

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