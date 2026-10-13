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

Logical types use library-specific mappings. For example, `timestamp-millis` maps to `DateTime` with Apache.Avro and `DateTimeOffset` with Chr.Avro. Choose your runtime before writing application code around logical types. See [runtime compatibility]({{ '/libraries/#logical-type-compatibility' | relative_url }}) for serialization limitations.

## Nullability and unions

An Avro union with `null` and one other type represents an optional value:

```json
{ "name": "Email", "type": ["null", "string"], "default": null }
```

In the quick-start project this becomes `string? Email`. Supply Avro defaults in the schema when your data contract requires them; C# nullability alone does not change the Avro schema.

Unions of multiple record types can use generated `I…Variant` interfaces. Other unions can be represented as `object`. Code consuming an object union must handle its possible runtime types, for example with pattern matching.

{: .note }
Upcoming **0.8.0** adds opt-in [C# union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union). See the [0.8.0 preview guide]({{ site.repository_baseurl }}/preview/generated-models/#native-c-unions) for setup and limitations.

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
