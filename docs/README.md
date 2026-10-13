# Maintaining the documentation

The user guide uses Jekyll and Just the Docs. Each directory under `versions/` is a complete guide, including its own example snippets. Shared layouts, assets, and controls live at the root of `docs/`. `PLAN.md` records the current implementation plan. Maintainer files are not published.

## Preview locally

Install Ruby 3.4 with Bundler, then run from the repository root:

```powershell
bundle install --gemfile docs/Gemfile
bundle exec --gemfile docs/Gemfile ruby tools/docs/build.rb --serve
```

Open `http://127.0.0.1:4000/avro-source-generator/`. The repository base path is intentional; previewing only at `/` can hide broken asset URLs.

Use `bundle exec --gemfile docs/Gemfile ruby tools/docs/build.rb` for a build without a server. Rebuild and restart after editing; the preview does not watch files.

The builder assembles and builds each version independently into `artifacts/docs-site/<version>/`. Navigation and search contain only that version's pages. It generates a root redirect to the latest stable guide and redirects for the earlier unversioned topic URLs. Output cleanup is restricted to a resolved subdirectory of `artifacts/`.

## Validate changes

```powershell
bundle exec --gemfile docs/Gemfile ruby tools/docs/test_check_links.rb
bundle exec --gemfile docs/Gemfile ruby tools/docs/check_links.rb artifacts/docs-site --baseurl /avro-source-generator
pwsh -File tools/docs/Verify-Examples.ps1
git diff --check
```

Link validation uses Ruby and the gems installed with the site, including Nokogiri for HTML parsing. The checks include version selector destinations and search index isolation. The workflow reuses its existing Ruby/Bundler setup. `Verify-Examples.ps1` uses SDK 10 to run each version's quick start, compile its Apache and Chr Kafka examples, and confirm protocol reference resolution: internal references succeed under `Strict`, external references fail under `Strict`, and external references succeed under `Deferred`. Consumers are isolated from repository build settings. Kafka examples require running services for an actual round trip; compilation does not prove connectivity or broker behavior.

Before delivery, inspect wide and narrow layouts, search for a property or diagnostic in each version, copy a code block, and switch versions on a nested topic. Check the sun/moon toggle using clicks, Space, and Enter, including inline code in text and tables. It offers light and dark only; before a manual choice, it follows the OS preference, including live changes. Manual choices persist across pages and versions. The toggle sits at the far right, after the GitHub and NuGet icon links, which retain accessible names and hover titles. Without JavaScript, the toggle is hidden, system theme selection still works, and all guides remain available by URL.

## Update content and versions

- `_data/versions.json` identifies each guide, package version, and release source tag. The first entry with channel `Stable` is the default. Confirm published versions on NuGet; a local Git tag alone does not prove publication.
- On a stable release, copy the complete matching preview guide into a new `versions/<release>/` directory, including its `_includes/examples/`. Add its metadata, mark the previous stable entry `Archived`, and update the rolling preview guide and README. Retain old directories and URLs.
- Published stable guide content and snippets are snapshots. Correct errors there when needed, but keep evolving feature work in `versions/preview/`. Shared theme and accessibility fixes may apply to every version.
- Keep standard setup on .NET SDK 10.0.100 or later. Explain higher requirements for optional C# union types in that version's generated-model and configuration pages.
- Each version's `_includes/examples/` feeds both its rendered guide and the consumer verifier. Update snippets and their package references together.
- Use `relative_url` for links and assets within a version. Use `site.repository_baseurl` for deliberate cross-version links, and absolute GitHub/NuGet URLs for external links. `site.docs_version.source_ref` pins source links to the selected release.
- Keep version-specific defaults and generated model behavior in the corresponding guide. The version label and selector supply context; avoid repeating version disclaimers throughout the content.
- `docs/releases/` is reserved for release artifacts and excluded from the site. Build output and local dependency caches are ignored.

## GitHub Pages rollout

The `docs` workflow validates pull requests and publishes successful documentation builds on `main`; it also supports manual dispatch. Build jobs have read-only repository permissions. Only the deployment job receives Pages and OIDC write permissions, and it uses the `github-pages` environment.

The repository is configured to use **GitHub Actions** as its Pages source. For a fork or a new repository, select that source in **Settings → Pages → Build and deployment**. Once the documentation changes are on `main`, check the deployment output for the published URL:

`https://sadicangel.github.io/avro-source-generator/`

Check the homepage, a nested guide, search, and both themes after the first deployment. No custom domain is configured.
