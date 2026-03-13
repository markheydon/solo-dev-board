# ADR-0014: Hosted Access Control for Public Deployments

**Date:** 2026-03-13
**Status:** Accepted

## Context

SoloDevBoard is preparing for a publicly reachable hosted deployment. The current authentication plan (see ADR-0005 and ADR-0007) covers OAuth and GitHub App authentication, but does not explicitly restrict access to authorised users. Without admission control, any GitHub user could sign in and view repository data, which is not acceptable for production. There is also a future possibility of paid or sponsored access, but this is not in scope for v1.0.0.

## Decision

For v1.0.0, SoloDevBoard will implement explicit operator-managed admission control for hosted deployments. Access is deny-by-default: only GitHub users or organisations authorised by the operator may sign in and access the UI. Admission control is evaluated before repository data is loaded. The design includes an abstraction/extension point for future entitlement providers (e.g., GitHub Sponsors), but does not implement billing integration at this stage.

## Rationale

This approach secures the hosted default, avoids conflating authentication with authorisation, and supports future commercial options without over-scoping v1.0.0. It aligns with ADR-0007's phased authentication strategy and keeps the path open for future entitlement automation.

## Consequences

- Story #117 created to track hosted admission control implementation.
- Operator-managed allow-list or equivalent entitlement source is required for hosted environments.
- Unauthorised users will see denied access and cannot view repository data.
- Future extension path for paid/sponsored access is preserved, but billing integration remains out of scope for v1.0.0.

## References

- ADR-0005: GitHub API Strategy
- ADR-0007: Multi-Tenancy Authentication — Phased Approach
