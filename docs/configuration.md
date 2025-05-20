# Configuration & Advanced Setup

This document contains detailed configuration, setup, and advanced usage instructions for PostgMem.

---

## Database Configuration

1. **Create a PostgreSQL database for the application**
2. **Install the pgvector extension in your database:**
   ```sql
   CREATE EXTENSION vector;
   ```
3. **Create the memories table:**
   ```sql
   CREATE TABLE memories (
       id UUID PRIMARY KEY,
       type TEXT NOT NULL,
       content JSONB NOT NULL,
       source TEXT NOT NULL,
       embedding VECTOR(384) NOT NULL,
       tags TEXT[] NOT NULL,
       confidence DOUBLE PRECISION NOT NULL,
       created_at TIMESTAMP WITH TIME ZONE NOT NULL,
       updated_at TIMESTAMP WITH TIME ZONE NOT NULL
   );
   ```

---

## Environment Configuration

Configure the application settings in the `.env` file or as environment variables:

```
ConnectionStrings__Storage=Host=localhost;Port=5432;Database=postgmem;Username=postgres;Password=postgres
Embeddings__ApiUrl=http://localhost:11434
Embeddings__Model=all-minilm:33m-l12-v2-fp16
```

- `ConnectionStrings__Storage`: PostgreSQL connection string
- `Embeddings__ApiUrl`: URL of your embedding API (defaults to Ollama)
- `Embeddings__Model`: The embedding model to use

---

## Container Information

The PostgMem application is containerized with the following settings:
- Repository: `postgmem`
- Tags: `latest` (and version when specified)
- OS: Linux
- Supported architectures: `linux-x64`, `linux-arm64`, `linux-arm`

---

## Accessing PostgreSQL

PostgreSQL is configured with the following credentials:
- Host: localhost
- Port: 5432
- Database: postgmem
- Username: postgres
- Password: postgres

You can also use PgAdmin to manage the database at http://localhost:5050.

---

## Embedding Model

The system uses the `all-minilm:33m-l12-v2-fp16` model from Ollama for generating embeddings.

---

## Shutting Down

To stop the infrastructure services, run:

```bash
docker-compose down
```

To stop and remove all volumes (which will delete all data), run:

```bash
docker-compose down -v
```

---

## Implementation Details

- `Memory.cs`: Defines the data model for memories
- `Storage.cs`: Handles database operations for storing and retrieving memories
- `EmbeddingService.cs`: Generates vector embeddings for text
- `MemoryTools.cs`: Implements MCP tools for interacting with the memory storage

---

## MCP Tools

The following MCP tools are available:

### Store
Store a new memory in the database.
- `type` (string): The type of memory (e.g., 'conversation', 'document', etc.)
- `content` (string): The content of the memory as a JSON object
- `source` (string): The source of the memory (e.g., 'user', 'system', etc.)
- `tags` (string[]): Optional tags to categorize the memory
- `confidence` (double): Confidence score for the memory (0.0 to 1.0)

### Search
Search for memories similar to the provided text.
- `query` (string): The text to search for similar memories
- `limit` (int): Maximum number of results to return (default: 10)
- `minSimilarity` (double): Minimum similarity threshold (0.0 to 1.0) (default: 0.7)
- `filterTags` (string[]): Optional tags to filter memories

### Get
Retrieve a specific memory by ID.
- `id` (Guid): The ID of the memory to retrieve

### Delete
Delete a memory by ID.
- `id` (Guid): The ID of the memory to delete

---

## Docker Compose Networking

If you're running both your client application and PostgMem in Docker containers using Docker Compose, ensure they're on the same network. Use the service name as the hostname:

```json
{
  "postgmem": {
    "url": "http://postgmem-api:5000/sse"
  }
}
```

---

## Testing MCP Connectivity

To test if your MCP connection is working:

1. Start the PostgMem server
2. Send a simple curl request to verify the server is responding:

```bash
curl http://localhost:5000/sse
```

You should receive a response confirming the SSE endpoint is available.

---

## For Contributors

If you want to customize or extend the database schema, see [docs/schema-migrations.md](schema-migrations.md) for details on how migrations work, how to add new migrations, and best practices for schema changes. 