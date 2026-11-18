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
| `AVROSG1001` | Trailing JSON content | Supply one root JSON value per file. |
| `AVROSG1002` | JSON value expected | Supply a JSON object, array, number, string, boolean, or null. Quote string values. |
| `AVROSG1003` | Invalid character | Remove the highlighted character. Between tokens, use spaces, tabs, carriage returns, or line feeds. |
| `AVROSG1004` | Invalid escape sequence | Correct the escape in the quoted string. |
| `AVROSG1005` | Invalid number | Correct the number's spelling. For example, write `1` instead of `01` in JSON. |
| `AVROSG1006` | Unterminated string | Close the string with a quotation mark on the same line. |
| `AVROSG1007` | Unexpected token | Check the expected token in the diagnostic. Remove a trailing comma when it precedes a closing delimiter. |
| `AVROSG1008` | JSON comment not allowed | Remove the highlighted comment from the JSON schema or protocol. |
| `AVROSG1009` | Unpaired surrogate in JSON property name | Replace the unpaired UTF-16 surrogate with valid Unicode text. |
| `AVROSG1010` | JSON nesting limit exceeded | Reduce the number of nested JSON arrays and objects. |
| `AVROSG2000` | Unterminated IDL documentation comment | Close the comment with `*/`. |
| `AVROSG2001` | Unterminated IDL block comment | Close the comment with `*/`. |
| `AVROSG2002` | Unterminated IDL verbatim identifier | Close the identifier with a backtick. |
| `AVROSG2003` | Misplaced IDL annotation | Move the annotation to a declaration or type that accepts it. |
| `AVROSG2004` | Misplaced IDL documentation | Place the comment before a declaration that accepts documentation. |
| `AVROSG3000` | Schema expected | Supply a schema at the document root or in each union branch. For example, `["null", false]` reports this error at `false`. |
| `AVROSG3001` | Protocol expected | Supply a JSON protocol with a `protocol` property in `.avpr`. |
| `AVROSG3003` | Invalid IDL document form | Supply one main `schema` directive with schema declarations, or exactly one `protocol` block. |
| `AVROSG3004` | Protocol in an IDL schema document | Move the protocol to its own document, or use schema declarations after the `schema` directive. |
| `AVROSG3005` | Repeated IDL directive | Keep only one `schema` directive and at most one top-level `namespace` directive; identical repetitions are also errors. Each repeated directive is reported. |
| `AVROSG4000` | Required property missing | Add the named property to the highlighted object. For example, a JSON schema object requires `type`. |
| `AVROSG4016` | Invalid field order | Supply a non-blank string, use null, or omit the value. |
| `AVROSG5000` | Malformed type reference in JSON or IDL | Use a valid Avro name such as `com.example.Order` wherever a type is referenced, including inside a union. |
| `AVROSG5001` | Duplicate schema name | Keep one declaration per fully qualified Avro name, or review [duplicate resolution]({{ '/configuration/#duplicate-resolution' | relative_url }}). |
| `AVROSG5002` | Missing named references | Check names, namespaces, imports, and [reference resolution]({{ '/configuration/#reference-resolution' | relative_url }}). |
| `AVROSG5004` | Repeated record or error field | Remove or rename the repeated field within that declaration. |
| `AVROSG5005` | Repeated enum symbol | Keep each symbol only once within that enum. |
| `AVROSG5006` | Repeated Avro union branch | Keep at most one branch per primitive, array, or map type, and one per named-type fullname. Logical annotations and C# mappings do not distinguish Avro types. |
| `AVROSG5007` | Immediately nested union | Move the inner branches into the enclosing union, resolving any duplicates. |
| `AVROSG6000` | Import cycle | Remove the circular dependency between imports. |
| `AVROSG6001` | Import extension mismatch | Match `schema`, `protocol`, or `idl` to the target file's extension. |
| `AVROSG6002` | Missing import | Include the target as an `AdditionalFiles` item and correct its path relative to the importing file. |
| `AVROSG6003` | Wrong imported definition kind | Ensure the target contains the kind declared by the import. |
| `AVROSG6004` | Unused import | Remove the import or reference a type it supplies. |
| `AVROSG7000` | No supported runtime detected | Install a runtime or select `AvroSourceGeneratorAvroLibrary=None`. |
| `AVROSG7001` | Multiple supported runtimes detected | Select the intended runtime explicitly. |
| `AVROSG7002` | Chr support removed in 0.8.0 | Migrate to `Apache`, explicitly select `None`, or remain on 0.7.x. See [migration guidance]({{ '/libraries/#migrating-from-chravro' | relative_url }}). |

## An imported file exists but cannot be found

Imports are resolved only among supplied inputs. Including `order.avdl` does not automatically include the files it imports. Supply all imported files, including transitive dependencies, as `AdditionalFiles`.

Also check the path relative to the importing file and use the correct import kind. See [Imports]({{ '/schemas/#imports' | relative_url }}).

## Some files generate and others do not

Independent valid inputs still generate after another input fails. Files that depend on invalid declarations are suppressed. Fix the original schema or import diagnostic before adding missing C# types manually.

## Generated models compile but serialization fails

Confirm that the generator target matches your runtime and serializer. Test the writer and reader with the same schema first, then check schema evolution, logical type support, union branches, and your message format.

Apache schema parsing and serialization can support different sets of logical types. See [Avro libraries]({{ '/libraries/#logical-type-compatibility' | relative_url }}) and [Kafka and Schema Registry]({{ '/kafka/' | relative_url }}).

## Reporting a problem

Open a [GitHub issue](https://github.com/sadicangel/avro-source-generator/issues) with the generator version, SDK version, selected runtime and version, relevant project configuration, a minimal schema, and the complete diagnostic or exception. Remove credentials and private data from the reproduction.
