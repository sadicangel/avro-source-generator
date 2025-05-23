using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Diagnostics;

internal static class DiagnosticMessage
{
    private static string? GetDisplayText(SyntaxKind syntaxKind) =>
        SyntaxFacts.GetText(syntaxKind) ?? syntaxKind.ToString();

    // Scanning error messages.
    public static string InvalidCharacter(char character) =>
        $"Invalid character input: '{character}'";
    public static string InvalidSyntaxValue(string value, SyntaxKind kind) =>
        $"'{value}' is not a valid '{GetDisplayText(kind)}'";
    public static string UnterminatedComment() =>
        $"Unterminated comment";
    public static string UnterminatedString() =>
        $"Unterminated string literal";

    // Parsing error messages.
    //public static string ExpectedTypeDefinition() =>
    //    $"Expected type definition";
    //public static string InvalidLocationForFunctionDefinition() =>
    //    $"Invalid location for function definition";
    //public static string InvalidLocationForTypeDefinition() =>
    //    $"Invalid location for type definition";
    public static string UnexpectedToken(SyntaxKind expected, SyntaxKind actual) =>
        $"Unexpected token '{GetDisplayText(actual)}'. Expected '{GetDisplayText(expected)}'";

    //// Binding error messages.
    //public static string AmbiguousBinaryOperator(SyntaxToken @operator, string leftTypeName, string rightTypeName) =>
    //    $"Binary operator '{@operator.Text}' is ambiguous on operands of type '{leftTypeName}' and '{rightTypeName}'";
    //public static string AmbiguousInvocationOperator(params ReadOnlySpan<string> typeNames) =>
    //    $"Invocation operator is ambiguous for argument list '({string.Join(", ", typeNames.ToArray())})'";
    //public static string AmbiguousUnaryOperator(SyntaxToken @operator, string operandTypeName) =>
    //    $"Unary operator '{@operator.Text}' is ambiguous on operand of type '{operandTypeName}'";
    //public static string IndexOutOfRange(int arrayLength) =>
    //    $"Index is out of range. Must be non-negative and less than {arrayLength}";
    //public static string InvalidArgumentListLength(int listLength) =>
    //    $"Expression does not contain an overload that expects {listLength} arguments";
    //public static string InvalidArrayLength() =>
    //    $"Invalid array length expression. Must be a constant {SyntaxFacts.GetText(SyntaxKind.I32Keyword)} value";
    //public static string InvalidAssignment() =>
    //    "Invalid left-hand side of assignment. Expected a reference (variable, property or indexer)";
    //public static string InvalidBreakOrContinue() =>
    //    "No enclosing loop out of which to break or continue";
    //public static string InvalidConversion(string sourceTypeName, string targetTypeName) =>
    //    $"Invalid conversion from type '{sourceTypeName}' to '{targetTypeName}'";
    //public static string InvalidConversionDeclaration() =>
    //    "Invalid conversion declaration. Must have 1 parameter and definition must include containing type";
    //public static string InvalidExpressionType(string expectedTypeName, string actualTypeName) =>
    //    $"Invalid expression of type '{actualTypeName}'. Expected '{expectedTypeName}'";
    //public static string InvalidImplicitConversion(string sourceTypeName, string targeTypeName) =>
    //    $"Invalid implicit conversion from type '{sourceTypeName}' to '{targeTypeName}'. An explicit conversion exists (are you missing a cast?)";
    //public static string InvalidImplicitType(string typeName) =>
    //    $"Invalid implicit type '{typeName}'";
    //public static string InvalidOperatorDeclaration(string operatorKind, string operationParameterCount) =>
    //    $"Invalid {operatorKind} declaration. Must have {operationParameterCount} parameters";
    //public static string InvalidReturn() =>
    //    "No enclosing function out of which to return";
    //public static string ReadOnlyAssignment(string symbolName) =>
    //    $"Invalid assignment of '{symbolName}'. It is read-only";
    //public static string RedundantConversion() =>
    //    "Conversion is redundant";
    //public static string SymbolRedeclaration(string symbolName) =>
    //    $"Redeclaration of symbol '{symbolName}'";
    //public static string UndefinedBinaryOperator(SyntaxToken @operator, string leftTypeName, string rightTypeName) =>
    //    $"Binary operator '{@operator.Text}' is not defined for types '{leftTypeName}' and '{rightTypeName}'";
    //public static string UndefinedIndexOperator(string containingTypeName) =>
    //    $"Index operator is not defined for type '{containingTypeName}'";
    //public static string UndefinedInvocationOperator(string containingTypeName) =>
    //    $"Invocation operator is not defined for type '{containingTypeName}'";
    //public static string UndefinedType(string typeName) =>
    //    $"Undefined type '{typeName}'";
    //public static string UndefinedTypeMember(string typeName, string memberName) =>
    //    $"'{typeName}' does not contain a definition for '{memberName}'";
    //public static string UndefinedSymbol(string symbolName) =>
    //    $"Undefined symbol '{symbolName}'";
    //public static string UndefinedUnaryOperator(SyntaxToken @operator, string operandTypeName) =>
    //    $"Unary operator '{@operator.Text}' is not defined for type '{operandTypeName}'";
    //public static string UninitializedProperty(string propertyName) =>
    //    $"Non-optional property '{propertyName}' is uninitialized";
    //public static string UninitializedVariable(string variableName) =>
    //    $"Non-optional variable '{variableName}' is uninitialized";
    //public static string UnreachableCode() =>
    //    "Unreachable code detected";
}
