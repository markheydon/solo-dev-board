---
layout: page
title: About
parent: User Guide
nav_order: 10
---

# About

The About page provides essential information about the SoloDevBoard application, including its version, runtime environment, and repository link.

## Overview

The About page offers a concise summary of the application's identity and technical details. It helps users verify the current version and access the project repository.

## Information Shown

- Application name and branding.
- Application version, retrieved from the centralised `IAppVersionService`.
- .NET runtime version currently in use.
- Link to the SoloDevBoard GitHub repository.

## How to Access

- Click the **More options** (three dots) menu in the app bar, then select **About**.
- Visit the `/about` route directly in your browser.

## Notes

- The application version is sourced from `Directory.Build.props` and exposed via the `AppVersionService`.
- Version information is centralised to ensure consistency across the application.
- The About page is designed for transparency and easy access to technical details.
