# PostgMem

A Model Context Protocol (MCP) server implementation that provides vector memory storage using PostgreSQL and pgvector for AI applications.

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

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database with pgvector extension installed
- Text embedding API (default configuration uses Ollama)

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
ConnectionStrings__Storage="Host=localhost;Database=mcp_memory;Username=postgres;Password=postgres"
Embeddings__ApiUrl=http://localhost:11434/
Embeddings__Model=all-minilm:33m-l12-v2-fp16
```

- `ConnectionStrings__Storage`: PostgreSQL connection string
- `Embeddings__ApiUrl`: URL of your embedding API (defaults to Ollama)
- `Embeddings__Model`: The embedding model to use

## Running the Application

1. Navigate to the PostgMem directory
2. Run the application:
   ```
   dotnet run
   ```
3. The MCP server will be available at `http://localhost:5000` by default

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