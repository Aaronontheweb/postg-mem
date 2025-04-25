using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using PostgMem.Services;
using Memory = PostgMem.Models.Memory;

namespace PostgMem.Tools;

[McpServerToolType]
public class MemoryTools
{
    private readonly IStorage _storage;

    public MemoryTools(IStorage storage)
    {
        _storage = storage;
    }

    [McpServerTool, Description("Store a new memory in the database")]
    public async Task<string> Store(
        [Description("The type of memory (e.g., 'conversation', 'document', etc.)")] string type,
        [Description("The content of the memory as a JSON object")] string content,
        [Description("The source of the memory (e.g., 'user', 'system', etc.)")] string source,
        [Description("Optional tags to categorize the memory")] string[]? tags = null,
        [Description("Confidence score for the memory (0.0 to 1.0)")] double confidence = 1.0,
        CancellationToken cancellationToken = default
    )
    {

        // Store the memory
        Memory memory = await _storage.StoreMemory(
            type,
            content,
            source,
            tags,
            confidence,
            cancellationToken
        );

        return $"Memory stored successfully with ID: {memory.Id}";
    }

    [McpServerTool, Description("Search for memories similar to the provided text")]
    public async Task<string> Search(
        [Description("The text to search for similar memories")] string query,
        [Description("Maximum number of results to return")] int limit = 10,
        [Description("Minimum similarity threshold (0.0 to 1.0)")] double minSimilarity = 0.7,
        [Description("Optional tags to filter memories")] string[]? filterTags = null,
        CancellationToken cancellationToken = default
    )
    {

        // Search for similar memories
        List<Memory> memories = await _storage.Search(
            query,
            limit,
            minSimilarity,
            filterTags,
            cancellationToken
        );

        if (memories.Count == 0)
        {
            return "No memories found matching your query.";
        }

        // Format the results
        StringBuilder result = new();
        result.AppendLine($"Found {memories.Count} memories:");
        result.AppendLine();

        foreach (Memory? memory in memories)
        {
            result.AppendLine($"ID: {memory.Id}");
            result.AppendLine($"Type: {memory.Type}");
            result.AppendLine($"Content: {memory.Content.RootElement}");
            result.AppendLine($"Source: {memory.Source}");
            result.AppendLine(
                $"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}"
            );
            result.AppendLine($"Confidence: {memory.Confidence:F2}");
            result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
        }

        return result.ToString();
    }

    [McpServerTool, Description("Retrieve a specific memory by ID")]
    public async Task<string> Get(
        [Description("The ID of the memory to retrieve")] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        Memory? memory = await _storage.Get(id, cancellationToken);

        if (memory == null)
        {
            return $"Memory with ID {id} not found.";
        }

        StringBuilder result = new();
        result.AppendLine($"ID: {memory.Id}");
        result.AppendLine($"Type: {memory.Type}");
        result.AppendLine($"Content: {memory.Content.RootElement}");
        result.AppendLine($"Source: {memory.Source}");
        result.AppendLine(
            $"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}"
        );
        result.AppendLine($"Confidence: {memory.Confidence:F2}");
        result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        result.AppendLine($"Updated: {memory.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        return result.ToString();
    }

    [McpServerTool, Description("Delete a memory by ID")]
    public async Task<string> Delete(
        [Description("The ID of the memory to delete")] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        bool success = await _storage.Delete(id, cancellationToken);

        return success ? $"Memory with ID {id} deleted successfully." : $"Memory with ID {id} not found or could not be deleted.";
    }
}
