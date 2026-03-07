---
layout: page
title: Repositories
parent: User Guide
nav_order: 0
---

# Repositories

The Repositories page shows the repositories available to your authenticated GitHub account in one place.

---

## Overview

Use this page to quickly confirm which repositories SoloDevBoard can access before you start using other features.

For each repository, the page shows:
- Repository name
- Visibility (`Public` or `Private`)
- Last updated date

---

## How to Use

1. Open **Repositories** from the left navigation.
2. Wait for the repository list to load.
3. Review repository visibility and recency.

If loading fails, use **Try again** after checking your GitHub authentication settings.

---

## Troubleshooting

If no repositories appear or loading fails:
- Confirm your GitHub token is configured via user secrets or environment variables.
- Confirm the token has scopes required by your repositories.
- Confirm your network can reach `api.github.com`.
