using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace PostgMem.Services;

public static class SchemaMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS memories (
                id UUID PRIMARY KEY,
                type TEXT NOT NULL,
                content JSONB NOT NULL,
                source TEXT NOT NULL,
                embedding VECTOR(384) NOT NULL,
                tags TEXT[] NOT NULL,
                confidence DOUBLE PRECISION NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
        ";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
} 