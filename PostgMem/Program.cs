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

// Configure health check endpoints
app.MapHealthChecks("/healthz");

app.Run();