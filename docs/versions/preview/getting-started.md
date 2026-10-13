---
title: Getting started
nav_order: 2
---

# Getting started

Add Avro Source Generator to your existing .NET project, include a schema, and use its generated model. Run the commands below from the directory containing your `.csproj`.

## 1. Check your tools

Use **.NET SDK 10.0.100 or later**. Visual Studio users should use **Visual Studio 2026 18.0 or later** with .NET 10 support.

```bash
dotnet --version
```

The SDK runs the generator during compilation. The SDK requirement is separate from your application's target framework; the examples here use .NET 10. Keep generated language features and runtime dependencies compatible with your application's target.

## 2. Install the packages

This example uses Apache.Avro:

```bash
dotnet add package AvroSourceGenerator --version {{ site.docs_version.package_version }}
dotnet add package Apache.Avro --version 1.12.2
```

If your project already uses Chr.Avro, follow [the Chr setup]({{ '/libraries/#chravro' | relative_url }}) instead.

Update the generator package reference created by installation with `PrivateAssets` and `ExcludeAssets`, and add the `AdditionalFiles` entry. Keep a single reference to each package:

```xml
{% include examples/PackageReferences.xml %}
```

`PrivateAssets` keeps the generator out of your package's public dependencies. `ExcludeAssets` prevents it from becoming a runtime reference. `AdditionalFiles` selects the schemas to compile.

## 3. Add a schema

Create a `schemas` directory in your project and save this as `schemas/user.avsc`:

```json
{% include examples/user.avsc %}
```

The namespace is `example`, the generated type is `User`, and the properties are `Name` and `Email`.

## 4. Use the generated model

Build your application:

```bash
dotnet build
```

Use the generated type wherever your application needs it:

```csharp
{% include examples/Program.cs %}
```

For example, in a console application this prints `Ada <ada@example.com>`. You do not create `User.cs` yourself or run an extra generation command.

## What next?

- [Produce and consume the model with Kafka]({{ '/kafka/' | relative_url }}).
- [Add protocols, IDL, and imports]({{ '/schemas/' | relative_url }}).
- [Customize generated models]({{ '/configuration/' | relative_url }}) or [extend partial types]({{ '/generated-models/#extending-generated-types' | relative_url }}).
- If a type does not appear, follow [Troubleshooting]({{ '/troubleshooting/#no-generated-types' | relative_url }}).
