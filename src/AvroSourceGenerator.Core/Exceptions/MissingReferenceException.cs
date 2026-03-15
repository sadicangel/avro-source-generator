using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Exceptions;

public sealed class MissingReferenceException : Exception
{
    public MissingReferenceException(SchemaName missingReference)
        : this([missingReference])
    {
    }

    public MissingReferenceException(IEnumerable<SchemaName> missingReferences)
        : this(Normalize(missingReferences))
    {
    }

    private MissingReferenceException(ImmutableArray<SchemaName> missingReferences)
        : base($"The following schema references could not be resolved: {string.Join(", ", missingReferences.Select(static x => x.FullName))}")
    {
        MissingReferences = missingReferences;
    }

    public ImmutableArray<SchemaName> MissingReferences { get; }

    private static ImmutableArray<SchemaName> Normalize(IEnumerable<SchemaName> missingReferences) =>
        [..
            missingReferences
                .Distinct()
                .OrderBy(static x => x.FullName, StringComparer.Ordinal)
        ];
}
