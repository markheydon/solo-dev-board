# ADR-0025: Optional shared Azure Container Registry

**Date:** 2026-08-17
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-025 during the repository workflow alignment chore (2026-11-14).

## Decision

Hosted CD may opt in to an **existing** Azure Container Registry via AppHost parameters `acr-name` and `acr-resource-group` (`PublishAsExisting` + `WithAzureContainerRegistry` + `WithAcrPullIdentity`). The `acr-pull` identity receives `AcrPull` in a separate Bicep module so cross-resource-group registries deploy without Bicep scope errors ([Aspire #11256](https://github.com/dotnet/aspire/issues/11256)). When both parameters are omitted, Aspire keeps its default behaviour and provisions a registry in the app resource group — required for forks and self-hosters who need no extra setup. When both are set, Staging and Production share that registry; use the same `ACR_NAME` and `ACR_RESOURCE_GROUP` on both GitHub Environments (repository-level variables preferred). The GitHub Actions OIDC identity for shared-ACR layouts should live in the ACR resource group with AcrPush and User Access Administrator on the registry, plus Contributor and User Access Administrator on each app RG. Reject documenting shared ACR as mandatory, calling `WithPurgeTask` or SKU `ConfigureInfrastructure` on a registry owned by other projects, or granting AcrPush to the running Container App.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
