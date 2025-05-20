using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using PostgMem.Services;
using PostgMem.Settings;
using Registrator.Net;
using HealthChecks.NpgSql;

namespace PostgMem.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgMem(
        this IServiceCollection services)
    {
        services.AddEmbeddings();
        services.AddStorage();
        services.AutoRegisterTypesInAssemblies(typeof(Storage).Assembly);
        return services;
    }

    public static IServiceCollection AddEmbeddings(
        this IServiceCollection services)
    {
        services
            .AddSingleton<EmbeddingSettings>(sp =>
                sp.GetRequiredService<IConfiguration>().GetSection("Embeddings").Get<EmbeddingSettings>() ??
                throw new ArgumentNullException("Embeddings Settings"))
            .AddHttpClient<IEmbeddingService, EmbeddingService>((sp, client) =>
            {
                EmbeddingSettings settings = sp.GetRequiredService<EmbeddingSettings>();
                client.BaseAddress = settings.ApiUrl;
                client.Timeout = settings.Timeout;
            });

        return services;
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services)
    {
        services
            .AddSingleton(sp =>
            {
                string connectionString =
                    sp.GetRequiredService<IConfiguration>().GetConnectionString("Storage") ??
                    throw new ArgumentNullException("Storage Connection String");
                NpgsqlDataSourceBuilder sourceBuilder = new(connectionString);
                sourceBuilder.UseVector();
                return sourceBuilder.Build();
            });
        return services;
    }
}