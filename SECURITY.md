# Security policy

## Supported versions

Please report vulnerabilities against:

- The current `main` branch.
- Tagged `v1.x` releases, once they exist.

Older pre-1.0 snapshots are not supported.

## Reporting a vulnerability

**Do not file a public GitHub issue for security problems.**

Use [GitHub private vulnerability reporting](https://github.com/markheydon/solo-dev-board/security/advisories/new) so the maintainer can fix the issue before it is disclosed.

If private reporting is unavailable, contact the maintainer through GitHub ([@markheydon](https://github.com/markheydon)) and say that the message is a security report. Do not include secrets, tokens, or exploit payloads in public channels.

Please include:

- A description of the issue and its impact.
- Steps to reproduce, or a proof of concept that does not destroy data.
- Affected versions, commit SHAs, or hosted URLs where you can share them privately.
- Any suggested mitigation.

You should receive an acknowledgement when the report is seen. A fix, advisory, or reasoned decline will follow once the issue has been assessed. Please do not disclose the vulnerability publicly until a fix is released, or the maintainer agrees that disclosure is appropriate.

## Scope notes

SoloDevBoard is a GitHub-authenticated Blazor Server app. Reports that matter most include:

- Leakage of GitHub tokens, client secrets, or session cookies.
- Authentication or admission-control bypass on hosted deployments.
- Cross-user data exposure on a shared hosted instance.
- Supply-chain issues in release artefacts or GitHub Actions workflows.

Out of scope: denial-of-service against GitHub.com, issues that require a stolen PAT or physical access to a self-hosted machine, and theoretical findings with no practical impact.

## Secrets in this repository

This repository is public. Never commit personal access tokens, GitHub App client secrets, Azure credentials, or `.env` files. See [`AGENTS.md`](AGENTS.md#open-source--security) and [`CONTRIBUTING.md`](CONTRIBUTING.md#secrets--security).
