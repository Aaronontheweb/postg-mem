using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using PostgMem.Services;
using Memory = PostgMem.Models.Memory;
using PostgMem.Models;

namespace PostgMem.Tools;

[McpServerToolType]
public class MemoryTools
{
    private readonly IStorage _storage;

    public MemoryTools(IStorage storage)
    {
        _storage = storage;
    }

    [McpServerTool, Description("Store a new memory in the database, optionally creating a relationship to another memory. Use this to save reference material, how-to guides, coding standards, or any information you (the LLM) may want to refer to when completing tasks. Include as much context as possible, such as markdown, code samples, and detailed explanations. Create relationships to link related reference materials or examples.")]
    public async Task<string> Store(
        [Description("The type of memory (e.g., 'conversation', 'document', 'reference', 'how-to', etc.). Use 'reference' or 'how-to' for reusable knowledge.")] string type,
        [Description("The content of the memory as a JSON object. Include rich context, markdown, code samples, and detailed explanations whenever possible.")] string content,
        [Description("The source of the memory (e.g., 'user', 'system', 'LLM', etc.). Use 'LLM' if you are storing knowledge for your own future use.")] string source,
        [Description("Optional tags to categorize the memory. Use tags like 'coding-standard', 'unit-test', 'reference', 'how-to', etc. to make retrieval easier.")] string[]? tags = null,
        [Description("Confidence score for the memory (0.0 to 1.0)")] double confidence = 1.0,
        [Description("Optionally, the ID of a related memory. Use this to link related reference materials, how-tos, or examples.")] Guid? relatedTo = null,
        [Description("Optionally, the type of relationship to create (e.g., 'example-of', 'explains', 'related-to'). Use relationships to connect related knowledge.")] string? relationshipType = null,
        CancellationToken cancellationToken = default
    )
    {
        // Store the memory (and optionally create a relationship)
        Memory memory = await _storage.StoreMemory(
            type,
            content,
            source,
            tags,
            confidence,
            relatedTo,
            relationshipType,
            cancellationToken
        );
        return $"Memory stored successfully with ID: {memory.Id}";
    }

    [McpServerTool, Description("Search for memories similar to the provided text. Use this to retrieve reference material, how-tos, or examples relevant to the current task. Filtering by tags can help narrow down to specific types of knowledge.")]
    public async Task<string> Search(
        [Description("The text to search for similar memories. Use natural language queries to find relevant reference or how-to information.")] string query,
        [Description("Maximum number of results to return")] int limit = 10,
        [Description("Minimum similarity threshold (0.0 to 1.0)")] double minSimilarity = 0.7,
        [Description("Optional tags to filter memories (e.g., 'reference', 'how-to', 'coding-standard')")] string[]? filterTags = null,
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
        }

        return result.ToString();
    }

    [McpServerTool, Description("Retrieve a specific memory by ID. Use this to fetch a particular reference, how-to, or example by its unique identifier.")]
    public async Task<string> Get(
        [Description("The ID of the memory to retrieve. Use this to fetch a specific piece of reference or how-to information.")] Guid id,
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

    [McpServerTool, Description("Delete a memory by ID. Use this to remove outdated or incorrect reference or how-to information.")]
    public async Task<string> Delete(
        [Description("The ID of the memory to delete. Use this to remove a specific piece of knowledge.")] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        bool success = await _storage.Delete(id, cancellationToken);

        return success ? $"Memory with ID {id} deleted successfully." : $"Memory with ID {id} not found or could not be deleted.";
    }

    [McpServerTool, Description("Fetch multiple memories by their IDs. Use this to retrieve a set of related reference materials, how-tos, or examples.")]
    public async Task<string> GetMany(
        [Description("The list of memory IDs to fetch. Use this to retrieve multiple related pieces of knowledge at once.")] Guid[] ids,
        CancellationToken cancellationToken = default
    )
    {
        var memories = await _storage.GetMany(ids, cancellationToken);
        if (memories.Count == 0)
            return "No memories found for the provided IDs.";
        StringBuilder result = new();
        result.AppendLine($"Found {memories.Count} memories:");
        result.AppendLine();
        foreach (var memory in memories)
        {
            result.AppendLine($"ID: {memory.Id}");
            result.AppendLine($"Type: {memory.Type}");
            result.AppendLine($"Content: {memory.Content.RootElement}");
            result.AppendLine($"Source: {memory.Source}");
            result.AppendLine($"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}");
            result.AppendLine($"Confidence: {memory.Confidence:F2}");
            result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
        }
        return result.ToString();
    }

    [McpServerTool, Description("Create a relationship between two memories. Use this to link related reference materials, how-tos, or examples (e.g., 'example-of', 'explains', 'related-to'). Relationships help organize knowledge for easier retrieval and understanding.")]
    public async Task<string> CreateRelationship(
        [Description("The ID of the source memory (e.g., the reference or how-to that is providing context)")] Guid fromId,
        [Description("The ID of the target memory (e.g., the example or related reference)")] Guid toId,
        [Description("The type of relationship (e.g., 'example-of', 'explains', 'related-to'). Use relationships to connect and organize knowledge.")] string type,
        CancellationToken cancellationToken = default
    )
    {
        var rel = await _storage.CreateRelationship(fromId, toId, RelationshipTypeHelper.FromDbString(type), cancellationToken);
        return $"Relationship created: {rel.Id} from {rel.FromMemoryId} to {rel.ToMemoryId} (type: {rel.Type})";
    }
}
