namespace AvroSourceGenerator.Syntax;

internal enum SyntaxKind
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
    QuestionMarkToken,

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

    DecimalKeyword,
    DateKeyword,
    TimeMsKeyword,
    TimestampMsKeyword,
    LocalTimestampMsKeyword,
    UuidKeyword,

    ArrayKeyword,
    MapKeyword,
    UnionKeyword,

    EnumKeyword,
    FixedKeyword,
    RecordKeyword,
    ErrorKeyword,
    ProtocolKeyword,

    NamespaceKeyword,
    SchemaKeyword,
    ImportKeyword,
    IdlKeyword,
    TypedefKeyword,

    ThrowsKeyword,
    OneWayKeyword,

    LineBreakTrivia,
    SingleLineCommentTrivia,
    MultiLineCommentTrivia,
    WhiteSpaceTrivia,
    InvalidTextTrivia,
    DocumentationTrivia,

    Document,

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
    UnionType,
    OptionalType,
    AnnotatedType,
    NamedType,
    LogicalType,

    NamespaceDirective,
    SchemaDirective,
    ImportDirective,

    Documentation,

    AnnotationName,
    NamespaceAnnotation,
    AliasesAnnotation,
    OrderAnnotation,
    LogicalTypeAnnotation,
    CustomAnnotation,

    EnumDeclaration,
    EnumDefaultClause,
    RecordDeclaration,
    ErrorDeclaration,
    FixedDeclaration,
    ProtocolDeclaration,

    FieldDeclaration,
    DefaultValueClause,
    MessageDeclaration,
    ParameterDeclaration,
    OneWayClause,
    ThrowsErrorClause,

    JsonNull,
    JsonBoolean,
    JsonNumber,
    JsonString,
    JsonArray,
    JsonObject,
    JsonProperty,
}
