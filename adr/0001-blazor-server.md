# ADR-0001: Use Blazor Server for the Front-End

**Date:** 2025-01-01
**Status:** Accepted

---

## Context

SoloDevBoard requires a web-based front-end that can:
- Render dynamic, interactive UI components (dashboards, tables, forms).
- Communicate with the GitHub API securely (tokens must not be exposed to the browser).
- Be deployed as a single application to Azure App Service without requiring a separate front-end build pipeline.
- Be developed by a single developer using C# exclusively, without requiring expertise in JavaScript frameworks.

The following options were considered:

| Option | Description |
|--------|-------------|
| **Blazor Server** | Razor components running on the server; UI updates delivered over SignalR. |
| **Blazor WebAssembly** | Razor components compiled to WebAssembly and running in the browser. |
| **ASP.NET Core MVC + Razor Pages** | Server-rendered HTML with optional JavaScript enhancement. |
| **React / Vue / Angular + ASP.NET Core API** | JavaScript SPA with a separate .NET API back-end. |

---

## Decision

**Use Blazor Server** as the front-end framework for SoloDevBoard.

---

## Rationale

- **Security:** GitHub tokens and API calls remain on the server. The browser never has direct access to credentials, which is critical for a tool that handles personal access tokens.
- **C#-only development:** The entire application — front-end and back-end — can be written in C#. This is well-suited to a solo developer with a .NET background.
- **Real-time UI:** Blazor Server's SignalR connection makes it straightforward to push live updates to the UI (e.g. refreshing dashboard data) without building a separate WebSocket or polling mechanism.
- **Shared domain model:** Blazor Server components can use domain and application layer types directly, without needing to serialise and deserialise across an HTTP API boundary.
- **Deployment simplicity:** A single `dotnet publish` artefact is deployed to Azure App Service. No CDN, no separate SPA hosting, no CORS configuration.
- **.NET 10 / C# 14:** Blazor Server is a first-class supported hosting model in .NET 10, with active investment from Microsoft.

### Trade-offs Accepted

- **Latency:** Every UI interaction requires a round-trip to the server over SignalR. For a single-user tool accessed over a reliable internet connection, this is acceptable.
- **Scalability:** Blazor Server maintains a server-side circuit (memory) per connected client. For a single-user application, this is not a concern.
- **Offline support:** Blazor Server requires a live server connection. Offline use is not a requirement for SoloDevBoard.

---

## Consequences

- All UI components are written as Razor components (`.razor` files) in the `SoloDevBoard.App` project.
- No JavaScript framework is introduced. JavaScript interop is used only when strictly necessary (e.g. for third-party visualisation components).
- The `SoloDevBoard.App` project references `SoloDevBoard.Application` for service interfaces.
- Azure App Service must be configured with **Always On** (for non-free tiers) to prevent the server from being recycled and dropping SignalR connections.
