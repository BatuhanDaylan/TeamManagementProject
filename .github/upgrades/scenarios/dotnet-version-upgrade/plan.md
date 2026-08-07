# Upgrade Plan — .NET 10

## Objective
Migrate solution to target framework net10.0 and update NuGet packages to compatible versions.

## Tasks
1. 01-update-nugets — Inventory and update NuGet packages per-project (non-breaking upgrades first)
2. 02-build-and-fix — Build solution and fix compilation/warning issues introduced by updates
3. 03-run-tests — Run unit/integration tests and fix failures
4. 04-finalize-commits — Final verification, cleanups, and commit changes

"Done when": All projects target net10.0, all intended package updates applied, solution builds without errors, tests pass.
