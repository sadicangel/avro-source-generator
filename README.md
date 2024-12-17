# Avro Source Generator

Avro Source Generator is a .NET source generator that generates C# code from Avro schemas.

Produces models that use modern C# features, such as nullable reference types, init-only and required properties, and other.

## Prerequisites

- .NET SDK 6.0 or later

## Usage

To use the Avro Source Generator in your project, add a reference to the `AvroSourceGenerator` package in your `.csproj` file:
```pwsh
dotnet add package AvroSourceGenerator
```
You can mark the package as `PrivateAssets="all"` to prevent projects referencing yours from getting a reference to `AvroSourceGenerator`. Additionally, use `ExcludeAssets="runtime"` to ensure that `AvroSourceGenerator.Attributes` is not copied to the build output, since it is not required at runtime.

```xml
<PackageReference Include="AvroSourceGenerator" Version="*" PrivateAssets="all" ExcludeAssets="runtime" />
```

To use the generator, apply the `Avro` attribute to a partial class or record and provide the corresponding Avro schema.
```cs
using AvroSourceGenerator;

namespace SchemaNamespace;

[Avro("""
{
  "type": "record",
  "namespace": "SchemaNamespace",
  "name": "User",
  "fields": [
    {
      "name": "Name",
      "type": "string"
    },
    {
      "name": "Email",
      "type": [
          "null",
          "string"
      ]
    },
    {
      "name": "CreatedAt",
      "type": {
          "type": "long",
          "logicalType": "timestamp-millis"
      }
    }
  ]
}
""")]
public partial class User;
```

This will generate the C# files containing the types defined in the Avro schema.

Here is an example of a generated class for the `User` schema defined above:
```cs
// <auto-generated/>
#pragma warning disable CS8618, CS8633, CS8714, CS8775
#nullable enable
namespace SchemaNamespace
{
    [global::System.CodeDom.Compiler.GeneratedCode("AvroSourceGenerator", "1.0.0.0")]
    public partial class User : global::Avro.Specific.ISpecificRecord
    {
        public required string Name { get; init; }
        public string? Email { get; init; }
        public required global::System.DateTime CreatedAt { get; init; }
    
        public global::Avro.Schema Schema { get => User._SCHEMA; }
        public static readonly global::Avro.Schema _SCHEMA = global::Avro.Schema.Parse("""
        {
          "type": "record",
          "namespace": "SchemaNamespace",
          "name": "User",
          "fields": [
            {
              "name": "Name",
              "type": "string"
            },
            {
              "name": "Email",
              "type": [
                "null",
                "string"
              ]
            },
            {
              "name": "CreatedAt",
              "type": {
                "type": "long",
                "logicalType": "timestamp-millis"
              }
            }
          ]
        }
        """);
    
        public object? Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return this.Name;
                case 1: return this.Email;
                case 2: return this.CreatedAt;
                default: throw new global::Avro.AvroRuntimeException($"Bad index {fieldPos} in Get()");
            }
        }
        
        public void Put(int fieldPos, object? fieldValue)
        {
            switch (fieldPos)
            {
                case 0:
                    Set_Name(this, (string)fieldValue!); break;
                    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_Name")]
                    extern static void Set_Name(User obj, string value);
                case 1:
                    Set_Email(this, (string?)fieldValue!); break;
                    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_Email")]
                    extern static void Set_Email(User obj, string? value);
                case 2:
                    Set_CreatedAt(this, (global::System.DateTime)fieldValue!); break;
                    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_CreatedAt")]
                    extern static void Set_CreatedAt(User obj, global::System.DateTime value);
                default:
                    throw new global::Avro.AvroRuntimeException($"Bad index {fieldPos} in Put()");
            }
        }
    }

}
#nullable restore
#pragma warning restore CS8618, CS8633, CS8714, CS8775
```

## Records

The generated code will match the declaration type of the class annotated with the `Avro` attribute. To generate records, simply declare the type as `record` or `record class`. For example:

```cs
using AvroSourceGenerator;

namespace SchemaNamespace;

[Avro(Schemas.User)]
public partial record User;
```
> [!NOTE]  
> This feature is limited to Avro `record` schemas. Code generated from `fixed` and `error` schemas will always be a regular class, as those types must inherit from non-record types.

## C# namespaces

By default, the generated code adheres to the namespaces specified in the Avro schema. To override this, set `UseCSharpNamespace` to `true`. This will ensure that all generated types share the same C# namespace as the class annotated with the `Avro` attribute. For example:

```cs
using AvroSourceGenerator;

namespace ExampleNamespace;

[Avro(Schemas.User, UseCSharpNamespace = true)]
public partial record User;
```
> [!WARNING]  
> This may break deserialization for tools that lookup types by the schema namespace.

## Access modifier

Generated code will have the same access modifier as the class annotated with the `Avro` attribute.  
To declare all types as internal, just declare the class as internal:
```cs
using AvroSourceGenerator;

namespace ExampleNamespace;

[Avro(Schemas.User, UseCSharpNamespace = true)]
internal partial record User;
```

## Language features

It is possible to specify the C# language features to be used in the generated code. This is useful when you need to ensure compatibility with older versions of C#.  
For example, setting `LanguageFeatures = LanguageFeatures.CSharp7_3` will generate code using only features that are compatible with C# 7.3 (eg: no nullable reference types, no records, etc).  
If left unspecified, will generate code that uses features compatible with the language version of the project.

Example for generating code compatible with C# 7.3 (suitable for .NET Framework and .NET Standard 2.0):
```cs
using AvroSourceGenerator;

namespace SchemaNamespace;

[Avro(Schemas.User, LanguageFeatures = LanguageFeatures.CSharp7_3)]
public partial record User;
```

It is also possible to enable or disable specific features.

Example for using latest features but exclude required properties:
```cs
using AvroSourceGenerator;

namespace SchemaNamespace;

[Avro(Schemas.User, LanguageFeatures = LanguageFeatures.Latest & ~LanguageFeatures.RequiredProperties)]
public partial record User;
```

## Limitations
In C#, `enum` types cannot be declared as `partial`, so annotating them with the `Avro` attribute is not beneficial, as it is not possible to generate additional implementation. Therefore, the root Avro schema must be a `record`, `error`, or `fixed` schema, but not an `enum`. To generate an `enum`, you can wrap the schema in a `record`. For example:
```cs
using AvroSourceGenerator;

namespace SchemaNamespace;

[Avro("""
{
  "type": "record",
  "name": "EnumWrapper",
  "fields": [
    {
      "name": "EnumWrapperField",
      "type": {
        "type": "enum",
        "name": "Suit",
        "namespace": "EnumSchemaNamespace",
        "symbols": [
          "Hearts",
          "Diamonds",
          "Clubs",
          "Spades"
        ]
      }
    }
  ]
}
""")]
public partial record EnumWrapper;
```
This will generate a file named `Suit.Avro.g.cs` with the following definition:
```cs
// <auto-generated/>
namespace EnumSchemaNamespace
{
    [global::System.CodeDom.Compiler.GeneratedCode("AvroSourceGenerator", "1.0.0.0")]
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades,
    }
}
```

## Contributing

Contributions are welcome! If you have any ideas, suggestions, or bug reports, please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
