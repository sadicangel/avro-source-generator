---
title: Generated models
nav_order: 4
---

# Generated models

The generated public model shape depends on your schema, C# language version, configuration, and selected Avro library.

For the [quick-start schema]({{ '/getting-started/' | relative_url }}), the important members look like this:

```csharp
namespace example;

public partial record User
{
    public required string Name { get; init; }
    public string? Email { get; init; }
}
```

This is an abridged illustration. The Apache target also generates the members needed by `Avro.Specific.ISpecificRecord`; you do not write them yourself.

## Records, classes, and properties

Records are used by default when supported. Choose [class generation]({{ '/configuration/#records-or-classes' | relative_url }}) if your application needs classes. Some schema kinds require classes because of runtime inheritance constraints, including Apache fixed types and errors.

The generator uses the consuming project's language version to select features such as nullable reference types, `init` accessors, and `required` properties. Configure a reduced [language feature set]({{ '/configuration/#language-features' | relative_url }}) when needed.

Avro field names determine the generated property names. For example, a field named `displayName` remains `displayName`; it is not automatically renamed to `DisplayName`.

## Common type mappings

| Avro type | C# model type |
|:---|:---|
| `boolean` | `bool` |
| `int` | `int` |
| `long` | `long` |
| `float` | `float` |
| `double` | `double` |
| `string` | `string` |
| `bytes` | `byte[]` |
| `enum` | A generated enum |
| `record` | A generated record or class |
| `array` | A typed collection |
| `map` | A string-keyed dictionary |
| `fixed` | An Apache fixed model for Apache.Avro; **`byte[]`** otherwise |

Logical types use library-specific mappings. For example, `timestamp-millis` maps to `DateTime` with Apache.Avro and `DateTimeOffset` with standalone models. Choose your runtime before writing application code around logical types. See [runtime compatibility]({{ '/libraries/#logical-type-compatibility' | relative_url }}) for serialization limitations.

## Schema defaults

The generator preserves field and protocol-parameter defaults as JSON in emitted Avro schemas. Set property values explicitly when constructing a C# model. Non-nullable properties follow the usual `required` rules when supported.

Opt-in C# initialization is tracked in [issue #191](https://github.com/sadicangel/avro-source-generator/issues/191).

## Nullability and unions

An Avro union with `null` and one other type represents an optional value:

```json
{ "name": "Email", "type": ["null", "string"], "default": null }
```

In the quick-start project this becomes `string? Email`. Supply Avro defaults in the schema when your data contract requires them; C# nullability alone does not change the Avro schema.

### Supported unions

The examples below show the C# type of a field named `Content` on a record named `Envelope`. Native unions require a separate [opt-in](#native-c-unions).

| Avro field type | Default C# type | With native unions enabled |
|:---|:---|:---|
| `["int"]` | `int` | `int` |
| `["null", "int"]` | `int?` | `int?` |
| `["int", "string"]` | `object` | `EnvelopeContentUnion` |
| `["null", "int", "string"]` | `object?` | `EnvelopeContentUnion?` |
| Two inline record definitions | `IEnvelopeContentVariant` | `EnvelopeContentUnion` |
| `["Email", "Sms"]`, with both records declared earlier | `object` | `EnvelopeContentUnion` |

By default, a union with two or more non-null branches gets a variant interface if each branch is an inline record or error definition, or an inline fixed definition with its own generated C# type. Each branch type implements the interface. Other unions with multiple non-null branches use `object`. This includes unions of named references and mixtures with primitives, enums, arrays, or maps. Use C# pattern matching to handle the possible runtime types of an `object` property. The `?` annotation on reference types depends on the enabled C# language features.

The generator creates variant interfaces and native union types for record and error fields. A union used as an array item or a map value uses the branch type for a single non-null branch, with nullability when applicable. Multiple non-null branches use `object` or `object?`, even with the native union opt-in. For example, an array with `"items": ["int", "string"]` generates `List<object>`.

Avro forbids duplicate branches and a union directly inside another union. Distinct named types can share a schema kind, such as two named fixed types. An array or map can also contain a union in its item or value schema. See [common diagnostics]({{ '/troubleshooting/#common-diagnostics' | relative_url }}).

## Native C# unions

Set `AvroSourceGeneratorNativeUnions` to `true` to use [C# union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union) for eligible fields. The default is `false`, including when the property is omitted. Use **.NET 11 or later** and **C# 15 or later** with a compiler that supports union types. If your compiler requires preview language features for unions, set `LangVersion` to `preview` as shown below.

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>preview</LangVersion>
  <AvroSourceGeneratorNativeUnions>true</AvroSourceGeneratorNativeUnions>
</PropertyGroup>
```

If you also set `AvroSourceGeneratorLanguageFeatures`, choose `CSharp15` or `All`. An eligible field has at least two distinct non-null C# branch types. The cases can include primitives, named types, arrays, and maps. An Avro `null` branch makes the union property nullable.

{: .note }
`AvroSourceGeneratorNativeUnions` ships with 0.8.0. The preview package linked from this guide uses `AvroSourceGeneratorPreviewFeatures=Unions` for the same opt-in.

Supported feature-set values include `All`, `CSharp7_3`, `CSharp8`, `CSharp9`, `CSharp10`, `CSharp11`, `CSharp12`, `CSharp13`, `CSharp14`, and `CSharp15`. The selected set is limited by the consuming project's language version.

### Limitations

- Each non-null Avro branch needs a distinct C# type to have its own case. For example, `bytes` and a `fixed` type that both map to `byte[]` share a C# case, so the generated model cannot distinguish them.
- Apache.Avro 1.12.2 returns enum ordinals when reading unions. This prevents correct enum deserialization and can make enum branches indistinguishable from each other or an `int` branch. The fix is tracked in [Apache Avro PR #4031](https://github.com/apache/avro/pull/4031).
- Test the serializer with every branch in your schema before relying on the feature in an application.

## Extending generated types

Add your own partial declaration in a normal `.cs` file. Match the generated namespace and declaration kind:

```csharp
namespace example;

public partial record User
{
    public string DisplayLabel => Email is null ? Name : $"{Name} <{Email}>";
}
```

Generated records, classes, and variant interfaces are partial. Enums cannot be extended with partial declarations. Avoid declaring members with the same names as generated properties or runtime integration members.

## Inspecting generated source

In Visual Studio, expand the generator beneath your project's analyzer dependencies to inspect its generated files. To write copies to disk for inspection, use standard compiler properties:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Keep this output under the intermediate directory so it is not compiled again as ordinary project source. Edit your schemas, configuration, or partial declarations to change behavior; generated files are replaced on subsequent builds.
