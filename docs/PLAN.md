# Documentation site implementation plan

## Purpose and defaults

- Build a GitHub Pages user guide for developers consuming AvroSourceGenerator.
- Default to the latest published stable guide, archive stable releases, and maintain one rolling preview guide.
- Use .NET SDK 10.0.100 or later and `net10.0` for the standard quick start. Do not recommend .NET 8.
- Keep the README useful on GitHub and NuGet with a complete, concise first example.

## Site and content

- Use Jekyll and Just the Docs with pinned dependencies. Build each complete guide under `docs/versions/<version>/` independently so its navigation and search remain scoped.
- Provide Home, Getting started, Schemas and imports, Generated models, Avro libraries, Configuration, Kafka and Schema Registry, and Troubleshooting for each version. Integrate preview features into their relevant pages.
- Identify the version in the URL and page header; include a selector that preserves the topic where available and otherwise falls back to Home.
- Describe modern C# models, multiple runtime targets, modular contracts, independent generation, imports, and references on Home in a factual tone for developers. Assume an existing project in Getting started.
- Distinguish references declared earlier in a JSON protocol from external-file references needing deferred binding.
- Show practical typed Kafka producer/consumer round trips instead of manual binary serialization.
- Include code copying, heading links, branding, and GitHub/NuGet links.
- Provide a compact sun/moon toggle with light and dark states, initialized from the system preference until a manual choice is saved. Place it at the far right, after accessible GitHub and NuGet icon links. Improve inline-code contrast in both palettes; system theme selection remains available without JavaScript.
- Keep implementation internals and generated implementation API references out of the user guide.

## README and publishing

- Retain badges, introduction, requirements, installation, one schema/model example, documentation and sample links, contributing, and license.
- Preserve useful README anchors and use absolute site links for the packaged NuGet README.
- Build and validate documentation on pull requests; deploy successful builds on `main` or manual dispatch using the official Pages actions.
- Target `https://sadicangel.github.io/avro-source-generator/`, including correct asset paths under that prefix.
- Enable GitHub Actions as the repository's Pages source during rollout. Preserve generator build and release workflows.

## Verification

- Build the site and check internal links, anchors, images, and repository-prefixed paths using Ruby and the existing Bundler setup; no Python runtime is required.
- Review desktop/mobile layouts, search isolation, version switching, code copying, and the light/dark toggle, including system initialization and keyboard operation.
- Run both published versions' quick starts and strict/deferred protocol cases using SDK 10; compile both Kafka integrations and run their round trips when local Kafka/Schema Registry infrastructure is available.
- Review stable behavior against its release source; distinguish repository sample prerequisites from package consumption.
- Review the final diff and run `git diff --check`; follow repository-required smoke checks before pushing or opening a PR.

## Initial boundaries

English only; stable snapshots plus one rolling preview guide. No custom domain or changes to generator behavior, package versions, or source-project target frameworks.
