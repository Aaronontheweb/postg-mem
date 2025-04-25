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

app.MapMcp();
app.Run();