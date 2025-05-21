using System.Text.Json;
using Npgsql;
using Pgvector;
using PostgMem.Models;
using Registrator.Net;

namespace PostgMem.Services;

public interface IStorage
{
    Task<Memory> StoreMemory(
        string type,
        string content,
        string source,
        string[]? tags,
        double confidence,
        Guid? relatedTo = null,
        string? relationshipType = null,
        CancellationToken cancellationToken = default,
        string? title = null
    );

    Task<List<Memory>> Search(
        string query,
        int limit = 10,
        double minSimilarity = 0.7,
        string[]? filterTags = null,
        CancellationToken cancellationToken = default
    );

    Task<Memory?> Get(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<bool> Delete(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<List<Memory>> GetMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<MemoryRelationship> CreateRelationship(Guid fromId, Guid toId, string type, CancellationToken cancellationToken = default);
    Task<List<MemoryRelationship>> GetRelationships(Guid memoryId, string? type = null, CancellationToken cancellationToken = default);
}

[AutoRegisterInterfaces(ServiceLifetime.Singleton)]
public class Storage : IStorage
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IEmbeddingService _embeddingService;

    public Storage(NpgsqlDataSource dataSource, IEmbeddingService embeddingService)
    {
        _dataSource = dataSource;
        _embeddingService = embeddingService;
    }

    public async Task<Memory> StoreMemory(
        string type,
        string content,
        string source,
        string[]? tags,
        double confidence,
        Guid? relatedTo = null,
        string? relationshipType = null,
        CancellationToken cancellationToken = default,
        string? title = null
    )
    {
        JsonDocument document = JsonDocument.Parse(content);

        // Extract text for embedding
        string textToEmbed = content; // Default to original content string
        if (document.RootElement.TryGetProperty("fact", out var factElement) && factElement.ValueKind == JsonValueKind.String)
        {
            textToEmbed = factElement.GetString() ?? content;
        }
        else if (document.RootElement.TryGetProperty("observation", out var observationElement) && observationElement.ValueKind == JsonValueKind.String)
        {
            textToEmbed = observationElement.GetString() ?? content;
        }
        else if (document.RootElement.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
        {
            textToEmbed = textElement.GetString() ?? content;
        }
        else if (document.RootElement.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
        {
            textToEmbed = contentElement.GetString() ?? content;
        }

        // Combine title and content for embedding if title is present
        if (!string.IsNullOrWhiteSpace(title))
        {
            textToEmbed = title + " " + textToEmbed;
        }

        float[] embedding = await _embeddingService.Generate(
            textToEmbed, // Use the combined title + content for embedding
            cancellationToken
        );
        
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        Memory memory = new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = document,
            Source = source,
            Embedding = new Vector(embedding),
            Tags = tags,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Title = title // Set the title
        };

        const string sql =
            @"
            INSERT INTO memories (id, type, content, source, embedding, tags, confidence, created_at, updated_at, title)
            VALUES (@id, @type, @content, @source, @embedding, @tags, @confidence, @createdAt, @updatedAt, @title)";

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", memory.Id);
        cmd.Parameters.AddWithValue("type", memory.Type);
        cmd.Parameters.AddWithValue("content", memory.Content);
        cmd.Parameters.AddWithValue("source", memory.Source);
        cmd.Parameters.AddWithValue("embedding", memory.Embedding);
        cmd.Parameters.AddWithValue("tags", memory.Tags ?? []);
        cmd.Parameters.AddWithValue("confidence", memory.Confidence);
        cmd.Parameters.AddWithValue("createdAt", memory.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", memory.UpdatedAt);
        cmd.Parameters.AddWithValue("title", (object?)memory.Title ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Optionally create a relationship
        if (relatedTo.HasValue && !string.IsNullOrWhiteSpace(relationshipType))
        {
            await CreateRelationship(memory.Id, relatedTo.Value, relationshipType, cancellationToken);
        }

        return memory;
    }

    public async Task<List<Memory>> Search(
        string query,
        int limit = 10,
        double minSimilarity = 0.7,
        string[]? filterTags = null,
        CancellationToken cancellationToken = default
    )
    {
        // Generate embedding for the query
        float[] queryEmbedding = await _embeddingService.Generate(
            query,
            cancellationToken
        );
        
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        string sql =
            @"
            SELECT id, type, content, source, embedding, tags, confidence, created_at, updated_at, title
            FROM memories
            WHERE embedding <=> @embedding < @maxDistance";

        if (filterTags is { Length: > 0 })
        {
            sql += " AND tags @> @tags";
        }

        sql += " ORDER BY embedding <=> @embedding LIMIT @limit";

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("embedding", new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue("maxDistance", 1 - minSimilarity); 
        cmd.Parameters.AddWithValue("limit", limit);

        if (filterTags != null && filterTags.Length > 0)
        {
            cmd.Parameters.AddWithValue("tags", filterTags);
        }

        List<Memory> memories = [];
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            memories.Add(
                new Memory
                {
                    Id = reader.GetGuid(0),
                    Type = reader.GetString(1),
                    Content = reader.GetFieldValue<JsonDocument>(2),
                    Source = reader.GetString(3),
                    Embedding = reader.GetFieldValue<Vector>(4),
                    Tags = reader.GetFieldValue<string[]>(5),
                    Confidence = reader.GetDouble(6),
                    CreatedAt = reader.GetDateTime(7),
                    UpdatedAt = reader.GetDateTime(8),
                    Title = reader.IsDBNull(9) ? null : reader.GetString(9)
                }
            );
        }

        return memories;
    }

    public async Task<Memory?> Get(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql =
            @"
            SELECT id, type, content, source, embedding, tags, confidence, created_at, updated_at, title
            FROM memories
            WHERE id = @id";

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", id);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new Memory
            {
                Id = reader.GetGuid(0),
                Type = reader.GetString(1),
                Content = reader.GetFieldValue<JsonDocument>(2),
                Source = reader.GetString(3),
                Embedding = reader.GetFieldValue<Vector>(4),
                Tags = reader.GetFieldValue<string[]>(5),
                Confidence = reader.GetDouble(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8),
                Title = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
        }

        return null;
    }

    public async Task<bool> Delete(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = "DELETE FROM memories WHERE id = @id";

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", id);

        int rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<List<Memory>> GetMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, type, content, source, embedding, tags, confidence, created_at, updated_at, title
            FROM memories
            WHERE id = ANY(@ids)";
        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        List<Memory> memories = [];
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            memories.Add(
                new Memory
                {
                    Id = reader.GetGuid(0),
                    Type = reader.GetString(1),
                    Content = reader.GetFieldValue<JsonDocument>(2),
                    Source = reader.GetString(3),
                    Embedding = reader.GetFieldValue<Vector>(4),
                    Tags = reader.GetFieldValue<string[]>(5),
                    Confidence = reader.GetDouble(6),
                    CreatedAt = reader.GetDateTime(7),
                    UpdatedAt = reader.GetDateTime(8),
                    Title = reader.IsDBNull(9) ? null : reader.GetString(9)
                }
            );
        }
        return memories;
    }

    public async Task<MemoryRelationship> CreateRelationship(Guid fromId, Guid toId, string type, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO memory_relationships (id, from_memory_id, to_memory_id, type, created_at)
            VALUES (@id, @from, @to, @type, @createdAt)";
        var rel = new MemoryRelationship
        {
            Id = Guid.NewGuid(),
            FromMemoryId = fromId,
            ToMemoryId = toId,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", rel.Id);
        cmd.Parameters.AddWithValue("from", rel.FromMemoryId);
        cmd.Parameters.AddWithValue("to", rel.ToMemoryId);
        cmd.Parameters.AddWithValue("type", type);
        cmd.Parameters.AddWithValue("createdAt", rel.CreatedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rel;
    }

    public async Task<List<MemoryRelationship>> GetRelationships(Guid memoryId, string? type = null, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        string sql = @"
            SELECT id, from_memory_id, to_memory_id, type, created_at
            FROM memory_relationships
            WHERE from_memory_id = @id";
        if (!string.IsNullOrEmpty(type))
            sql += " AND type = @type";
        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", memoryId);
        if (!string.IsNullOrEmpty(type))
            cmd.Parameters.AddWithValue("type", type);
        List<MemoryRelationship> rels = [];
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rels.Add(new MemoryRelationship
            {
                Id = reader.GetGuid(0),
                FromMemoryId = reader.GetGuid(1),
                ToMemoryId = reader.GetGuid(2),
                Type = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }
        return rels;
    }
}
