---
name: upgrade-jevdotnet-package
description: Upgrade JevDotNet dependencies and prepare a verified package-version change without publishing it.
---

# Upgrade the JevDotNet package

Prepare a package upgrade in `X:\JevDotNet`. Preserve unrelated work and do not publish, push, or create a release unless separately requested.

## Inspect

Read `src/JevDotNet/JevDotNet.csproj`, `Directory.Build.props`, `JevDotNet.slnx`, `CHANGELOG.md`, the newest Git history, and the working-tree status. Determine the requested package version; ask only if it is missing or ambiguous.

Query NuGet for current dependency versions. Do not cross a dependency's major-version boundary unless the user explicitly requests it. Keep development-only dependencies out of the shipped package.

## Update and verify

1. Update applicable package references and set `<Version>` in the library project.
2. Convert `## Unreleased` in `CHANGELOG.md` into `## Version <version> (<date>)`, preserving its bullets, and add a fresh empty `## Unreleased` above it.
3. Run `dotnet format JevDotNet.slnx --verify-no-changes` and `dotnet build JevDotNet.slnx --configuration Release`.
4. Run the live tests only when the user requested them and the shared `TypeSafeApiKey` user secret is available: `dotnet test tests/JevDotNet.Tests/JevDotNet.Tests.csproj --configuration Release --no-build`.
5. Pack to `artifacts` and inspect both `.nupkg` and `.snupkg`. Confirm development, test, and secrets assemblies are absent.
6. Inspect the final diff and secret-scan tracked files.

Stop with a report of versions, validation results, artifacts, and changed files. Do not commit or publish without an explicit follow-up instruction.
