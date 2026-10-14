# Avro Source Generator

[![NuGet stable](https://img.shields.io/nuget/v/AvroSourceGenerator?style=flat&logo=nuget&logoColor=white&label=stable&color=blue)](https://www.nuget.org/packages/AvroSourceGenerator)
[![NuGet preview](https://img.shields.io/nuget/vpre/AvroSourceGenerator?style=flat&logo=nuget&logoColor=white&label=preview&color=orange)](https://www.nuget.org/packages/AvroSourceGenerator#versions-body-tab)
[![NuGet downloads](https://img.shields.io/nuget/dt/AvroSourceGenerator?style=flat&logo=nuget&logoColor=white&label=downloads&color=blueviolet)](https://www.nuget.org/packages/AvroSourceGenerator)

[![CI pipeline](https://img.shields.io/github/actions/workflow/status/sadicangel/avro-source-generator/build.yml?branch=main&event=push&style=flat&logo=githubactions&logoColor=white&label=CI)](https://github.com/sadicangel/avro-source-generator/actions/workflows/build.yml?query=branch%3Amain+event%3Apush)
[![Documentation](https://img.shields.io/badge/docs-online-blue?style=flat&logo=githubpages&logoColor=white)](https://sadicangel.github.io/avro-source-generator/)
[![Minimum .NET SDK: 10.0.100](https://img.shields.io/badge/.NET%20SDK-10.0.100%2B-512BD4?style=flat&logo=dotnet&logoColor=white)](#prerequisites)
[![License: MIT](https://img.shields.io/github/license/sadicangel/avro-source-generator?style=flat&label=license&color=green)](LICENSE)

![Avro Source Generator](https://raw.githubusercontent.com/sadicangel/avro-source-generator/refs/heads/main/icon.png)

**Avro Source Generator** generates strongly typed C# models from [Avro](https://avro.apache.org/) schemas (`.avsc`), protocols (`.avpr`), and IDL files (`.avdl`) during your normal build.

- Integrate with **Apache.Avro**, **Chr.Avro**, or generate standalone models.
- Use partial records or classes, nullable properties, and modern C# features.
- Share named types across inputs and resolve IDL imports.
- Keep models up to date without a separate code-generation command.

**[Check out the documentation](https://sadicangel.github.io/avro-source-generator/)** — setup, configuration, Kafka, and troubleshooting.

## Prerequisites

Use **.NET SDK 10.0.100 or later**. Visual Studio users should use **Visual Studio 2026 18.0 or later** with .NET 10 support.

The upcoming 0.8.0 preview removes Chr.Avro support. See the [migration guide](https://sadicangel.github.io/avro-source-generator/preview/libraries/#migrating-from-chravro); this removal is not yet included in published packages.

The quick start below uses stable **AvroSourceGenerator 0.7.0** and targets `net10.0`. [0.8.0 preview features](https://sadicangel.github.io/avro-source-generator/preview/generated-models/#native-c-unions), including native C# unions, have separate requirements.

## Installation & Basic Usage

From your existing project directory, install the generator and Apache runtime:

```bash
dotnet add package AvroSourceGenerator --version 0.7.0
dotnet add package Apache.Avro --version 1.12.2
```

In your `.csproj`, update the installed generator reference to a build-only dependency and include your schemas (keep one reference per package):

```xml
<ItemGroup>
  <PackageReference Include="AvroSourceGenerator" Version="0.7.0"
                    PrivateAssets="all" ExcludeAssets="runtime" />
  <PackageReference Include="Apache.Avro" Version="1.12.2" />
  <AdditionalFiles Include="schemas/*.avsc" />
</ItemGroup>
```

Create `schemas/user.avsc`:

```json
{
  "type": "record",
  "name": "User",
  "namespace": "example",
  "fields": [
    { "name": "Name", "type": "string" },
    { "name": "Email", "type": ["null", "string"], "default": null }
  ]
}
```

Use the generated model in your application:

```csharp
using example;

var user = new User
{
    Name = "Ada",
    Email = "ada@example.com",
};

Console.WriteLine($"{user.Name} <{user.Email}>");
```

Run `dotnet build`. The generator creates `example.User` during compilation. The code above prints `Ada <ada@example.com>`.

You can also use [Chr.Avro or standalone models](https://sadicangel.github.io/avro-source-generator/0.7.0/libraries/), and include [protocols, IDL, and imports](https://sadicangel.github.io/avro-source-generator/0.7.0/schemas/).

## Example Output

The quick-start model has this public shape (abridged):

```csharp
namespace example;

public partial record User
{
    public required string Name { get; init; }
    public string? Email { get; init; }
}
```

The Apache target also generates the `ISpecificRecord` implementation. Read about [generated models](https://sadicangel.github.io/avro-source-generator/0.7.0/generated-models/) and [extending partial types](https://sadicangel.github.io/avro-source-generator/0.7.0/generated-models/#extending-generated-types).

## Documentation

| Topic | Guide |
|:---|:---|
| First generated model | [Getting started](https://sadicangel.github.io/avro-source-generator/0.7.0/getting-started/) |
| Input formats, imports, and shared types | [Schemas and imports](https://sadicangel.github.io/avro-source-generator/0.7.0/schemas/) |
| Types, nullability, and partial declarations | [Generated models](https://sadicangel.github.io/avro-source-generator/0.7.0/generated-models/) |
| Apache.Avro, Chr.Avro, or no runtime | [Avro libraries](https://sadicangel.github.io/avro-source-generator/0.7.0/libraries/) |
| MSBuild properties and defaults | [Configuration](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/) |
| Typed producers, consumers, and schema registration | [Kafka and Schema Registry](https://sadicangel.github.io/avro-source-generator/0.7.0/kafka/) |
| Build diagnostics and common fixes | [Troubleshooting](https://sadicangel.github.io/avro-source-generator/0.7.0/troubleshooting/) |
| Preview packages and native unions | [0.8.0 preview guide](https://sadicangel.github.io/avro-source-generator/preview/generated-models/#native-c-unions) |

## Command-line tool

An on-demand .NET tool, `AvroSourceGenerator.Cli` (`avro-sg compile`), is in development. See the [preview CLI guide](https://sadicangel.github.io/avro-source-generator/preview/cli/); the CLI is not yet available in the documented package releases.

## Samples

- [Apache.Avro](https://github.com/sadicangel/avro-source-generator/tree/v0.7.0/samples/AvroSourceGenerator.ApacheAvro) — model generation and IDL.
- [Confluent Kafka](https://github.com/sadicangel/avro-source-generator/tree/v0.7.0/samples/AvroSourceGenerator.ConfluentKafka) — Apache serializers with Kafka and Schema Registry.
- [Chr.Avro Kafka](https://github.com/sadicangel/avro-source-generator/tree/v0.7.0/samples/AvroSourceGenerator.ChrAvroKafka) — Chr serializers with Kafka and Schema Registry.

These links target stable examples. Samples in the current `main` checkout target .NET 11 and use the development SDK selected by [global.json](global.json). Kafka samples also require Docker. See [running repository samples](https://sadicangel.github.io/avro-source-generator/0.7.0/kafka/#complete-examples-with-local-containers) for details.

## Extending Generated Types

Add members in your own matching partial record or class; generated enums cannot be extended with partial declarations. See [the partial-type example](https://sadicangel.github.io/avro-source-generator/0.7.0/generated-models/#extending-generated-types).

## Configuration

Set MSBuild properties in your `.csproj`. The [configuration guide](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/) documents defaults and examples.

<!-- Keep existing configuration deep links useful after moving the detailed guide. -->
<a id="access-modifier"></a>
<a id="record-vs-class-generation"></a>
<a id="language-features"></a>
<a id="avro-library-selection"></a>
<a id="no-supported-package-detected"></a>
<a id="multiple-supported-packages-detected"></a>
<a id="apacheavro-logical-type-compatibility"></a>
<a id="duplicate-resolution"></a>
<a id="reference-resolution"></a>

- [Access modifier](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/#access-modifier), [records or classes](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/#records-or-classes), and [language features](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/#language-features).
- [Library selection](https://sadicangel.github.io/avro-source-generator/0.7.0/libraries/) and [logical type compatibility](https://sadicangel.github.io/avro-source-generator/0.7.0/libraries/#logical-type-compatibility).
- [Duplicate resolution](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/#duplicate-resolution) and [reference resolution](https://sadicangel.github.io/avro-source-generator/0.7.0/configuration/#reference-resolution).

## Contributing

Bug reports, feature suggestions, and documentation improvements are welcome through [GitHub issues](https://github.com/sadicangel/avro-source-generator/issues) and pull requests.

See [repository guidance](AGENTS.md) for build and test commands, [package compatibility checks](tests/AvroSourceGenerator.PackageCompatibility/README.md) for SDK validation, and [documentation maintenance](docs/README.md) for site preview and publishing instructions.

## License

Licensed under the [MIT License](https://github.com/sadicangel/avro-source-generator/blob/main/LICENSE).
