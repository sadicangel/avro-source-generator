---
title: Schemas and imports
nav_order: 3
---

# Schemas and imports

## Include your Avro files

Add the files you want to compile as `AdditionalFiles` in your project:

```xml
<ItemGroup>
  <AdditionalFiles Include="schemas/**/*.avsc" />
  <AdditionalFiles Include="schemas/**/*.avpr" />
  <AdditionalFiles Include="schemas/**/*.avdl" />
</ItemGroup>
```

`**/` includes subdirectories. Choose globs that include each input only once.

| Extension | Contents | Use it for |
|:---|:---|:---|
| `.avsc` | JSON schema | A record, enum, fixed type, or another Avro schema |
| `.avpr` | JSON protocol | Related types and protocol messages |
| `.avdl` | Avro IDL | Human-readable definitions, protocols, and imports |

The extension selects the parser. Renaming a protocol to `.avsc` does not turn it into a schema. Protocol generation provides model and protocol declarations; it does not set up an RPC transport for your application.

## JSON schemas

Save a record as `schemas/customer.avsc`:

```json
{
  "type": "record",
  "name": "Customer",
  "namespace": "example",
  "fields": [
    { "name": "Id", "type": "string" }
  ]
}
```

The generated C# type is `example.Customer`. Named references use Avro names and namespaces, so use the fully qualified name when the target is in another namespace.

## JSON protocols

A protocol can contain several related type declarations. In this self-contained `schemas/orders.avpr`, `Customer` is declared before `Order` references it, so the default `Strict` mode works:

```json
{% include examples/standalone-protocol.avpr %}
```

Use this as an alternative to the separate `customer.avsc` above. Including both declarations of `example.Customer` would produce a duplicate-definition error.

### Referencing a type from another file

If `Customer` stays in `customer.avsc`, the protocol can contain only the referring type:

```json
{% include examples/external-protocol.avpr %}
```

Include both files as `AdditionalFiles` and enable deferred reference resolution:

```xml
<PropertyGroup>
  <AvroSourceGeneratorReferenceResolution>Deferred</AvroSourceGeneratorReferenceResolution>
</PropertyGroup>
```

{: .note }
`Strict` does not make declarations in unrelated input files visible just because those files were supplied earlier. External JSON references need `Deferred`. IDL imports provide explicit visibility through the importing file's transitive import closure.

## Avro IDL

The equivalent standalone IDL schema in `schemas/customer.avdl` is:

```text
namespace example;

schema Customer;

/** A customer in our application. */
record Customer {
    string Id;
}
```

For related records, IDL also supports protocol blocks:

```text
@namespace("example")
protocol Orders {
    record Order {
        string Id;
        string? Comment = null;
    }
}
```

## Imports

Suppose your project contains:

```text
schemas/
  customer.avsc
  order.avdl
```

`order.avdl` can import the JSON schema relative to its own directory:

```text
@namespace("example")
protocol Orders {
    import schema "customer.avsc";

    record Order {
        string Id;
        example.Customer Customer;
    }
}
```

Use `import schema` for `.avsc`, `import protocol` for `.avpr`, and `import idl` for `.avdl`.

{: .warning }
Every imported file, including transitive imports, must also be supplied as an `AdditionalFiles` item. The generator resolves imports among those supplied inputs; it does not read extra files from disk automatically.

Imports are relative to the importing file, not the project directory. Missing files, mismatched import kinds, and cycles are errors. Unused imports produce warnings.

## References between files

The default `Strict` mode requires a named reference to resolve to an earlier declaration in the same file or to a declaration available through the file's transitive IDL imports.

If separate JSON schemas reference one another, choose `Deferred`:

```xml
<PropertyGroup>
  <AvroSourceGeneratorReferenceResolution>Deferred</AvroSourceGeneratorReferenceResolution>
</PropertyGroup>
```

This resolves references after all supported inputs have been read. Declared import paths are still validated.

## Duplicate definitions and errors

Prefer one declaration for each fully qualified Avro name. See [Duplicate resolution]({{ '/configuration/#duplicate-resolution' | relative_url }}) when intentional duplicates cannot be avoided.

An invalid input produces build diagnostics. Independent valid inputs still generate models, while files depending on an invalid schema are suppressed until it is fixed. See [Troubleshooting]({{ '/troubleshooting/' | relative_url }}) for common diagnostic IDs.
