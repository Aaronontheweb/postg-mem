using System;

namespace PostgMem.Models;

public enum RelationshipType
{
    Parent,
    Child,
    Reference,
    Related,
    Cause,
    Effect,
    Duplicate,
    VersionOf,
    PartOf,
    Contains,
    Precedes,
    Follows,
    ExampleOf,
    InstanceOf,
    Generalizes,
    Specializes,
    Synonym,
    Antonym
}

public static class RelationshipTypeHelper
{
    public static string ToDbString(this RelationshipType type)
        => type.ToString();

    public static RelationshipType FromDbString(string value)
    {
        if (Enum.TryParse<RelationshipType>(value, true, out var result))
            return result;
        throw new ArgumentException($"Unknown relationship type: {value}");
    }
} 