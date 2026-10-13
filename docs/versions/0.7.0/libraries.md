---
title: Avro libraries
nav_order: 5
---

# Avro libraries

Avro Source Generator creates models. A runtime library handles reading and writing Avro data. Choose the runtime that fits your application's existing serializers and integrations.

| Choice | What the generator provides | Typical use |
|:---|:---|:---|
| Apache.Avro | Models implementing the Apache specific-record contract, plus Avro schema definitions | Apache serializers and Confluent's Apache-based Avro serializer |
| Chr.Avro | Models and supporting schema/integration members for Chr.Avro | Chr binary serializers and Chr.Avro.Confluent |
| None | Models without runtime-specific members | Applications that manage serialization separately |

## Automatic detection

By default, the generator detects supported Avro libraries from the project's references. With one supported library, no extra generator setting is needed.

If none is detected, models still generate, but you receive `AVROSG6000`. If multiple supported libraries are detected, `AVROSG6001` asks you to choose one; automatic detection falls back to library-neutral generation.

## Apache.Avro

Install the runtime alongside the generator:

```bash
dotnet add package Apache.Avro --version 1.12.2
```

To select it explicitly:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAvroLibrary>Apache</AvroSourceGeneratorAvroLibrary>
</PropertyGroup>
```

Generated records implement `Avro.Specific.ISpecificRecord`. The generator supplies the schema and the field access methods used by Apache's serializers. See [Kafka configuration]({{ '/kafka/#apacheavro' | relative_url }}).

## Chr.Avro

Install the runtime:

```bash
dotnet add package Chr.Avro --version 10.13.1
```

Select the Chr target explicitly if your application also references Apache.Avro:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAvroLibrary>Chr</AvroSourceGeneratorAvroLibrary>
</PropertyGroup>
```

See [Chr Kafka configuration]({{ '/kafka/#chravro' | relative_url }}) for a complete producer/consumer example.

## Standalone models

You can omit both runtime libraries and explicitly choose `None`:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAvroLibrary>None</AvroSourceGeneratorAvroLibrary>
</PropertyGroup>
```

This suppresses the no-library warning and generates library-neutral models. It does not supply an Avro serializer. Installing a runtime later can change generated model shapes, including logical and fixed types.

## Logical type compatibility

Runtime support matters as well as successful generation:

- Unknown logical annotations remain in Apache schema JSON, and fields use the underlying C# type. Apache.Avro 1.12.1 rejects unknown logical types when parsing the generated schema; 1.12.2 accepts them.
- Apache.Avro 1.12.2 parses fixed-backed decimal schemas, but its specific-record writer fails when serializing those decimal values.
- Mixed unions can have runtime-specific serialization limitations. Test your actual branch types with your selected serializer, especially when the generated property is `object`.

The generator does not validate the runtime library's version. Check that your runtime and Schema Registry accept the schemas and logical types used by your application.
