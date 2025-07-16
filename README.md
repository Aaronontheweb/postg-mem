# PostgMem

## Deprecated. See https://github.com/petabridge/memorizer-v1 for the final version of this.

PostgMem is a .NET-based service that allows AI agents to store, retrieve, and search through memories using vector embeddings. It leverages PostgreSQL with the pgvector extension to provide efficient similarity search capabilities.

Key features:
- Store structured memories with vector embeddings
- Retrieve memories by ID
- Semantic search through memories using vector similarity
- Filter search results using tags
- MCP (Model Context Protocol) integration for easy use with AI agents

## Technologies

- .NET 9.0
- PostgreSQL with pgvector extension
- Model Context Protocol (MCP)
- ASP.NET Core
- Npgsql for PostgreSQL connectivity

---

## 🚀 Local Deployment (Recommended)

### Prerequisites
- Docker and Docker Compose
- .NET 9.0 SDK

### 1. Start Infrastructure and Application

```bash
# From solution root directory
# Build and publish the .NET container

dotnet publish -c Release /t:PublishContainer
```

This creates a container image named `postgmem:latest`.

```bash
docker-compose up -d
```

This starts:
- PostgreSQL with pgvector (port 5432)
- PgAdmin (port 5050)
- Ollama (port 11434)
- PostgMem API (port 5000)

---

## 🔌 MCP Configuration Example

To use PostgMem with any MCP-compatible client, add the following to your configuration (e.g., `mcp.json`):

```json
{
  "postgmem": {
    "url": "http://localhost:5000/sse"
  }
}
```

---

## 🧠 Example System Prompt for LLMs

> You have access to a long-term memory system via the Model Context Protocol (MCP) at the endpoint `postgmem`. Use the following tools:
>
> - `store`: Store a new memory. Parameters: `type`, `content` (JSON), `source`, `tags`, `confidence`, `relatedTo` (optional, memory ID), `relationshipType` (optional).
> - `search`: Search for similar memories. Parameters: `query`, `limit`, `minSimilarity`, `filterTags`.
> - `get`: Retrieve a memory by ID. Parameter: `id`.
> - `getMany`: Retrieve multiple memories by their IDs. Parameter: `ids` (list of IDs).
> - `delete`: Delete a memory by ID. Parameter: `id`.
> - `createRelationship`: Create a relationship between two memories. Parameters: `fromId`, `toId`, `type`.
>
> Use these tools to remember, recall, relate, and manage information as needed to assist the user. You can also manually retrieve or relate memories by their IDs when necessary.

---

## 📖 Documentation

- [Configuration & Advanced Setup](docs/configuration.md)
- [Local Development](docs/local-development.md)
- [Schema Migrations](docs/schema-migrations.md)
- [Architecture Decision Records](docs/adr/README.md)

## License

MIT
