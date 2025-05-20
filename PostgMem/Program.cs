using Configuration.Extensions.EnvironmentFile;
using PostgMem.Extensions;
using PostgMem.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .Configuration
    .AddEnvironmentFile() 
    .AddEnvironmentVariables("POSTGMEM_"); 

builder
    .Logging
    .AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; })
    .AddFile("log.log", minimumLevel: LogLevel.Trace);

builder
    .Services
    .AddPostgMem()
    .AddMcpServer().WithTools<MemoryTools>();


WebApplication app = builder.Build();

// Run schema migration at startup
try
{
    var connectionString = builder.Configuration.GetConnectionString("Storage") ?? throw new InvalidOperationException("Missing Storage connection string");
    PostgMem.Services.SchemaMigrator.MigrateAsync(connectionString).GetAwaiter().GetResult();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Schema migration failed: {ex.Message}");
    throw;
}

app.MapMcp();
app.Run();