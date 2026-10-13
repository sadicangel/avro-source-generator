---
title: Troubleshooting
nav_order: 8
---

# Troubleshooting

Start with the first Avro diagnostic in the build output. Fixing the declaration or import that owns the error often resolves later missing-type errors too.

## No generated types

Check the following:

1. Run `dotnet --version` from your application directory. Use SDK **10.0.100 or later** and check whether a `global.json` selects another SDK.
2. Confirm `AvroSourceGenerator` is installed in the project that contains the schemas. Keep analyzer and build assets enabled; use the [quick-start package reference]({{ '/getting-started/' | relative_url }}).
3. Include the files as `AdditionalFiles`, not just `Content` or `None`, and check that the glob matches their actual location.
4. Match the extension to the contents: `.avsc`, `.avpr`, or `.avdl`.
5. Run `dotnet build` and inspect Avro diagnostics before looking at C# missing-type errors.
6. Check the Avro namespace and field capitalization in your C# code.

If the command-line build succeeds but the editor still shows stale errors, reload the project or restart the editor after checking its .NET tooling support. See [generated-source inspection]({{ '/generated-models/#inspecting-generated-source' | relative_url }}) to examine the actual output.

## Common diagnostics

| ID | Meaning | What to do |
|:---|:---|:---|
| `AVROSG0001` | Unsupported input extension | Use `.avsc`, `.avpr`, or `.avdl`. |
| `AVROSG0002` | Empty input | Add a complete schema, protocol, or IDL definition. |
| `AVROSG0003` | Duplicate source path | Remove overlapping `AdditionalFiles` entries or equivalent paths. |
| `AVROSG0004` | Duplicate schema name | Keep one declaration per fully qualified Avro name, or review [duplicate resolution]({{ '/configuration/#duplicate-resolution' | relative_url }}). |
| `AVROSG0005` | Missing named references | Check names, namespaces, imports, and [reference resolution]({{ '/configuration/#reference-resolution' | relative_url }}). |
| `AVROSG1000` | Invalid JSON | Correct the JSON syntax at the reported location. |
| `AVROSG2000` | Schema expected | Use a schema in `.avsc`; protocols belong in `.avpr`. |
| `AVROSG2001` | Protocol expected | Supply a JSON protocol with a `protocol` property in `.avpr`. |
| `AVROSG5000` | Import cycle | Remove the circular dependency between imports. |
| `AVROSG5001` | Import extension mismatch | Match `schema`, `protocol`, or `idl` to the target file's extension. |
| `AVROSG5002` | Missing import | Include the target as an `AdditionalFiles` item and correct its path relative to the importing file. |
| `AVROSG5003` | Wrong imported definition kind | Ensure the target contains the kind declared by the import. |
| `AVROSG5004` | Unused import | Remove the import or reference a type it supplies. |
| `AVROSG6000` | No supported runtime detected | Install a runtime or select `AvroSourceGeneratorAvroLibrary=None`. |
| `AVROSG6001` | Multiple runtimes detected | Explicitly select `Apache` or `Chr`. |

Other schema and IDL diagnostics identify the invalid property, declaration, or token. Use the location and message in the build output to correct that input.

## An imported file exists but cannot be found

Imports are resolved only among supplied inputs. Including `order.avdl` does not automatically include the files it imports. Supply all imported files, including transitive dependencies, as `AdditionalFiles`.

Also check the path relative to the importing file and use the correct import kind. See [Imports]({{ '/schemas/#imports' | relative_url }}).

## Some files generate and others do not

Independent valid inputs still generate after another input fails. Files that depend on invalid declarations are suppressed. Fix the original schema or import diagnostic before adding missing C# types manually.

## Generated models compile but serialization fails

Confirm that the generator target matches your runtime and serializer. Test the writer and reader with the same schema first, then check schema evolution, logical type support, union branches, and your message format.

Apache schema parsing and serialization can support different sets of logical types. Chr mixed unions may need custom builder cases. See [Avro libraries]({{ '/libraries/#logical-type-compatibility' | relative_url }}) and [Kafka and Schema Registry]({{ '/kafka/' | relative_url }}).

## Reporting a problem

Open a [GitHub issue](https://github.com/sadicangel/avro-source-generator/issues) with the generator version, SDK version, selected runtime and version, relevant project configuration, a minimal schema, and the complete diagnostic or exception. Remove credentials and private data from the reproduction.
