# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
  - [Binding Redirect Configuration](#binding-redirect-configuration)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [TeamManagement.Api\TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj)
  - [TeamManagement.Application\TeamManagement.Application.csproj](#teammanagementapplicationteammanagementapplicationcsproj)
  - [TeamManagement.Domain\TeamManagement.Domain.csproj](#teammanagementdomainteammanagementdomaincsproj)
  - [TeamManagement.Infrastructure\TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 4 | 0 require upgrade |
| Total NuGet Packages | 12 | All compatible |
| Total Code Files | 73 |  |
| Total Code Files with Incidents | 0 |  |
| Total Lines of Code | 3930 |  |
| Total Number of Issues | 0 |  |
| Estimated LOC to modify | 0+ | at least 0,0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Binding Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| [TeamManagement.Api\TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj) | net10.0 | ✅ None | 0 | 0 | 0 |  | AspNetCore, Sdk Style = True |
| [TeamManagement.Application\TeamManagement.Application.csproj](#teammanagementapplicationteammanagementapplicationcsproj) | net10.0 | ✅ None | 0 | 0 | 0 |  | ClassLibrary, Sdk Style = True |
| [TeamManagement.Domain\TeamManagement.Domain.csproj](#teammanagementdomainteammanagementdomaincsproj) | net10.0 | ✅ None | 0 | 0 | 0 |  | ClassLibrary, Sdk Style = True |
| [TeamManagement.Infrastructure\TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | net10.0 | ✅ None | 0 | 0 | 0 |  | ClassLibrary, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 12 | 100,0% |
| ⚠️ Incompatible | 0 | 0,0% |
| 🔄 Upgrade Recommended | 0 | 0,0% |
| ***Total NuGet Packages*** | ***12*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| BCrypt.Net-Next | 4.2.0 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| MailKit | 4.17.0 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.10 |  | [TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj) | ✅Compatible |
| Microsoft.AspNetCore.OpenApi | 10.0.10 |  | [TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj) | ✅Compatible |
| Microsoft.AspNetCore.SignalR.Core | 1.2.12 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 |  | [TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj) | ✅Compatible |
| Microsoft.Extensions.Configuration.Json | 10.0.11 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.11 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| Microsoft.OpenApi | 2.7.5 |  | [TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj) | ✅Compatible |
| Newtonsoft.Json | 13.0.1 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |  | [TeamManagement.Api.csproj](#teammanagementapiteammanagementapicsproj)<br/>[TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |
| System.IdentityModel.Tokens.Jwt | 8.22.0 |  | [TeamManagement.Infrastructure.csproj](#teammanagementinfrastructureteammanagementinfrastructurecsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;TeamManagement.Api.csproj</b><br/><small>net10.0</small>"]
    P2["<b>📦&nbsp;TeamManagement.Application.csproj</b><br/><small>net10.0</small>"]
    P3["<b>📦&nbsp;TeamManagement.Domain.csproj</b><br/><small>net10.0</small>"]
    P4["<b>📦&nbsp;TeamManagement.Infrastructure.csproj</b><br/><small>net10.0</small>"]
    P1 --> P4
    P1 --> P2
    P2 --> P3
    P4 --> P3
    P4 --> P2
    click P1 "#teammanagementapiteammanagementapicsproj"
    click P2 "#teammanagementapplicationteammanagementapplicationcsproj"
    click P3 "#teammanagementdomainteammanagementdomaincsproj"
    click P4 "#teammanagementinfrastructureteammanagementinfrastructurecsproj"

```

## Project Details

<a id="teammanagementapiteammanagementapicsproj"></a>
### TeamManagement.Api\TeamManagement.Api.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** AspNetCore
- **Dependencies**: 2
- **Dependants**: 0
- **Number of Files**: 13
- **Lines of Code**: 673
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["TeamManagement.Api.csproj"]
        MAIN["<b>📦&nbsp;TeamManagement.Api.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#teammanagementapiteammanagementapicsproj"
    end
    subgraph downstream["Dependencies (2"]
        P4["<b>📦&nbsp;TeamManagement.Infrastructure.csproj</b><br/><small>net10.0</small>"]
        P2["<b>📦&nbsp;TeamManagement.Application.csproj</b><br/><small>net10.0</small>"]
        click P4 "#teammanagementinfrastructureteammanagementinfrastructurecsproj"
        click P2 "#teammanagementapplicationteammanagementapplicationcsproj"
    end
    MAIN --> P4
    MAIN --> P2

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="teammanagementapplicationteammanagementapplicationcsproj"></a>
### TeamManagement.Application\TeamManagement.Application.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 1
- **Dependants**: 2
- **Number of Files**: 27
- **Lines of Code**: 352
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (2)"]
        P1["<b>📦&nbsp;TeamManagement.Api.csproj</b><br/><small>net10.0</small>"]
        P4["<b>📦&nbsp;TeamManagement.Infrastructure.csproj</b><br/><small>net10.0</small>"]
        click P1 "#teammanagementapiteammanagementapicsproj"
        click P4 "#teammanagementinfrastructureteammanagementinfrastructurecsproj"
    end
    subgraph current["TeamManagement.Application.csproj"]
        MAIN["<b>📦&nbsp;TeamManagement.Application.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#teammanagementapplicationteammanagementapplicationcsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;TeamManagement.Domain.csproj</b><br/><small>net10.0</small>"]
        click P3 "#teammanagementdomainteammanagementdomaincsproj"
    end
    P1 --> MAIN
    P4 --> MAIN
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="teammanagementdomainteammanagementdomaincsproj"></a>
### TeamManagement.Domain\TeamManagement.Domain.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 0
- **Dependants**: 2
- **Number of Files**: 12
- **Lines of Code**: 187
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (2)"]
        P2["<b>📦&nbsp;TeamManagement.Application.csproj</b><br/><small>net10.0</small>"]
        P4["<b>📦&nbsp;TeamManagement.Infrastructure.csproj</b><br/><small>net10.0</small>"]
        click P2 "#teammanagementapplicationteammanagementapplicationcsproj"
        click P4 "#teammanagementinfrastructureteammanagementinfrastructurecsproj"
    end
    subgraph current["TeamManagement.Domain.csproj"]
        MAIN["<b>📦&nbsp;TeamManagement.Domain.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#teammanagementdomainteammanagementdomaincsproj"
    end
    P2 --> MAIN
    P4 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="teammanagementinfrastructureteammanagementinfrastructurecsproj"></a>
### TeamManagement.Infrastructure\TeamManagement.Infrastructure.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 2
- **Dependants**: 1
- **Number of Files**: 23
- **Lines of Code**: 2718
- **Estimated LOC to modify**: 0+ (at least 0,0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P1["<b>📦&nbsp;TeamManagement.Api.csproj</b><br/><small>net10.0</small>"]
        click P1 "#teammanagementapiteammanagementapicsproj"
    end
    subgraph current["TeamManagement.Infrastructure.csproj"]
        MAIN["<b>📦&nbsp;TeamManagement.Infrastructure.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#teammanagementinfrastructureteammanagementinfrastructurecsproj"
    end
    subgraph downstream["Dependencies (2"]
        P3["<b>📦&nbsp;TeamManagement.Domain.csproj</b><br/><small>net10.0</small>"]
        P2["<b>📦&nbsp;TeamManagement.Application.csproj</b><br/><small>net10.0</small>"]
        click P3 "#teammanagementdomainteammanagementdomaincsproj"
        click P2 "#teammanagementapplicationteammanagementapplicationcsproj"
    end
    P1 --> MAIN
    MAIN --> P3
    MAIN --> P2

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

