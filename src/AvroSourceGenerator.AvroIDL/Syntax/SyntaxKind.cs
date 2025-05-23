namespace AvroSourceGenerator.AvroIDL.Syntax;

public enum SyntaxKind
{
    InvalidSyntax,

    EofToken,

    BraceOpenToken,
    BraceCloseToken,

    ParenthesisOpenToken,
    ParenthesisCloseToken,

    BracketOpenToken,
    BracketCloseToken,

    LessThanToken,
    GreaterThanToken,

    AtSignToken,
    CommaToken,
    DotToken,
    ColonToken,
    SemicolonToken,
    EqualsToken,
    HookToken,

    IntegerLiteralToken,
    FloatLiteralToken,
    StringLiteralToken,

    IdentifierToken,

    TrueKeyword,
    FalseKeyword,

    VoidKeyword,
    NullKeyword,
    IntKeyword,
    LongKeyword,
    StringKeyword,
    BooleanKeyword,
    FloatKeyword,
    DoubleKeyword,
    BytesKeyword,

    ArrayKeyword,
    MapKeyword,

    EnumKeyword,
    FixedKeyword,
    RecordKeyword,
    ErrorKeyword,
    ProtocolKeyword,

    DecimalKeyword,
    DateKeyword,
    TimeMsKeyword,
    TimestampMsKeyword,
    LocalTimestampMsKeyword,
    UuidKeyword,

    ThrowsKeyword,
    OneWayKeyword,

    SchemaKeyword,
    ImportKeyword,

    LineBreakTrivia,
    SingleLineCommentTrivia,
    MultiLineCommentTrivia,
    WhiteSpaceTrivia,
    InvalidTextTrivia,

    CompilationUnit,

    SimpleName,
    QualifiedName,

    VoidType,
    NullType,
    IntType,
    LongType,
    StringType,
    BooleanType,
    FloatType,
    DoubleType,
    BytesType,

    ArrayType,
    MapType,

    NamedType,

    Annotation,

    EnumDeclaration,
    RecordDeclaration,
    ErrorDeclaration,
    FixedDeclaration,
    ProtocolDeclaration,

    FieldDeclaration,
    DefaultValueClause,
    JsonValue,
    MessageDeclaration,
    ParameterDeclaration,
    OneWayClause,
    ThrowsErrorClause,
}
