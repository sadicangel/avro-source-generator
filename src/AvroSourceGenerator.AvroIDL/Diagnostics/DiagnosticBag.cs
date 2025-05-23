using System.Collections;
using System.Diagnostics;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Diagnostics;

public readonly record struct SymbolKind;

public interface IReadOnlyDiagnosticBag : IReadOnlyList<Diagnostic>
{
    bool HasErrorDiagnostics { get; }
    bool HasWarningDiagnostics { get; }

    IEnumerable<Diagnostic> GetErrors();
    IEnumerable<Diagnostic> GetWarnings();
}

[DebuggerDisplay("Count = {Count}")]
public sealed class DiagnosticBag : IReadOnlyDiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics;

    public DiagnosticBag() => _diagnostics = [];
    public DiagnosticBag(IEnumerable<Diagnostic> diagnostics) => _diagnostics = new(diagnostics);

    public int Count { get => _diagnostics.Count; }

    public bool HasErrorDiagnostics { get => _diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error); }

    public bool HasWarningDiagnostics { get => _diagnostics.Any(d => d.Severity is DiagnosticSeverity.Warning); }

    public bool HasInformationDiagnostics { get => _diagnostics.Any(d => d.Severity is DiagnosticSeverity.Information); }

    public Diagnostic this[int index] { get => _diagnostics[index]; }

    public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<Diagnostic> GetErrors() => this.Where(d => d.Severity is DiagnosticSeverity.Error);
    public IEnumerable<Diagnostic> GetWarnings() => this.Where(d => d.Severity is DiagnosticSeverity.Warning);

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

    public void Report(SourceSpan sourceSpan, DiagnosticSeverity severity, string message)
    {
        _diagnostics.Add(new Diagnostic(Id: "", sourceSpan, severity, message));
    }

    public void ReportError(SourceSpan sourceSpan, string message) =>
        Report(sourceSpan, DiagnosticSeverity.Error, message);
    public void ReportWarning(SourceSpan sourceSpan, string message) =>
        Report(sourceSpan, DiagnosticSeverity.Warning, message);
    public void ReportInformation(SourceSpan sourceSpan, string message) =>
        Report(sourceSpan, DiagnosticSeverity.Warning, message);

    // Scanning Errors.
    internal void ReportInvalidCharacter(SourceSpan sourceSpan, char character) =>
        ReportError(sourceSpan, DiagnosticMessage.InvalidCharacter(character));
    internal void ReportInvalidSyntaxValue(SourceSpan sourceSpan, SyntaxKind kind) =>
        ReportError(sourceSpan, DiagnosticMessage.InvalidSyntaxValue(sourceSpan.Text.ToString(), kind));
    internal void ReportUnterminatedComment(SourceSpan sourceSpan) =>
        ReportError(sourceSpan, DiagnosticMessage.UnterminatedComment());
    internal void ReportUnterminatedString(SourceSpan sourceSpan) =>
        ReportError(sourceSpan, DiagnosticMessage.UnterminatedString());

    // Parsing Errors.
    //internal void ReportExpectedTypeDefinition(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.ExpectedTypeDefinition());
    //internal void ReportInvalidLocationForFunctionDefinition(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidLocationForFunctionDefinition());
    //internal void ReportInvalidLocationForTypeDefinition(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidLocationForTypeDefinition());
    internal void ReportUnexpectedToken(SyntaxKind expected, SyntaxToken actual) =>
        ReportError(actual.SourceSpan, DiagnosticMessage.UnexpectedToken(expected, actual.SyntaxKind));

    //// Binding Errors.
    //internal void ReportAmbiguousBinaryOperator(SyntaxToken @operator, string leftTypeName, string rightTypeName) =>
    //    ReportError(@operator.Location, DiagnosticMessage.AmbiguousBinaryOperator(@operator, leftTypeName, rightTypeName));
    //internal void ReportAmbiguousInvocationOperator(SourceSpan sourceSpan, params ReadOnlySpan<string> typeNames) =>
    //    ReportError(sourceSpan, DiagnosticMessage.AmbiguousInvocationOperator(typeNames));
    //internal void ReportAmbiguousUnaryOperator(SyntaxToken @operator, string operandTypeName) =>
    //    ReportError(@operator.Location, DiagnosticMessage.AmbiguousUnaryOperator(@operator, operandTypeName));
    //internal void ReportIndexOutOfRange(SourceSpan sourceSpan, int arrayLength) =>
    //    ReportError(sourceSpan, DiagnosticMessage.IndexOutOfRange(arrayLength));
    //internal void ReportInvalidArgumentListLength(SourceSpan sourceSpan, int listLength) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidArgumentListLength(listLength));
    //internal void ReportInvalidArrayLength(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidArrayLength());
    //internal void ReportInvalidAssignment(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidAssignment());
    //internal void ReportInvalidBreakOrContinue(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidBreakOrContinue());
    //internal void ReportInvalidConversion(SourceSpan sourceSpan, string sourceTypeName, string targetTypeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidConversion(sourceTypeName, targetTypeName));
    //internal void ReportInvalidConversionDeclaration(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidConversionDeclaration());
    //internal void ReportInvalidExpressionType(SourceSpan sourceSpan, string expectedTypeName, string actualTypeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidExpressionType(expectedTypeName, actualTypeName));
    //internal void ReportInvalidImplicitConversion(SourceSpan sourceSpan, string sourceTypeName, string targetTypeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidImplicitConversion(sourceTypeName, targetTypeName));
    //internal void ReportInvalidImplicitType(SourceSpan sourceSpan, string typeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidImplicitType(typeName));
    //internal void ReportInvalidOperatorDeclaration(SourceSpan sourceSpan, string operatorKind, string operationParameterCount) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidOperatorDeclaration(operatorKind, operationParameterCount));
    //internal void ReportInvalidReturn(SourceSpan sourceSpan) =>
    //    ReportError(sourceSpan, DiagnosticMessage.InvalidReturn());
    //internal void ReportReadOnlyAssignment(SourceSpan sourceSpan, string symbolName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.ReadOnlyAssignment(symbolName));
    //internal void ReportRedundantConversion(SourceSpan sourceSpan) =>
    //    ReportWarning(sourceSpan, DiagnosticMessage.RedundantConversion());
    //internal void ReportSymbolRedeclaration(SourceSpan sourceSpan, string symbolName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.SymbolRedeclaration(symbolName));
    //internal void ReportUndefinedBinaryOperator(SyntaxToken @operator, string leftTypeName, string rightTypeName) =>
    //    ReportError(@operator.Location, DiagnosticMessage.UndefinedBinaryOperator(@operator, leftTypeName, rightTypeName));
    //internal void ReportUndefinedIndexOperator(SourceSpan sourceSpan, string containingTypeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UndefinedInvocationOperator(containingTypeName));
    //internal void ReportUndefinedInvocationOperator(SourceSpan sourceSpan, string containingTypeName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UndefinedInvocationOperator(containingTypeName));
    //internal void ReportUndefinedType(SourceSpan sourceSpan, string typeName) => ReportError(sourceSpan, DiagnosticMessage.UndefinedType(typeName));
    //internal void ReportUndefinedTypeMember(SourceSpan sourceSpan, string typeName, string memberName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UndefinedTypeMember(typeName, memberName));
    //internal void ReportUndefinedSymbol(SourceSpan sourceSpan, string symbolName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UndefinedSymbol(symbolName));
    //internal void ReportUndefinedUnaryOperator(SyntaxToken @operator, string operandTypeName) =>
    //    ReportError(@operator.Location, DiagnosticMessage.UndefinedUnaryOperator(@operator, operandTypeName));
    //internal void ReportUninitializedProperty(SourceSpan sourceSpan, string propertyName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UninitializedProperty(propertyName));
    //internal void ReportUninitializedVariable(SourceSpan sourceSpan, string variableName) =>
    //    ReportError(sourceSpan, DiagnosticMessage.UninitializedVariable(variableName));
    //internal void ReportUnreachableCode(SourceSpan sourceSpan) =>
    //    ReportWarning(sourceSpan, DiagnosticMessage.UnreachableCode());
}
