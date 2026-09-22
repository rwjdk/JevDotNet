---
name: upgrade-jevdotnet-consumers
description: Find projects directly under X:\ that consume JevDotNet, upgrade them to a requested released version, and verify each repository independently.
---

# Upgrade JevDotNet consumers

Use this only after the requested JevDotNet version is available from the consumer's configured NuGet source.

## Discover

Inspect immediate child directories of `X:\` that are Git repositories. Exclude `X:\JevDotNet`. Read each candidate's repository instructions before changing it. Include only repositories with an actual `PackageReference` or central `PackageVersion` for `JevDotNet`.

For every included repository, record its branch, upstream, remotes, working-tree status, version-management pattern, and build entry point. Preserve all unrelated changes.

## Upgrade and verify

1. Update only the `JevDotNet` package version, preserving central package management and formatting.
2. Restore and build the widest repository-native solution or documented build entry point.
3. Run tests only when repository instructions or the user require them. Never assume access to unrelated live credentials.
4. If verification fails, leave that repository uncommitted, record the exact failure, and continue with other consumers.
5. For a successful clean change, commit and push only when the user authorized consumer repository mutations. Never stage unrelated edits or force-push.

Report every repository inspected, inclusion decision, old and new version, verification result, commit/push result, and pre-existing changes left untouched.
