namespace AvroSourceGenerator.Schemas;

public enum SchemaTypeTag
{
    /// <summary>
    /// No value.
    /// </summary>
    Null,
    /// <summary>
    /// A binary value.
    /// </summary>
    Boolean,
    /// <summary>
    /// A 32-bit signed integer.
    /// </summary>
    Int,
    /// <summary>
    /// A 64-bit signed integer.
    /// </summary>
    Long,
    /// <summary>
    /// A single precision (32-bit) IEEE 754 floating-point number.
    /// </summary>
    Float,
    /// <summary>
    /// A double precision (64-bit) IEEE 754 floating-point number.
    /// </summary>
    Double,
    /// <summary>
    /// A sequence of 8-bit unsigned bytes.
    /// </summary>
    Bytes,
    /// <summary>
    /// An unicode character sequence.
    /// </summary>
    String,
    /// <summary>
    /// A logical collection of fields.
    /// </summary>
    Record,
    /// <summary>
    /// An enumeration.
    /// </summary>
    Enum,
    /// <summary>
    /// An array of values.
    /// </summary>
    Array,
    /// <summary>
    /// A map of values with string keys.
    /// </summary>
    Map,
    /// <summary>
    /// A union.
    /// </summary>
    Union,
    /// <summary>
    /// A fixed-length byte string.
    /// </summary>
    Fixed,
    /// <summary>
    /// A protocol error.
    /// </summary>
    Error,
    /// <summary>
    /// A logical type.
    /// </summary>
    Logical,
}
