namespace AvroSourceGenerator.Schemas;

public enum SchemaType
{
    Null,
    Boolean,
    Int,
    Long,
    Float,
    Double,
    Bytes,
    String,

    Array,
    Map,
    Union,

    Enum,
    Fixed,
    Record,
    Error,

    Protocol,

    Logical,

    Reference,
    UnionType,
}
