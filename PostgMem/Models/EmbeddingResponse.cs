using System.Text.Json.Serialization;

namespace PostgMem.Models;

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; init; } = [];
}
