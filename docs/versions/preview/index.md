---
title: Home
nav_order: 1
description: Usage and configuration of the Avro C# source generator.
---

# Avro Source Generator

Avro Source Generator is an incremental C# source generator for Avro schemas, protocols, and IDL. It generates models during compilation and integrates with your editor and `dotnet build`.

[Getting started]({{ '/getting-started/' | relative_url }}) · [Kafka and Schema Registry]({{ '/kafka/' | relative_url }})

## Generated C# models

Generated models support partial records or classes, nullable reference types, `init` accessors, and `required` properties, depending on your project's C# version. Add application-specific members through partial declarations.

Optional [C# 15 union types]({{ '/generated-models/#native-c-unions' | relative_url }}) represent multi-branch Avro unions with typed cases and compiler-checked pattern matching.

```csharp
var user = new User
{
    Name = "Ada",
    Email = "ada@example.com",
};
```

## Runtime targets

The generator supports **Apache.Avro** and **library-neutral models**. The Apache target includes runtime integration members. The upcoming 0.8.0 removes Chr.Avro support. See [migration guidance]({{ '/libraries/#migrating-from-chravro' | relative_url }}).

## Schema handling and build behavior

- **Three input formats:** JSON schemas (`.avsc`), JSON protocols (`.avpr`), and human-readable Avro IDL (`.avdl`).
- **References:** share named types through IDL imports or resolve references between supplied files with deferred binding.
- **Independent generation:** a broken schema does not stop unrelated valid inputs from generating. Diagnostics identify the invalid input.
- **Incremental builds:** the generator runs in the editor and as part of compilation.
- **MSBuild configuration:** control visibility, records or classes, language features, and reference resolution.

## Documentation

| Topic | Guide |
|:---|:---|
| Add the generator to an existing project | [Getting started]({{ '/getting-started/' | relative_url }}) |
| Share definitions and import schemas | [Schemas and imports]({{ '/schemas/' | relative_url }}) |
| Understand properties, types, and unions | [Generated models]({{ '/generated-models/' | relative_url }}) |
| Choose Apache.Avro or standalone models | [Avro libraries]({{ '/libraries/' | relative_url }}) |
| Customize generated code | [Configuration]({{ '/configuration/' | relative_url }}) |
| Produce and consume generated models | [Kafka and Schema Registry]({{ '/kafka/' | relative_url }}) |
| Fix a warning or error | [Troubleshooting]({{ '/troubleshooting/' | relative_url }}) |
