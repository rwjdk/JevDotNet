# AI Rules for JevDotNet

## Repository map

- `src/JevDotNet/` contains the public NuGet library.
- `tests/JevDotNet.Tests/` contains deterministic and live xUnit tests.
- `development/Sandbox/` is the console application for ad-hoc testing.
- `development/Secrets/` loads the shared TypeSafe AI API key from .NET user secrets.
- `.agents/skills/` contains the package upgrade, release, and consumer-upgrade workflows.

## Public API rules

- All public types, constructors, methods, properties, and record parameters in the NuGet library must have XML documentation.
- Only document public APIs; do not add XML documentation to internal, private, or protected implementation details.
- Public asynchronous methods must use the `Async` suffix, even when a requested name omits it.
- Refer to an ID as an "ID", never as a "unique identifier" or "Id" in documentation.
- Capitalize the Jev primitive names Choice, Score, and Noul in documentation.
- Preserve the public namespaces `JevDotNet` and `JevDotNet.Models`.

## Code style and build rules

- Follow `src/JevDotNet/.editorconfig` for library code.
- Nullable reference types and implicit usings are enabled.
- Warnings and code-style diagnostics are errors. Do not suppress a warning when the source can reasonably be corrected.
- Preserve unrelated user changes and inspect the working tree before editing.
- Add new projects to `JevDotNet.slnx`.
- Development and test projects must remain non-packable.

## Testing and secrets

- Keep deterministic tests for reflection, serialization, and mapping behavior.
- Live tests use the `TypeSafeApiKey` environment variable or the shared user-secrets store configured by `development/Secrets/Secrets.csproj`.
- Never place API keys in source, command output, test data, environment files, logs, or commits.
- Do not run live tests unless the user explicitly requests them or the active repository skill requires them.
- GitHub Actions runs deterministic tests on every build and live tests on trusted builds using the `TYPESAFE_API_KEY` repository secret. Fork and Dependabot pull requests run deterministic tests only.
- Do not run the Sandbox unless the user explicitly requests an ad-hoc API call.

## Verification

For ordinary source changes, run:

```powershell
dotnet format .\JevDotNet.slnx --verify-no-changes
dotnet build .\JevDotNet.slnx --configuration Release
```

When manual tests are authorized, run:

```powershell
dotnet test .\tests\JevDotNet.Tests\JevDotNet.Tests.csproj --configuration Release --no-build
```

For package-affecting changes, also pack to `artifacts`, inspect both the `.nupkg` and `.snupkg`, and confirm that development, test, and secrets assemblies are absent.

## README and changelog

- Write `README.md` for NuGet consumers, not repository maintainers.
- Keep README examples compilable against the current public namespaces and API.
- Keep API keys out of examples; use environment variables or .NET user secrets.
- Keep a top-level `## Unreleased` section in `CHANGELOG.md`.
- Add an entry under `## Unreleased` in the same change whenever behavior, packaging,
  or user-facing documentation changes meaningfully. Skip only changes with no
  user-visible effect, such as formatting or internal-only refactors.
- Released sections must include the version and date, and releases must be separated by `---`.

## Package and release workflows

- Use `.agents/skills/upgrade-jevdotnet-package` to prepare dependency or package-version upgrades.
- Use `.agents/skills/release-jevdotnet` for an explicitly authorized GitHub and NuGet release.
- Use `.agents/skills/upgrade-jevdotnet-consumers` to upgrade repositories directly under `X:\` that consume JevDotNet.
- The Build and Test workflow attempts to publish after successful tests on pushes to `main` and on manual dispatch. It uses `dotnet nuget push --skip-duplicate`, so an already published package version is skipped; a new version is published. Branch pushes run tests without publishing.
- Never publish to NuGet, create a GitHub release, tag, force-push, or modify consumer repositories without explicit authorization for that action.
