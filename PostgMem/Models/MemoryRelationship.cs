using System;

namespace PostgMem.Models;

public class MemoryRelationship
{
    public Guid Id { get; init; }
    public Guid FromMemoryId { get; init; }
    public Guid ToMemoryId { get; init; }
    public RelationshipType Type { get; init; }
    public DateTime CreatedAt { get; init; }
} 