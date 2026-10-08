using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class FieldPossibleTypesTests
{
    [Fact]
    public void PossibleTypes_UseCanonicalNonNullUnionBranches()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            """
            {
              "type": "record",
              "name": "Record",
              "fields": [
                { "name": "ordinary", "type": "string" },
                { "name": "nullableSingle", "type": ["null", "string"] },
                { "name": "multi", "type": ["string", "int"] },
                { "name": "nullableMulti", "type": ["long", "null", "boolean"] },
                { "name": "onlyNull", "type": "null" }
              ]
            }
            """);

        var record = Assert.IsType<RecordSchema>(Assert.Single(parsed.Declarations));
        var fields = record.Fields.ToDictionary(field => field.Name);

        AssertFieldMetadata(fields["ordinary"], allowsNull: false, hasNullableAnnotation: false, SchemaType.String);
        AssertFieldMetadata(fields["nullableSingle"], allowsNull: true, hasNullableAnnotation: true, SchemaType.String);
        AssertFieldMetadata(fields["multi"], allowsNull: false, hasNullableAnnotation: false, SchemaType.Int, SchemaType.String);
        AssertFieldMetadata(fields["nullableMulti"], allowsNull: true, hasNullableAnnotation: true, SchemaType.Boolean, SchemaType.Long);
        AssertFieldMetadata(fields["onlyNull"], allowsNull: true, hasNullableAnnotation: false, SchemaType.Null);

        Assert.Equal(
            [SchemaType.Long, SchemaType.Null, SchemaType.Boolean],
            Assert.IsType<UnionSchema>(fields["nullableMulti"].Type).Schemas.Select(schema => schema.Type));
    }

    [Fact]
    public void PossibleTypes_AvdlOptionalSyntaxExcludesNullBranch()
    {
        var parsed = SchemaCompilerTestHelpers.ParseSource(
            """
            schema OptionalRecord;

            record OptionalRecord {
                string? value;
            }
            """);

        var record = Assert.IsType<RecordSchema>(Assert.Single(parsed.Declarations));
        var field = Assert.Single(record.Fields);

        AssertFieldMetadata(field, allowsNull: true, hasNullableAnnotation: true, SchemaType.String);
    }

    [Fact]
    public void NullableAnnotation_RespectsNullableReferenceTypeOptionIndependentlyOfAllowsNull()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            """
            {
              "type": "record",
              "name": "Record",
              "fields": [
                { "name": "reference", "type": ["null", "string"] },
                { "name": "value", "type": ["null", "int"] }
              ]
            }
            """,
            useNullableReferenceTypes: false);

        var record = Assert.IsType<RecordSchema>(Assert.Single(parsed.Declarations));
        var fields = record.Fields.ToDictionary(field => field.Name);

        AssertFieldMetadata(fields["reference"], allowsNull: true, hasNullableAnnotation: false, SchemaType.String);
        AssertFieldMetadata(fields["value"], allowsNull: true, hasNullableAnnotation: true, SchemaType.Int);
    }

    [Fact]
    public void NullableAnnotation_AvdlOptionalSyntaxRespectsNullableReferenceTypeOption()
    {
        var parsed = SchemaCompilerTestHelpers.ParseSource(
            """
            schema OptionalRecord;

            record OptionalRecord {
                string? reference;
                int? value;
            }
            """,
            useNullableReferenceTypes: false);

        var record = Assert.IsType<RecordSchema>(Assert.Single(parsed.Declarations));
        var fields = record.Fields.ToDictionary(field => field.Name);

        AssertFieldMetadata(fields["reference"], allowsNull: true, hasNullableAnnotation: false, SchemaType.String);
        AssertFieldMetadata(fields["value"], allowsNull: true, hasNullableAnnotation: true, SchemaType.Int);
    }

    private static void AssertFieldMetadata(Field field, bool allowsNull, bool hasNullableAnnotation, params SchemaType[] possibleTypes)
    {
        Assert.Equal(possibleTypes, field.PossibleTypes.Select(schema => schema.Type));
        Assert.Equal(allowsNull, field.AllowsNull);
        Assert.Equal(hasNullableAnnotation, field.Type.CSharpName.HasNullableAnnotation);
    }
}
