using System.Text.Json;
using PostgMem.Services;

namespace PostgMem.IntegrationTests.Mocks;

public class MockEmbeddingService : IEmbeddingService
{
    private readonly Dictionary<string, float[]> _embeddings = new();
    private int _dimension = 384;

    public Task<float[]> Generate(string text, CancellationToken cancellationToken = default)
    {
        if (_embeddings.TryGetValue(text, out var embedding))
        {
            return Task.FromResult(embedding);
        }

        // Generate a deterministic embedding based on the text
        var hash = text.GetHashCode();
        var random = new Random(hash);
        var newEmbedding = new float[_dimension];
        
        for (int i = 0; i < _dimension; i++)
        {
            newEmbedding[i] = (float)random.NextDouble();
        }

        // Normalize the embedding
        float sum = 0;
        for (int i = 0; i < _dimension; i++)
        {
            sum += newEmbedding[i] * newEmbedding[i];
        }

        float magnitude = (float)Math.Sqrt(sum);
        for (int i = 0; i < _dimension; i++)
        {
            newEmbedding[i] /= magnitude;
        }

        _embeddings[text] = newEmbedding;
        return Task.FromResult(newEmbedding);
    }

    public Task<float[]> Generate(JsonDocument document, CancellationToken cancellationToken = default)
    {
        string jsonString = document.RootElement.ToString();
        return Generate(jsonString, cancellationToken);
    }
} 