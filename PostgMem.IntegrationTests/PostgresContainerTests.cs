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
}