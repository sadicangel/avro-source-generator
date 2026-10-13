---
title: Configuration
nav_order: 6
---

# Configuration

Configure the generator with MSBuild properties in your consuming project's `.csproj`, or in a shared `Directory.Build.props` when several projects should use the same settings.

| Property | Default | Supported values |
|:---|:---|:---|
| `AvroSourceGeneratorAccessModifier` | `public` | `public`, `internal` |
| `AvroSourceGeneratorRecordDeclaration` | Records when supported | `record`, `class` |
| `AvroSourceGeneratorLanguageFeatures` | Selected from the project's C# version | `CSharp7_3`, `CSharp8`, `CSharp9`, `CSharp10`, `CSharp11`, `CSharp12`, `CSharp13`, `Latest` |
| `AvroSourceGeneratorAvroLibrary` | `Auto` | `Auto`, `None`, `Apache`, `Chr` |
| `AvroSourceGeneratorDuplicateResolution` | `Error` | `Error`, `Ignore` |
| `AvroSourceGeneratorReferenceResolution` | `Strict` | `Strict`, `Deferred` |

## Access modifier

Generate types that are visible only within your assembly:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAccessModifier>internal</AvroSourceGeneratorAccessModifier>
</PropertyGroup>
```

## Records or classes

Use classes when that better fits your application:

```xml
<PropertyGroup>
  <AvroSourceGeneratorRecordDeclaration>class</AvroSourceGeneratorRecordDeclaration>
</PropertyGroup>
```

By default, records are used when supported by the selected feature set and runtime target. Some schema kinds require classes because of inheritance constraints.

## Language features

Usually, leave this unset and let the generator follow your project's C# version. You can choose a reduced set of generated features without changing your application's SDK:

```xml
<PropertyGroup>
  <AvroSourceGeneratorLanguageFeatures>CSharp10</AvroSourceGeneratorLanguageFeatures>
</PropertyGroup>
```

For example, this avoids generating `required` properties. A feature-set value controls generated syntax; it does not install a compiler or change the project's `LangVersion`. Choose a feature set your compiler supports.

## Library selection

Force Apache.Avro when both supported libraries are referenced:

```xml
<PropertyGroup>
  <AvroSourceGeneratorAvroLibrary>Apache</AvroSourceGeneratorAvroLibrary>
</PropertyGroup>
```

Choose `None` to generate standalone models without a no-library warning. See [Avro libraries]({{ '/libraries/' | relative_url }}) for installation and detection behavior.

## Duplicate resolution

Duplicates normally produce errors. When duplicate declarations across files are intentional, you can keep the first declaration in the supplied file order:

```xml
<PropertyGroup>
  <AvroSourceGeneratorDuplicateResolution>Ignore</AvroSourceGeneratorDuplicateResolution>
</PropertyGroup>
```

{: .warning }
With `Ignore`, input order determines the selected definition. Use this only when that choice is safe. Duplicate declarations within a single file and equivalent source paths remain errors.

The default `Error` reports the duplicate and suppresses affected files. Independent valid files still generate.

## Reference resolution

Choose deferred binding for related types declared in separate JSON input files:

```xml
<PropertyGroup>
  <AvroSourceGeneratorReferenceResolution>Deferred</AvroSourceGeneratorReferenceResolution>
</PropertyGroup>
```

`Strict` requires an earlier declaration in the same file or a declaration in its transitive IDL import closure. `Deferred` resolves references after all supported inputs are read. Import paths are validated in either mode. See [Schemas and imports]({{ '/schemas/' | relative_url }}) for examples.
