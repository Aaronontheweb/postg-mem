# PostgMem

PostgMem is a Vector Database Memory using PostgreSQL with pgvector extension and Ollama for embedding generation.

## Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK

## Running the Application

### Option 1: Start Infrastructure and Container Application (Recommended)

1. Build and publish the .NET container:

```bash
# From solution root directory
dotnet publish -c Release /t:PublishContainer
```

This will create a container image named `postgmem:latest`.

2. Start all services with Docker Compose:

```bash
docker-compose up -d
```

This will start:
- PostgreSQL with pgvector extension on port 5432
- PgAdmin (PostgreSQL admin interface) on port 5050
- Ollama (for embeddings) on port 11434
- PostgMem application on port 5000

### Option 2: Local Development

1. Start only the infrastructure services:

```bash
docker-compose up -d postgres pgadmin ollama ollama-init
```

2. Run the .NET application locally with the required environment variables:

```bash
# PowerShell:
cd PostgMem
$env:ConnectionStrings__Storage="Host=localhost;Port=5432;Database=postgmem;Username=postgres;Password=postgres"
$env:Embeddings__ApiUrl="http://localhost:11434"
$env:Embeddings__Model="all-minilm:33m-l12-v2-fp16"
dotnet run
```

For bash/zsh (Linux/macOS):
```bash
cd PostgMem
export ConnectionStrings__Storage="Host=localhost;Port=5432;Database=postgmem;Username=postgres;Password=postgres"
export Embeddings__ApiUrl="http://localhost:11434"
export Embeddings__Model="all-minilm:33m-l12-v2-fp16"
dotnet run
```

## Container Information

The PostgMem application is containerized with the following settings:
- Repository: `postgmem`
- Tags: `latest` (and version when specified)
- OS: Linux
- Supported architectures: `linux-x64`, `linux-arm64`, `linux-arm`

## Connecting to PostgMem

The PostgMem API will be available at:

```
http://localhost:5000
```

## Accessing PostgreSQL

PostgreSQL is configured with the following credentials:
- Host: localhost
- Port: 5432
- Database: postgmem
- Username: postgres
- Password: postgres

You can also use PgAdmin to manage the database at http://localhost:5050.

## Embedding Model

The system uses the `all-minilm:33m-l12-v2-fp16` model from Ollama for generating embeddings.

## Shutting Down

To stop the infrastructure services, run:

```bash
docker-compose down
```

To stop and remove all volumes (which will delete all data), run:

```bash
docker-compose down -v
```

## Overview

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

## Setup

### Database Configuration

1. Create a PostgreSQL database for the application
2. Install the pgvector extension in your database:
   ```sql
   CREATE EXTENSION vector;
   ```
3. Create the memories table:
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

### Environment Configuration

Configure the application settings in the `.env` file:

```
ConnectionStrings__Storage=Host=localhost;Port=5432;Database=postgmem;Username=postgres;Password=postgres
Embeddings__ApiUrl=http://localhost:11434
Embeddings__Model=all-minilm:33m-l12-v2-fp16
```

- `ConnectionStrings__Storage`: PostgreSQL connection string
- `Embeddings__ApiUrl`: URL of your embedding API (defaults to Ollama)
- `Embeddings__Model`: The embedding model to use

## MCP Tools

The following MCP tools are available:

### Store

Store a new memory in the database.

Parameters:
- `type` (string): The type of memory (e.g., 'conversation', 'document', etc.)
- `content` (string): The content of the memory as a JSON object
- `source` (string): The source of the memory (e.g., 'user', 'system', etc.)
- `tags` (string[]): Optional tags to categorize the memory
- `confidence` (double): Confidence score for the memory (0.0 to 1.0)

### Search

Search for memories similar to the provided text.

Parameters:
- `query` (string): The text to search for similar memories
- `limit` (int): Maximum number of results to return (default: 10)
- `minSimilarity` (double): Minimum similarity threshold (0.0 to 1.0) (default: 0.7)
- `filterTags` (string[]): Optional tags to filter memories

### Get

Retrieve a specific memory by ID.

Parameters:
- `id` (Guid): The ID of the memory to retrieve

### Delete

Delete a memory by ID.

Parameters:
- `id` (Guid): The ID of the memory to delete

## Implementation Details

- `Memory.cs`: Defines the data model for memories
- `Storage.cs`: Handles database operations for storing and retrieving memories
- `EmbeddingService.cs`: Generates vector embeddings for text
- `MemoryTools.cs`: Implements MCP tools for interacting with the memory storage

## License

[Your license information here]

## Contributing

[Your contribution guidelines here] 