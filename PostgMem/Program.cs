using Configuration.Extensions.EnvironmentFile;
using PostgMem.Extensions;
using PostgMem.Tools;
using Microsoft.Extensions.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .Configuration
    .AddEnvironmentFile() 
    .AddEnvironmentVariables("POSTGMEM_"); 

builder
    .Logging
    .AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; })
    .AddFile("log.log", minimumLevel: LogLevel.Trace);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("Storage") ?? 
    throw new InvalidOperationException("Missing Storage connection string");

// Add services
builder.Services.AddPostgMem();
builder.Services.AddMcpServer().WithTools<MemoryTools>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgres", "required"]);

WebApplication app = builder.Build();

// Run schema migration at startup
try
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    logger.LogInformation("Running schema migration at startup");
    
    PostgMem.Services.SchemaMigrator.MigrateAsync(connectionString).GetAwaiter().GetResult();
    
    logger.LogInformation("Schema migration completed successfully");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Schema migration failed: {ex.Message}");
    throw;
}

app.MapMcp();

// Add default system memory after schema migration
try
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    var storage = app.Services.GetRequiredService<PostgMem.Services.IStorage>();
    var embeddingService = app.Services.GetRequiredService<PostgMem.Services.IEmbeddingService>();
    
    logger.LogInformation("Creating/updating system description memory");
    
    // Create a JSON string describing the system
    string systemDescription = @"{
        ""description"": ""PostgMem is a PostgreSQL-based memory service for AI agents."",
        ""usage"": ""Agents can use this service to store and retrieve knowledge using vector embeddings."",
        ""capabilities"": [
            ""Store memories with types, content, tags, and confidence scores"",
            ""Search for memories using semantic similarity"",
            ""Retrieve specific memories by ID"",
            ""Delete memories when no longer needed""
        ],
        ""api"": {
            ""store"": ""Store new memories with type, content, source, tags, and confidence"",
            ""search"": ""Find similar memories using vector similarity search"",
            ""get"": ""Retrieve a specific memory by its ID"",
            ""delete"": ""Remove a memory by its ID""
        }
    }";
    
    // Store the system description directly using IStorage
    await storage.StoreMemory(
        type: "system",
        content: systemDescription,
        source: "system",
        tags: new[] { "system", "description", "help" },
        confidence: 1.0
    );
    
    logger.LogInformation("System description memory created/updated successfully");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    logger.LogError(ex, "Failed to create system memory: {Message}", ex.Message);
    // Don't throw here - application should still start even if memory creation fails
}

// Configure health check endpoints
app.MapHealthChecks("/healthz");

app.Run();