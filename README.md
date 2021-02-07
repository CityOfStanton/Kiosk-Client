
<!-- omit in toc -->
<p align="center">
  <img src="https://raw.githubusercontent.com/CityOfStanton/Kiosk-Client/develop/logo/Kiosk-Client_App%20Logo%20%40%20200.png">
</p>

<!-- omit in toc -->
# Kiosk Client ![CodeQL](https://github.com/CityOfStanton/Kiosk-Client/workflows/CodeQL/badge.svg) [![Build Status](https://dev.azure.com/chadbirch0541/Kiosk%20Client/_apis/build/status/CityOfStanton.Kiosk-Client?branchName=develop)](https://dev.azure.com/chadbirch0541/Kiosk%20Client/_build/latest?definitionId=1&branchName=develop)

A simple Universal Windows Platform (UWP) app used to display web-based content on a Windows 10 systems.

- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
- [Known Issues](#known-issues)
- [Contributing](#contributing)
- [Future Work](#future-work)

## Features
  * Remotely control the app using a web-based static file or a local file.
    * Displays images
      * Supports most common formats such as .png and .jpg.
    * Displays websites
      * Customizable, automatic website scrolling.
  * Set timing between actions to allow for a dynamic experience.
  * Designed for use with Windows Kiosk Mode.
  * Minimum permission requirement reduces elevation attack surface.

## Getting Started

### Prerequisites

* [Visual Studio](https://visualstudio.microsoft.com/vs/community)
* A Windows machine with [Developer Mode enabled](https://docs.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).

## Known Issues
Due to the way we render web pages, you can't press the Escape key to return to the settings. Instead, there is a button with a low opacity that exists in the bottom right-hand corner of the screen for the first 5 seconds the webpage is rendered. Clicking on that button will return you to the Settings menu.

## Contributing

We encourage anyone to contribute features or bug fixes. Simply raise an issue, then use that thread to let people know what you're working on. This coordinate work so two people aren't working on the same problem at any given time.

**Note:** PR builds will fail the pipeline check if you're contributing from a fork. This due to a [security restriction in Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/repos/github?view=azure-devops&tabs=yaml#access-restrictions). A repository owner will verify your commit builds and passes tests once it has been merged with the [develop branch](https://github.com/CityOfStanton/Kiosk-Client/tree/develop).

## Future Work

Some ideas for future work include:

* A UI to help design the Orchestration Settings Files
* Support for more actions, such as movies, PowerPoints, etc.
* Support for embedded data, such as images. This would enable clients to function offline.
* Multiple client coordination, allowing groups of clients on a network to coordinate the information they display.