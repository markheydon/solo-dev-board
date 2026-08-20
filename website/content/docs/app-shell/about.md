---
weight: 20
title: About
landingIcon: info
---

The About page provides essential information about the SoloDevBoard application, including its version, runtime environment, and repository link.

![About page showing version, runtime, and authentication details](/images/about/overview.png)

## Information shown

- Application name and branding.
- Application version (SemVer from git tags via MinVer at build time).
- Build commit SHA (when available), linked to the source repository for verification.
- .NET runtime version currently in use.
- GitHub authentication mode (hosted sign-in or PAT-only local trusted mode).
- Current GitHub identity (`@login`) for the active authentication mode.
- Link to the SoloDevBoard GitHub repository.

{{< callout type="important" >}}
Authentication details reflect the **active** mode for this session (hosted sign-in or PAT). Switching mode requires signing out or changing local configuration — the About page does not change credentials by itself.
{{< /callout >}}

## How to access

{{% steps %}}

### Open More options

Click the **More options** (three dots) menu in the app bar.

### Choose About

Select **About**, or choose **User Guide** to open the published documentation at [solodevboard.com/docs/](https://solodevboard.com/docs/).

{{% /steps %}}

You can also visit the `/about` route directly in your browser.
