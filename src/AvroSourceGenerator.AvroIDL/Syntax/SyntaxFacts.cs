namespace AvroSourceGenerator.AvroIDL.Syntax;

public static class SyntaxFacts
{
    public static string? GetText(SyntaxKind syntaxKind)
    {
        return syntaxKind switch
        {
            SyntaxKind.InvalidSyntax => null,
            SyntaxKind.EofToken => null,

            SyntaxKind.BraceOpenToken => "{",
            SyntaxKind.BraceCloseToken => "}",
            SyntaxKind.ParenthesisOpenToken => "(",
            SyntaxKind.ParenthesisCloseToken => ")",
            SyntaxKind.BracketOpenToken => "[",
            SyntaxKind.BracketCloseToken => "]",
            SyntaxKind.LessThanToken => "<",
            SyntaxKind.GreaterThanToken => ">",
            SyntaxKind.AtSignToken => "@",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.DotToken => ".",
            SyntaxKind.ColonToken => ":",
            SyntaxKind.SemicolonToken => ";",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.HookToken => "?",

            SyntaxKind.IntegerLiteralToken => null,
            SyntaxKind.FloatLiteralToken => null,
            SyntaxKind.StringLiteralToken => null,
            SyntaxKind.IdentifierToken => null,

            SyntaxKind.VoidKeyword => "void",
            SyntaxKind.NullKeyword => "null",
            SyntaxKind.IntKeyword => "int",
            SyntaxKind.LongKeyword => "long",
            SyntaxKind.StringKeyword => "string",
            SyntaxKind.BooleanKeyword => "boolean",
            SyntaxKind.FloatKeyword => "float",
            SyntaxKind.DoubleKeyword => "double",
            SyntaxKind.BytesKeyword => "bytes",
            SyntaxKind.TrueKeyword => "true",
            SyntaxKind.FalseKeyword => "false",

            SyntaxKind.ArrayKeyword => "array",
            SyntaxKind.MapKeyword => "map",

            SyntaxKind.EnumKeyword => "enum",
            SyntaxKind.FixedKeyword => "fixed",
            SyntaxKind.RecordKeyword => "record",
            SyntaxKind.ErrorKeyword => "error",
            SyntaxKind.ProtocolKeyword => "protocol",

            SyntaxKind.DecimalKeyword => "decimal",
            SyntaxKind.DateKeyword => "date",
            SyntaxKind.TimeMsKeyword => "time_ms",
            SyntaxKind.TimestampMsKeyword => "timestamp_ms",
            SyntaxKind.LocalTimestampMsKeyword => "local_timestamp_ms",
            SyntaxKind.UuidKeyword => "uuid",

            SyntaxKind.ThrowsKeyword => "throws",
            SyntaxKind.OneWayKeyword => "oneway",

            SyntaxKind.SchemaKeyword => "schema",
            SyntaxKind.ImportKeyword => "import",

            SyntaxKind.LineBreakTrivia => null,
            SyntaxKind.SingleLineCommentTrivia => null,
            SyntaxKind.MultiLineCommentTrivia => null,
            SyntaxKind.WhiteSpaceTrivia => null,
            SyntaxKind.InvalidTextTrivia => null,

            SyntaxKind.CompilationUnit => null,

            SyntaxKind.SimpleName => null,
            SyntaxKind.QualifiedName => null,

            SyntaxKind.VoidType => null,
            SyntaxKind.NullType => null,
            SyntaxKind.IntType => null,
            SyntaxKind.LongType => null,
            SyntaxKind.StringType => null,
            SyntaxKind.BooleanType => null,
            SyntaxKind.FloatType => null,
            SyntaxKind.DoubleType => null,
            SyntaxKind.BytesType => null,

            SyntaxKind.EnumDeclaration => null,
            SyntaxKind.RecordDeclaration => null,
            SyntaxKind.ErrorDeclaration => null,
            SyntaxKind.FixedDeclaration => null,
            SyntaxKind.ProtocolDeclaration => null,
            SyntaxKind.FieldDeclaration => null,
            SyntaxKind.DefaultValueClause => null,
            SyntaxKind.JsonValue => null,
            SyntaxKind.MessageDeclaration => null,
            SyntaxKind.ParameterDeclaration => null,
            SyntaxKind.OneWayClause => null,
            SyntaxKind.ThrowsErrorClause => null,

            _ => throw new InvalidOperationException($"Unexpected {nameof(SyntaxKind)}: '{syntaxKind}'")
        };
    }

    public static SyntaxKind GetKeywordKind(ReadOnlySpan<char> syntaxText)
    {
        return syntaxText switch
        {
            "void" => SyntaxKind.VoidKeyword,
            "null" => SyntaxKind.NullKeyword,
            "int" => SyntaxKind.IntKeyword,
            "long" => SyntaxKind.LongKeyword,
            "string" => SyntaxKind.StringKeyword,
            "boolean" => SyntaxKind.BooleanKeyword,
            "float" => SyntaxKind.FloatKeyword,
            "double" => SyntaxKind.DoubleKeyword,
            "bytes" => SyntaxKind.BytesKeyword,

            "true" => SyntaxKind.TrueKeyword,
            "false" => SyntaxKind.FalseKeyword,

            "array" => SyntaxKind.ArrayKeyword,
            "map" => SyntaxKind.MapKeyword,

            "enum" => SyntaxKind.EnumKeyword,
            "fixed" => SyntaxKind.FixedKeyword,
            "record" => SyntaxKind.RecordKeyword,
            "error" => SyntaxKind.ErrorKeyword,
            "protocol" => SyntaxKind.ProtocolKeyword,

            "decimal" => SyntaxKind.DecimalKeyword,
            "date" => SyntaxKind.DateKeyword,
            "time_ms" => SyntaxKind.TimeMsKeyword,
            "timestamp_ms" => SyntaxKind.TimestampMsKeyword,
            "local_timestamp_ms" => SyntaxKind.LocalTimestampMsKeyword,
            "uuid" => SyntaxKind.UuidKeyword,

            "throws" => SyntaxKind.ThrowsKeyword,
            "oneway" => SyntaxKind.OneWayKeyword,

            "schema" => SyntaxKind.SchemaKeyword,
            "import" => SyntaxKind.ImportKeyword,

            _ => SyntaxKind.IdentifierToken,
        };
    }
}
