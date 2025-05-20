using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var ollama =
    builder.AddOllama("ollama")
        .WithDataVolume()
        .WithOpenWebUI();

/*
 * The set of available models from Ollama: https://ollama.com/library
 */
// Store the model name in a variable for reuse
var embeddingsModelName = "all-minilm:33m-l12-v2-fp16";
var embeddings = ollama
    .AddModel("embeddings", embeddingsModelName); // fast embeddings model

var postgres = builder.AddPostgres("postgres")
    // install pgvector so we can get vector / embedding support
    // https://github.com/pgvector/pgvector?tab=readme-ov-file#docker
    .WithImage("pgvector/pgvector", "pg17")
    // Add init script to ensure database creation
    .WithEnvironment("POSTGRES_DB", "postgmem");
    //.WithDataVolume();

// This is now redundant but keeping for clarity in connection string generation
var memDb = postgres.AddDatabase("postgmem");

// adminer interface for postgres
var adminer = postgres.WithPgAdmin();

var memServer = builder.AddProject<Projects.PostgMem>("postg-mem")
    .WithReference(memDb, "Storage")
    .WithReference(embeddings)
    .WithEnvironment("Embeddings__ApiUrl", ollama.GetEndpoint("http"))
    .WithEnvironment("Embeddings__Model", embeddingsModelName);

builder.Build().Run();