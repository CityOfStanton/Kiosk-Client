<!-- omit in toc -->
<p align="center">
  <img src="https://raw.githubusercontent.com/CityOfStanton/Kiosk-Client/main/logo/Kiosk-Client_App%20Logo.png">
</p>

<!-- omit in toc -->
# Kiosk Client 2.0

A free, simple, easy-to-use Windows app that displays important information on screen. Designed for kiosk settings, Kiosk Client renders images and webpages in a predefined sequence or random order, driven by orchestration files hosted on any static web source or stored locally.

- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Building](#building)
  - [Running](#running)
  - [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [Orchestration File Format](#orchestration-file-format)
- [Deployment](#deployment)
  - [GitHub Actions CI/CD](#github-actions-cicd)
  - [Code Signing](#code-signing)
  - [Publishing to the Microsoft Store](#publishing-to-the-microsoft-store)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [Windows Store](#windows-store)
- [License](#license)

## Features

- **Image display** — Supports common formats (PNG, JPG, etc.) with configurable stretch modes.
- **Website display** — Embedded WebView2 browser with automatic scrolling support.
- **Remote orchestration** — Load content definitions from any publicly accessible URL.
- **Local file support** — Load orchestration files from the local file system.
- **Legacy format support** — Full backward compatibility with v1.x JSON and XML orchestration files.
- **Automatic content reload** — Orchestrations are reloaded between cycles so content updates appear without restarting.
- **Network resilience** — Watermark indicator on network failure; automatic retry with configurable countdown timer.
- **Full-screen kiosk mode** — Hides the taskbar and Start menu during orchestration playback.
- **Auto-retry** — Configurable countdown on the home screen to automatically restart a previously loaded orchestration.
- **Native AOT** — Compiled ahead of time for fast startup and low memory usage.
- **MSIX packaging** — Modern Windows installer with automatic updates.

## Architecture

Kiosk Client 2.0 is built with:

| Technology | Purpose |
|---|---|
| **.NET 8** | Runtime and build framework |
| **WinUI 3 (Windows App SDK 1.6)** | Modern native UI framework |
| **WebView2** | Embedded Chromium browser for website actions |
| **CommunityToolkit.Mvvm** | MVVM pattern with source generators |
| **System.Text.Json** | JSON serialization with polymorphic type support |
| **Native AOT** | Ahead-of-time compilation for performance |
| **xUnit** | Unit testing framework |

The app uses a simple static service locator in `App.xaml.cs` instead of a DI container for full AOT compatibility.

## Getting Started

### Prerequisites

- **Windows 10 19041** or later
- [**.NET 8 SDK**](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.400 or later)
- [**Visual Studio 2022**](https://visualstudio.microsoft.com/vs/) with the following workloads:
  - .NET Desktop Development
  - Windows Application Development (Windows App SDK)
- [Developer Mode enabled](https://docs.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development) on your machine

### Building

```powershell
# Restore and build the entire solution
dotnet build KioskClient.slnx --configuration Release

# Build for a specific platform
dotnet build KioskClient.slnx --configuration Release -p:Platform=x64
```

### Running

Open `KioskClient.slnx` in Visual Studio, set `src/KioskClient` as the startup project, and press **F5**.

Alternatively, deploy the MSIX package from the build output.

### Running Tests

```powershell
# Run all tests
dotnet test KioskClient.slnx

# Run with detailed output
dotnet test tests/KioskClient.Tests --verbosity normal
```

## Project Structure

```
KioskClient.slnx                     Solution file
src/
  KioskClient.Core/                   Core library (models, services, serialization)
    Models/                           Orchestration, ActionBase, ImageAction, WebsiteAction, etc.
    Serialization/                    JSON & XML serialization with legacy format support
    Services/                         HttpService, OrchestrationLoader, OrchestrationRunner
  KioskClient/                        WinUI 3 application
    Pages/                            HomePage, SettingsPage, OrchestrationPage, ImagePage, WebsitePage
    Dialogs/                          TutorialDialog, AboutDialog, ExamplesDialog
    ViewModels/                       SettingsViewModel (MVVM with source generators)
    Converters/                       XAML value converters
    Services/                         SettingsService, SettingsKeys
    Assets/                           Application icons and images
tests/
  KioskClient.Tests/                  xUnit tests for Core library
.github/
  workflows/
    build.yml                         CI/CD pipeline
```

## Orchestration File Format

Orchestration files can be **JSON** or **XML**. Both formats from v1.x are fully supported.

### JSON Example

```json
{
  "name": "Sample Orchestration",
  "version": "1.0",
  "pollingIntervalMinutes": 60,
  "lifecycle": "ContinuousLoop",
  "order": "Sequential",
  "actions": [
    {
      "$type": "KioskLibrary.Actions.ImageAction, KioskLibrary",
      "name": "Welcome Image",
      "duration": 10,
      "path": "https://example.com/welcome.png",
      "stretch": "Uniform"
    },
    {
      "$type": "KioskLibrary.Actions.WebsiteAction, KioskLibrary",
      "name": "Dashboard",
      "duration": 30,
      "path": "https://example.com/dashboard",
      "autoScroll": true,
      "scrollingTime": 20,
      "scrollingResetDelay": 5,
      "settingsDisplayTime": 5
    }
  ]
}
```

### XML Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<Orchestration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Name>Sample Orchestration</Name>
  <Version>1.0</Version>
  <PollingIntervalMinutes>60</PollingIntervalMinutes>
  <Lifecycle>ContinuousLoop</Lifecycle>
  <Order>Sequential</Order>
  <Actions>
    <ActionBase xsi:type="ImageAction">
      <Name>Welcome Image</Name>
      <Duration>10</Duration>
      <Path>https://example.com/welcome.png</Path>
      <Stretch>Uniform</Stretch>
    </ActionBase>
  </Actions>
</Orchestration>
```

## Deployment

### GitHub Actions CI/CD

The pipeline at `.github/workflows/build.yml` runs on every push and pull request to `main`:

1. **Build & Test** — Restores, builds (x64 & ARM64), and runs all unit tests.
2. **Security Analysis** — Runs `dotnet list package --vulnerable` and GitHub CodeQL analysis.
3. **Package** — Builds signed (or unsigned) MSIX packages and uploads them as artifacts.

MSIX artifacts are uploaded for each platform and can be downloaded from the workflow run's **Artifacts** section.

### Code Signing

The workflow supports code signing with a PFX certificate. To enable signing:

1. **Base64-encode your PFX file:**

   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Kiosk Client Store Key.pfx"))
   ```

2. **Add the following GitHub repository secrets:**

   | Secret Name | Description |
   |---|---|
   | `SIGNING_CERTIFICATE_BASE64` | The Base64-encoded contents of `Kiosk Client Store Key.pfx` |
   | `SIGNING_CERTIFICATE_PASSWORD` | The password for the PFX file |

3. The workflow will automatically detect the secrets and sign the MSIX package. If the secrets are not set, an unsigned package is produced.

### Publishing to the Microsoft Store

1. Build a **Release** MSIX package via the GitHub Actions workflow (or locally with `dotnet publish`).
2. Download the MSIX artifact from the workflow run.
3. Sign in to [Microsoft Partner Center](https://partner.microsoft.com/dashboard).
4. Navigate to **Apps and games** → **Kiosk Client** → **Update**.
5. Upload the MSIX package under **Packages**.
6. Complete the **Store listing**, **Pricing**, and **Age rating** sections.
7. Submit for certification.

The existing Store listing is at: [Kiosk Client on Microsoft Store](https://www.microsoft.com/store/apps/9NQZFB05ZMV9)

## Configuration

Settings are persisted in Windows application storage and survive between sessions.

| Setting | Default | Description |
|---|---|---|
| Retry Timeout (seconds) | 30 | Countdown timer on the home screen before auto-retrying a failed orchestration |
| Max URL History | 5 | Number of previously loaded URLs saved in the source dropdown |

These can be changed on the **Settings** page of the application.

## Contributing

We welcome contributions! To get started:

1. Fork the repository.
2. Create a feature branch from `main`.
3. Make your changes and ensure all tests pass (`dotnet test`).
4. Submit a Pull Request.

Please open an issue first to discuss significant changes.

## Windows Store

Download the latest release from the Microsoft Store:

<a href='//www.microsoft.com/store/apps/9NQZFB05ZMV9?cid=storebadge&ocid=badge'><img src='https://developer.microsoft.com/store/badges/images/English_get-it-from-MS.png' alt='Get it from Microsoft' width="284" height="104"/></a>

## License

Copyright (c) 2021-2026 City of Stanton. All rights reserved. See [LICENSE](LICENSE) for details.
