using Akka.Hosting;
using Akka.Hosting.TestKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PostgMem.Extensions;
using PostgMem.Services;
using PostgMem.Settings;
using Xunit.Abstractions;

namespace PostgMem.IntegrationTests;

/// <summary>
/// Integration tests for PostgMem using TestContainers
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class IntegrationTests : TestKit
{
    private readonly IntegrationTestFixture _fixture;
    private IStorage _storage = null!;
    private IEmbeddingService _embeddingService = null!;

    public IntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output) 
        : base(output: output)
    {
        _fixture = fixture;
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var apiUrl = new Uri(_fixture.OllamaApiUrl);

        // Add in-memory configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Storage"] = _fixture.PostgresConnectionString,
                ["Embeddings:ApiUrl"] = apiUrl.ToString(),
                ["Embeddings:Model"] = "all-minilm",
                ["Embeddings:Timeout"] = TimeSpan.FromMinutes(1).ToString()
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(config);

        // Configure HttpClient without logging middleware
        services.AddHttpClient<IEmbeddingService, EmbeddingService>(client =>
        {
            client.BaseAddress = apiUrl;
            client.Timeout = TimeSpan.FromMinutes(1);
        });

        // Add other services
        services.AddSingleton(new EmbeddingSettings
        {
            ApiUrl = apiUrl,
            Model = "all-minilm",
            Timeout = TimeSpan.FromMinutes(1)
        });
        services.AddPostgMem();
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.ConfigureLoggers(configBuilder =>
        {
            configBuilder.LogLevel = Akka.Event.LogLevel.DebugLevel;
        });
    }

    [Fact]
    public async Task CanConnectAndUsePgvector()
    {
        await using var conn = new NpgsqlConnection(_fixture.PostgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        // Create a vector with 384 dimensions (all zeros)
        var vector = string.Join(",", Enumerable.Repeat("0", 384));
        cmd.CommandText = $"INSERT INTO memories (id, type, content, source, embedding, tags, confidence, created_at, updated_at) VALUES (gen_random_uuid(), 'test', '{{}}'::jsonb, 'test', '[{vector}]'::vector, ARRAY['tag'], 1.0, now(), now()) RETURNING id;";
        var id = await cmd.ExecuteScalarAsync();
        Assert.NotNull(id);
    }

    [Fact]
    public async Task Should_Store_And_Retrieve_Memory()
    {
        // Get services from DI
        _storage = Host.Services.GetRequiredService<IStorage>();
        _embeddingService = Host.Services.GetRequiredService<IEmbeddingService>();

        // Test storing a memory
        var memory = await _storage.StoreMemory(
            "test",
            "{\"content\": \"test content\"}",
            "test",
            new[] { "test" },
            1.0,
            null,
            null,
            CancellationToken.None);

        // Test retrieving the memory
        var retrieved = await _storage.Get(memory.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(memory.Id, retrieved.Id);
    }

    [Fact]
    public async Task CanSearchMemories()
    {
        // Get services from DI
        _storage = Host.Services.GetRequiredService<IStorage>();
        _embeddingService = Host.Services.GetRequiredService<IEmbeddingService>();

        // Arrange
        var memories = new[]
        {
            ("memory1", "{\"fact\": \"The sky is blue\"}", new[] { "nature" }),
            ("memory2", "{\"fact\": \"Grass is green\"}", new[] { "nature" }),
            ("memory3", "{\"fact\": \"The sun is hot\"}", new[] { "nature", "space" })
        };

        foreach (var (type, content, tags) in memories)
        {
            await _storage.StoreMemory(type, content, "test", tags, 1.0);
        }

        // Act
        var results = await _storage.Search(
            "What color is the sky?",
            limit: 1,
            minSimilarity: 0.5,
            filterTags: new[] { "nature" }
        );

        // Assert
        Assert.Single(results);
        Assert.Contains("sky", results[0].Content.RootElement.GetProperty("fact").GetString());
    }

    [Fact]
    public async Task CanDeleteMemory()
    {
        // Get services from DI
        _storage = Host.Services.GetRequiredService<IStorage>();
        _embeddingService = Host.Services.GetRequiredService<IEmbeddingService>();

        // Arrange
        var memory = await _storage.StoreMemory(
            "test",
            "{\"fact\": \"This will be deleted\"}",
            "test",
            new[] { "temporary" },
            1.0
        );

        // Act
        var deleteResult = await _storage.Delete(memory.Id);
        var retrievedMemory = await _storage.Get(memory.Id);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(retrievedMemory);
    }

    [Fact]
    public async Task SchemaVersionTable_IsPopulated_AfterMigration()
    {
        await using var conn = new NpgsqlConnection(_fixture.PostgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT version, name, applied_at FROM schema_version ORDER BY version";
        await using var reader = await cmd.ExecuteReaderAsync();
        var found = false;
        var versions = new List<int>();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            found = true;
            var version = reader.GetInt32(0);
            var name = reader.GetString(1);
            var appliedAt = reader.GetDateTime(2);
            Assert.True(version > 0, $"Migration version should be positive, got {version}");
            Assert.False(string.IsNullOrWhiteSpace(name), "Migration name should not be empty");
            Assert.True(appliedAt <= DateTime.UtcNow);
            versions.Add(version);
            names.Add(name);
        }
        Assert.True(found, "No migrations found in schema_version table");
        // Optionally: check that version numbers are unique and increasing
        Assert.Equal(versions.OrderBy(v => v), versions);
        Assert.Equal(names.Distinct().Count(), names.Count);
    }

    [Fact]
    public async Task CanGetManyMemories()
    {
        _storage = Host.Services.GetRequiredService<IStorage>();
        // Store multiple memories
        var m1 = await _storage.StoreMemory("type1", "{\"fact\": \"A\"}", "src1", new[] { "tag1" }, 1.0);
        var m2 = await _storage.StoreMemory("type2", "{\"fact\": \"B\"}", "src2", new[] { "tag2" }, 1.0);
        var m3 = await _storage.StoreMemory("type3", "{\"fact\": \"C\"}", "src3", new[] { "tag3" }, 1.0);
        // Fetch by ids
        var results = await _storage.GetMany(new[] { m1.Id, m3.Id }, CancellationToken.None);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, m => m.Id == m1.Id);
        Assert.Contains(results, m => m.Id == m3.Id);
    }

    [Fact]
    public async Task CanCreateAndGetMemoryRelationships()
    {
        _storage = Host.Services.GetRequiredService<IStorage>();
        // Store two memories
        var m1 = await _storage.StoreMemory("type1", "{\"fact\": \"Parent\"}", "src1", new[] { "tag1" }, 1.0);
        var m2 = await _storage.StoreMemory("type2", "{\"fact\": \"Child\"}", "src2", new[] { "tag2" }, 1.0);
        // Create relationship
        var rel = await _storage.CreateRelationship(m1.Id, m2.Id, "parent", CancellationToken.None);
        Assert.Equal(m1.Id, rel.FromMemoryId);
        Assert.Equal(m2.Id, rel.ToMemoryId);
        Assert.Equal("parent", rel.Type);
        // Get relationships
        var rels = await _storage.GetRelationships(m1.Id, "parent", CancellationToken.None);
        Assert.Single(rels);
        Assert.Equal(rel.Id, rels[0].Id);
        Assert.Equal(m2.Id, rels[0].ToMemoryId);
    }

    [Fact]
    public async Task CanStoreMemoryWithRelationship()
    {
        _storage = Host.Services.GetRequiredService<IStorage>();
        // Store a related memory first
        var related = await _storage.StoreMemory("typeX", "{\"fact\": \"Related\"}", "srcX", new[] { "tagX" }, 1.0);
        // Store a new memory and create a relationship in one call
        var memory = await _storage.StoreMemory("typeY", "{\"fact\": \"Main\"}", "srcY", new[] { "tagY" }, 1.0, related.Id, "reference");
        // Check the memory exists
        var retrieved = await _storage.Get(memory.Id);
        Assert.NotNull(retrieved);
        // Check the relationship exists
        var rels = await _storage.GetRelationships(memory.Id, "reference");
        Assert.Single(rels);
        Assert.Equal(memory.Id, rels[0].FromMemoryId);
        Assert.Equal(related.Id, rels[0].ToMemoryId);
        Assert.Equal("reference", rels[0].Type);
    }
}