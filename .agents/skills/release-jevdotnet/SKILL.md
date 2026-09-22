---
name: release-jevdotnet
description: Commit, publish, and create a GitHub and NuGet release for an already prepared JevDotNet version.
---

# Release JevDotNet

Release only an already prepared and verified version. External publication requires the user's explicit instruction for this release.

## Preconditions

Inspect the package version, matching `CHANGELOG.md` entry, Git status, current branch/upstream, and configured remotes. Build, run the live tests with the shared user secret, pack, and inspect package contents. Stop on any warning, failure, version mismatch, missing secret, or unrelated staged change.

## Commit and push

When authorized, stage only release files, commit with `Release JevDotNet <version>`, and push the current branch normally. Never force-push. The normal GitHub CI workflow publishes the package when the version is new and skips an already-published version. Wait for the build and publish jobs and require both to pass.

## Publish

Before pushing the release commit, confirm that the version does not already exist on NuGet.org and that the exact verified package artifact is being used. Do not invoke a separate publishing workflow or publish from the local machine.

Create a GitHub release whose tag is `<version>`, title matches the changelog heading, and body contains that entry's bullets. Mark prerelease versions appropriately. Do not publish a draft form or other incomplete release.

Report the commit, tag, GitHub release URL, NuGet package URL, and final repository status.
