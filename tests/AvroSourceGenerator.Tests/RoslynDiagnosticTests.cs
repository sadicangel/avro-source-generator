using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceText = AvroSourceGenerator.Text.SourceText;

namespace AvroSourceGenerator.Tests;

public sealed class RoslynDiagnosticTests
{
    [Fact]
    public void Every_core_code_maps_to_a_cached_descriptor_and_formats()
    {
        foreach (var code in Enum.GetValues<AvroDiagnosticCode>())
        {
            var core = new AvroDiagnostic(code, SourceSpan.None, "first", "second");
            var diagnostic = core.ToDiagnostic();
            var field = typeof(DiagnosticDescriptors).GetField(code.ToString(), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            Assert.StartsWith("AVROSG", diagnostic.Id);
            Assert.Same(field.GetValue(null), diagnostic.Descriptor);
            Assert.Same(diagnostic.Descriptor, core.ToDiagnostic().Descriptor);
            Assert.Equal(core.Severity == AvroDiagnosticSeverity.Warning ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.DoesNotContain("{0}", diagnostic.GetMessage());
            Assert.Equal(Location.None, diagnostic.Location);
        }
    }

    [Fact]
    public void Idl_diagnostics_have_one_source_prefix()
    {
        var source = new SourceText("test.avdl", "bad");
        var diagnostics = new[]
        {
            AvroDiagnostic.InvalidCharacter(new SourceSpan(source, 0, source.Length)),
            AvroDiagnostic.InvalidSource(SourceSpan.None, "bad"),
        };

        Assert.Collection(
            diagnostics,
            static diagnostic =>
            {
                Assert.Equal("bad", Assert.Single(diagnostic.Arguments!));
                Assert.Equal("The provided Avro IDL source is invalid: Invalid character input: 'bad'", diagnostic.GetMessage());
            },
            static diagnostic =>
            {
                Assert.Equal("bad", Assert.Single(diagnostic.Arguments!));
                Assert.Equal("The provided Avro IDL source is invalid: bad", diagnostic.GetMessage());
            });
        Assert.All(diagnostics, static core =>
        {
            var diagnostic = core.ToDiagnostic();
            Assert.Equal("AVROSG1000", diagnostic.Id);
            Assert.Equal(core.GetMessage(), diagnostic.GetMessage());
        });
    }

    [Fact]
    public void Idl_codes_have_distinct_descriptors_with_shared_metadata()
    {
        var descriptors = Enum.GetValues<AvroDiagnosticCode>()
            .Where(static code => code.IsInvalidSyntax)
            .Select(DiagnosticDescriptors.Get)
            .ToArray();
        var expected = DiagnosticDescriptors.InvalidSource;

        Assert.Equal(descriptors.Length, descriptors.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(descriptors, descriptor =>
        {
            Assert.Equal(expected.Id, descriptor.Id);
            Assert.Equal(expected.Title, descriptor.Title);
            Assert.Equal(expected.Category, descriptor.Category);
            Assert.Equal(expected.DefaultSeverity, descriptor.DefaultSeverity);
            Assert.Equal(expected.Description, descriptor.Description);
        });
    }

    [Fact]
    public void Multiline_spans_use_utf16_character_positions()
    {
        var source = new SourceText("test.avdl", "a\r\nb\rc\n");
        var location = new SourceSpan(source, 1, 5).ToLocation();
        Assert.Equal(new TextSpan(1, 5), location.SourceSpan);
        Assert.Equal(new LinePositionSpan(new(0, 1), new(2, 1)), location.GetLineSpan().Span);
    }

    [Theory]
    [InlineData("", 0, 0)]
    [InlineData(" \r\n\t", 1, 1)]
    [InlineData("a\r\nb\rc\n", 3, 0)]
    public void Eof_and_empty_file_spans_remain_file_locations(string text, int line, int column)
    {
        var source = new SourceText("test.avsc", text);
        var location = new SourceSpan(source, text.Length, 0).ToLocation();
        Assert.Equal("test.avsc", location.GetLineSpan().Path);
        Assert.Equal(new LinePosition(line, column), location.GetLineSpan().StartLinePosition);
        Assert.Equal(new TextSpan(text.Length, 0), location.SourceSpan);
    }
}
