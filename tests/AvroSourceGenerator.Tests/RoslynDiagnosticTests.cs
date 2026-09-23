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
        foreach (var code in Enum.GetValues<AvroDiagnosticCode>().Where(static code => code is not AvroDiagnosticCode.None))
        {
            var core = new AvroDiagnostic(code, SourceSpan.None, "first", "second", "third");
            var diagnostic = core.ToDiagnostic();
            Assert.Equal($"AVROSG{(int)code:D4}", diagnostic.Id);
            Assert.Same(code.ToDiagnosticDescriptor(), diagnostic.Descriptor);
            var field = typeof(DiagnosticDescriptors).GetField(code.ToString(), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Same(field.GetValue(null), diagnostic.Descriptor);
            Assert.Same(diagnostic.Descriptor, core.ToDiagnostic().Descriptor);
            Assert.Equal((DiagnosticSeverity)core.Severity, diagnostic.Severity);
            Assert.Equal(core.GetMessage(), diagnostic.GetMessage());
            Assert.Equal(Location.None, diagnostic.Location);
        }
    }

    [Fact]
    public void Idl_diagnostics_use_specific_messages_and_ids()
    {
        var source = new SourceText("test.avdl", "bad");
        var diagnostics = new[] { AvroDiagnostic.InvalidCharacter(new SourceSpan(source, 0, source.Length)), AvroDiagnostic.InvalidIdlDocument(SourceSpan.None), };

        Assert.Collection(
            diagnostics,
            static diagnostic =>
            {
                Assert.Equal("bad", Assert.Single(diagnostic.Arguments!));
                Assert.Equal("Invalid character 'bad'.", diagnostic.GetMessage());
            },
            static diagnostic =>
            {
                Assert.Empty(diagnostic.Arguments);
                Assert.Equal("An Avro IDL file must contain a main schema directive or a single protocol declaration.", diagnostic.GetMessage());
            });
        Assert.All(
            diagnostics,
            static core =>
            {
                var diagnostic = core.ToDiagnostic();
                Assert.Equal(core.GetMessage(), diagnostic.GetMessage());
            });
    }

    [Fact]
    public void Diagnostic_codes_have_distinct_descriptors_and_ids()
    {
        var descriptors = Enum.GetValues<AvroDiagnosticCode>().Where(static code => code is not AvroDiagnosticCode.None).Select(DiagnosticDescriptors.ToDiagnosticDescriptor).ToArray();
        Assert.Equal(descriptors.Length, descriptors.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(descriptors.Length, descriptors.Select(static descriptor => descriptor.Id).Distinct().Count());
    }

    [Fact]
    public void Diagnostic_codes_have_unique_descriptions()
    {
        var descriptions = Enum.GetValues<AvroDiagnosticCode>().Where(static code => code is not AvroDiagnosticCode.None)
            .Select(static code => code.ToDiagnosticDescriptor().Description.ToString())
            .ToArray();

        Assert.All(descriptions, static description => Assert.False(string.IsNullOrWhiteSpace(description)));
        Assert.Equal(descriptions.Length, descriptions.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Core_and_roslyn_severities_are_value_compatible()
    {
        foreach (var severity in Enum.GetValues<AvroDiagnosticSeverity>())
            Assert.Equal(severity.ToString(), ((DiagnosticSeverity)severity).ToString());
    }

    [Fact]
    public void Multiline_spans_use_utf16_character_positions()
    {
        var source = new SourceText("test.avdl", "a\r\nb\rc\n");
        var location = new SourceSpan(source, 1, 5).ToLocation();
        Assert.Equal(new TextSpan(1, 5), location.SourceSpan);
        Assert.Equal(new LinePositionSpan(new LinePosition(0, 1), new LinePosition(2, 1)), location.GetLineSpan().Span);
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
