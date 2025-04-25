using Npgsql;
using PostgMem.Services;
using PostgMem.Settings;

namespace PostgMem.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddPostgMem(
        this IServiceCollection services)
    {
        services.AddEmbeddings();
        services.AddStorage();
        return services;
    }

    internal static IServiceCollection AddEmbeddings(
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

    internal static IServiceCollection AddStorage(
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