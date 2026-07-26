> **Audience:** Developers and operators. This guide is repository documentation and is not part of the published end-user site in `user-docs/`.

# Observability and Telemetry

SoloDevBoard emits structured logs, metrics, and distributed traces so operators can diagnose production behaviour without relying on ad hoc console output.

---

## What is collected

| Signal | Source | Production destination |
|---|---|---|
| **Logs** | `ILogger` categories across the Blazor Server app and infrastructure layer | Azure Application Insights (via OpenTelemetry) and Container Apps stdout (JSON) |
| **Metrics** | ASP.NET Core, HTTP client, and .NET runtime instrumentation | Azure Application Insights |
| **Traces** | ASP.NET Core requests and outbound HTTP calls | Azure Application Insights |

Local development uses the Aspire dashboard via the OTLP exporter. Azure deployments use Application Insights provisioned by the AppHost.

---

## Provisioning (Azure)

Aspire provisions observability resources during `aspire deploy`:

| Resource | Purpose |
|---|---|
| **Application Insights** (`app-insights`) | APM, logs, metrics, and traces for the `app` container |
| **Log Analytics workspace** | Backing store for Application Insights telemetry and Container Apps platform logs |

The AppHost wires the Application Insights connection string into the `app` resource as `APPLICATIONINSIGHTS_CONNECTION_STRING`. No manual secret mapping is required in the CD workflow.

See [Deployment](deployment.md) for the full operator guide.

---

## Health endpoints

SoloDevBoard exposes operational health endpoints for container orchestration and post-deploy smoke checks:

| Endpoint | Type | When to use |
|---|---|---|
| `GET /health` | Readiness | After deployment, scale-from-zero, or when verifying the app can accept traffic |
| `GET /alive` | Liveness | When confirming the process is responsive (no external dependency checks) |
| `GET /health/github` | GitHub PAT connectivity | When verifying the configured personal access token can reach GitHub (PAT mode only) |

Both endpoints are available in all environments, including production Azure Container Apps. They return `Healthy` when checks pass and do not require authentication, including when hosted admission control is enabled.

The AppHost wires `/health` as the Container App HTTP probe via `.WithHttpHealthCheck("/health")`. See [Deployment — Health checks and Container Apps probes](deployment.md#health-checks-and-container-apps-probes) for probe design rationale and verification steps.

---

## Structured logging

Server-side logging follows these conventions:

- Use `ILogger<T>` with [message templates](https://learn.microsoft.com/dotnet/core/extensions/logging#log-message-template-format) and named placeholders (for example, `LogWarning("Hosted admission denied. Reason: {Reason}", decision.Reason)`).
- Default log level is `Information`; ASP.NET Core framework noise is suppressed to `Warning` in production.
- Non-development environments write JSON-formatted logs to stdout so Container Apps log ingestion can query fields such as `LogLevel`, `Category`, and `Message`.
- OpenTelemetry logging exports formatted messages and scopes to Application Insights when a connection string is present.

### Recommended log levels

| Category | Production level | Notes |
|---|---|---|
| `Default` | Information | Application events |
| `Microsoft.AspNetCore` | Warning | Suppress routine request noise |
| `System.Net.Http.HttpClient` | Warning | Suppress per-request HTTP client traces |

Adjust levels in `appsettings.Production.json` when investigating a specific subsystem.

---

## Sensitive data handling

Telemetry must never contain credentials, tokens, cookies, or personal data beyond what is required for operations.

### Never log or emit in telemetry

- GitHub personal access tokens, OAuth codes, client secrets, or installation tokens.
- `Authorization`, `Cookie`, or `Set-Cookie` header values.
- Full OAuth callback URLs containing `code`, `state`, `token`, `access_token`, `refresh_token`, or `client_secret` query parameters.

### Built-in safeguards

SoloDevBoard applies the following defaults in `SoloDevBoard.ServiceDefaults`:

- **Inbound URL redaction:** Known OAuth-related query-string keys are replaced with `[Redacted]` on ASP.NET Core request spans before `url.full` is set.
- **Outbound URL redaction:** Outbound `HttpClient` spans rely on the OpenTelemetry HTTP instrumenter's default behaviour, which redacts all query-string values.
- **Header tag stripping:** If sensitive request header tags (`Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`) are present on a span, they are removed at enrichment time.
- **Health-check exclusion:** `/health` and `/alive` requests are excluded from distributed tracing to reduce noise.

When adding new logging or custom span enrichment, follow the same rules. Prefer opaque identifiers (for example, repository owner/name) over secrets. Do not add custom enrichment that copies request headers into telemetry.

---

## Operational usage

### Azure portal

1. Open the Application Insights resource created by your deployment (named with the `app-insights` Aspire resource prefix).
2. Use **Logs** to query `traces`, `requests`, `exceptions`, and `customEvents` tables.
3. Use **Investigate → Failures** for error spikes and **Performance** for slow requests.

Example Kusto query for recent errors:

```kusto
exceptions
| where timestamp > ago(24h)
| order by timestamp desc
| take 50
```

Example query for hosted sign-in warnings:

```kusto
traces
| where timestamp > ago(24h)
| where message contains "Hosted sign-in failed"
| order by timestamp desc
```

### Container Apps stdout logs

When Application Insights is unavailable (for example, during a misconfiguration investigation), query the Log Analytics workspace linked to the Container Apps environment:

```kusto
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1h)
| where ContainerAppName_s == "app"
| order by TimeGenerated desc
```

Stdout entries are JSON when `ASPNETCORE_ENVIRONMENT` is not `Development`.

### Local development

Run the AppHost and open the Aspire dashboard:

```bash
aspire start --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

Structured logs, traces, and metrics for the `app` resource appear in the dashboard via OTLP. The AppHost registers Application Insights only in publish/deploy mode, so no Azure credentials or deployment metadata are required for local runs. Application Insights export is inactive locally.

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| No telemetry in Application Insights | Deploy predates App Insights wiring, or connection string missing | Redeploy with current AppHost; verify `APPLICATIONINSIGHTS_CONNECTION_STRING` on the Container App |
| Duplicate telemetry | Both OTLP and Application Insights exporters active | Expected during local runs with a connection string; production should rely on Application Insights |
| High log volume / cost | Verbose categories left enabled | Restore production log levels; review Log Analytics ingestion in Cost Management |
| OAuth codes visible in traces | Custom enrichment bypassing redaction | Use `TelemetryRedaction.RedactHttpUrl` before attaching URLs to spans |

For deployment prerequisites and environment configuration, see [Deployment](deployment.md) and [Azure Deployment Costs](azure-costs.md).
