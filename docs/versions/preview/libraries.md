---
title: Avro libraries
nav_order: 5
---

# Avro libraries

Avro Source Generator creates models. A runtime library handles reading and writing Avro data. Choose the runtime that fits your application's existing serializers and integrations.

| Choice | What the generator provides | Typical use |
|:---|:---|:---|
| Apache.Avro | Models implementing the Apache specific-record contract, plus Avro schema definitions | Apache serializers and Confluent's Apache-based Avro serializer |
| None | Models without runtime-specific members | Applications that manage serialization separately |

## Automatic detection

By default, the generator detects Apache.Avro from the project's references. With Apache.Avro installed, no extra generator setting is needed.

If none is detected, models still generate, but you receive `AVROSG6000`. If Chr.Avro is detected, automatic selection reports error `AVROSG6002` and falls back to standalone models, including when Apache.Avro is also referenced. Select `Apache` or `None` explicitly to continue.

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

## Standalone models

You can omit the runtime library and explicitly choose `None`:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAvroLibrary>None</AvroSourceGeneratorAvroLibrary>
</PropertyGroup>
```

This suppresses the no-library warning and generates library-neutral models. It does not supply an Avro serializer. Installing a runtime later can change generated model shapes, including logical and fixed types.

## Migrating from Chr.Avro

**Unreleased breaking change for 0.8.0:** this removal is not included in the preview package currently listed by this guide.

[Chr.Avro was archived on July 2, 2026](https://github.com/ch-robinson/dotnet-avro), and its maintainers recommend switching to Apache.Avro. Avro Source Generator 0.8.0 removes the Chr target, generated serializer/deserializer builder cases, and the Chr Kafka sample.

Projects configured with `AvroSourceGeneratorAvroLibrary=Chr` receive error `AVROSG6002`. Standalone fallback models still generate, but the error prevents a successful build. The CLI rejects `--library Chr` with migration advice before writing files.

To migrate to Apache.Avro:

1. Replace Chr runtime and Kafka packages with Apache.Avro and, for Kafka, Confluent.SchemaRegistry.Serdes.Avro. Follow [Apache setup](#apacheavro) and [Kafka configuration]({{ '/kafka/#apacheavro' | relative_url }}).
2. Set `AvroSourceGeneratorAvroLibrary` to `Apache` (or use `--library Apache` with the CLI).
3. Remove registrations of generated Chr serializer/deserializer builder cases and update code that uses Chr schema APIs.
4. Review regenerated models and exercise every union branch and logical type in serialization tests.

The runtime change can alter C# types: timestamps use `DateTime` instead of Chr's `DateTimeOffset`, dates use `DateTime` instead of `DateOnly`, and time values use `TimeSpan` instead of `TimeOnly`. Decimal values use `AvroDecimal`; fixed types become generated Apache fixed models instead of `byte[]`. Apache errors also inherit from its runtime error type. Existing Avro schemas remain your data contract, but application code and serializer configuration need review.

Alternatively, explicitly select `None` for standalone models and provide your own serialization integration. This does not preserve Chr-specific generated members or guarantee Chr serializer compatibility. Existing Chr applications can remain on Avro Source Generator 0.7.x; the [0.7.0 guide]({{ site.repository_baseurl }}/0.7.0/libraries/#chravro) retains the original setup.

## Logical type compatibility

Runtime support matters as well as successful generation:

- Unknown logical annotations remain in Apache schema JSON, and fields use the underlying C# type. Apache.Avro 1.12.1 rejects unknown logical types when parsing the generated schema; 1.12.2 accepts them.
- Apache.Avro 1.12.2 parses fixed-backed decimal schemas, but its specific-record writer fails when serializing those decimal values.
- Mixed unions can have runtime-specific serialization limitations. Test your actual branch types with your selected serializer, especially when the generated property is `object`.

The generator does not validate the runtime library's version. Check that your runtime and Schema Registry accept the schemas and logical types used by your application.
