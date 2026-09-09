using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Tests;

public sealed class GeneratorConfigurationEqualityTests
{
    [Fact]
    public void Matching_ordered_diagnostics_have_equal_options_and_hashes()
    {
        var first = Diagnostic("TEST0001", "first");
        var second = Diagnostic("TEST0002", "second");
        var a = Options([first, second]);
        var b = Options([first, second]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal([first, second], b.Diagnostics);
        Assert.NotEqual(a, Options([second, first]));
    }

    [Fact]
    public void Diagnostics_with_the_same_id_keep_their_relative_order()
    {
        var first = Diagnostic("TEST0001", "first");
        var second = Diagnostic("TEST0001", "second");
        var other = Diagnostic("TEST0002", "other");
        var a = Options([first, second, other]);
        var b = Options([first, second, other]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal([first, second, other], a.Diagnostics);
        Assert.NotEqual(a, Options([second, first, other]));
        Assert.NotEqual(a, Options([first, other]));
    }

    private static AvroDiagnostic Diagnostic(string id, string argument) => new AvroDiagnostic(id == "TEST0001" ? AvroDiagnosticCode.InvalidSchema : AvroDiagnosticCode.InvalidJson, Text.SourceSpan.None, argument);

    private static GeneratorConfiguration Options(ImmutableArray<AvroDiagnostic> diagnostics) => new GeneratorConfiguration(GenerationTarget.Modern, LanguageFeatures.Latest, AccessModifier.Public, ReferenceResolution.Strict, DuplicateResolution.Error, diagnostics);
}
