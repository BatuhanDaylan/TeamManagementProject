# 01-update-nugets: update-nugets

Inventory and update NuGet packages per-project (non-breaking upgrades first)

## Research Findings

### Projects Affected
- TeamManagement.Api\TeamManagement.Api.csproj — several packages have available patch updates
- TeamManagement.Infrastructure\TeamManagement.Infrastructure.csproj — Newtonsoft.Json has a patch update

### Packages to Update
| Project | Package | Current | Latest | Action |
|---------|---------|---------|--------|--------|
| TeamManagement.Api | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.10 | 10.0.11 | Update (patch)
| TeamManagement.Api | Microsoft.AspNetCore.OpenApi | 10.0.10 | 10.0.11 | Defer (major jump for Microsoft.OpenApi indicated; investigate after patch updates)
| TeamManagement.Api | Microsoft.EntityFrameworkCore.Design | 10.0.10 | 10.0.11 | Update (patch)
| TeamManagement.Api | Microsoft.OpenApi | 2.7.5 | 3.10.0 | Defer (major version bump — review breaking changes)
| TeamManagement.Infrastructure | Newtonsoft.Json | 13.0.1 | 13.0.4 | Update (patch)

### Decision
- Apply patch-level updates now (10.0.10 → 10.0.11, Newtonsoft.Json 13.0.1 → 13.0.4).
- Defer large-major upgrades (Microsoft.OpenApi 2.x → 3.x and Microsoft.OpenApi package jump) for a follow-up analysis to assess breaking changes.

### Next steps
1. Apply patch updates to the projects listed as 'Update (patch)'.
2. Build affected projects (targeted builds) and iterate on fixes if any errors/warnings appear.
3. Re-evaluate deferred major upgrades separately and plan their migration if needed.
