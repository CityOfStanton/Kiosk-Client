# Kiosk Client 2.0

Kiosk Client is a tool that displays various types of information the is sourced from a URL, typically via a JSON file, or locally via a JSON file. The tool is meant to be used alongside Windows Kiosk Mode.

# Why we're rewriting it

The codebase is full of legacy code for the old WIN UI framework. It has become too difficult to maintain and needs to be rewritten to be clean, performant, and more easily upgradeable in the future. The old version received a warning about its legacy code base that linked to this article: https://learn.microsoft.com/en-us/windows/uwp/dotnet-native/modernize-uwp-apps-with-dotnet. This should all be taken in to account when creating a new version.

# Legacy codebase

The legacy source code for the application can be found in the #Old folder, as well as GitHub at https://github.com/CityOfStanton/Kiosk-Client.

# Requirements

Here are requirements for the new version of Kiosk Client, which will be version 2.0.

## How It Works

A user will get the app from the Windows Store or WinGet. Once installed, the user will open the app for the first time and be greeted with a demo walkthrough. The user can click 'Next' through the demo, or dismiss the demo. There should be an option to 'Never Show Again', which will result in the demo mode never being showed again, unless the user click on the Demo button. Once the demo mode concludes, the user will have 2 options to load a file: either by entering a website URL in a Drop Down box that you can type in, or by specifying a file on the local file system using an Open button that will being up an Open File Dialog. If they enter a URL, then there should be a button that allows them to Load the URL. The Load process will validate the URL to ensure it exists, that it the content inside is valid for the application, and also list a summary of the info. In addition, if the Load was successful, the URL should be stored in the Drop Down List for later retrieval. The previous 5 URL should be stored there. If the Load is successful, then the user should be able to Run the file, or as it's sometimes called, an orchestration. The contents of the file should be displayed on a loop, as specified in the file contents, and never return to the main window. If the file fails to load, then the user needs to be presented with the reason why it failed to load. The Run button should be disabled if the Load failed.

The orchestration will specify a duration that is should be displayed. After that time, it should start again but it needs to reload its contents before it displays again. This way, if some new information has been added to the file, then that will be what is displayed. You will likely need a separate background process to load the orchestration before the next run starts. If this is not possible with the file system loaded files, then only enable this for the URL loaded orchestrations.

## Core Functionality

* The application must be a native UWP app supporting the latest .net version allowed. 
* The application should leverage application storage to retain important information between runs.
* The application should use the Win UI 3 framework. 
* When the orchestration tuns, it runs in a full screen mode. The Windows start menu, taskbar, etc. should not be visible. 

## Legacy File Support

There is extensive legacy content that all must be supported. The same file format should be used for this new version. You can find the existing content on GitHub as well at: https://github.com/CityOfStanton/Kiosk-Client-Content. 

## UI

The UI should remain very similar to the UI from the legacy code base. Updates are fine, but the overall design should look very similar.
All functionality from the side toolbar should remain in the new version.

# Browser

For the WebView2 web viewer, you should leverage the natively installed web browser. This way the browser can receive updates outside of the application receiving updates.

## Network Errors

On occasion, it has been observed that a network failure causes the orchestration to be unable to reload. This results in the orchestration ending its run once it has reached the end of its alloted time. When it ends, it returns to the home screen.

This behavior needs to be modified in the new version. Ideally, when the orchestration fails to load a new version due to a network failure, a watermark should appear at the bottom left of the screen indicating the network has been disconnected. There should also be a retry timer on the home screen that activates whenever a previously successfully loaded orchestration returns to the main screen. The countdown timer should default to 30 seconds. There should be a button to stop the countdown until the next run is manually started. Once the countdown expires, you should attempt to start the orchestration again. 

## Testing 

There should be extensive unit tests for the code. 
Ensure each test is documented with its purpose, including expected results.

## Deployment

The deployable MSIX file should be built from a GitHub Actions file. That file should build the code, run all tests, run any recommended security analysis tools, then build the installer package. The installer package should be stored as a build artifact that can be easily uploaded to Microsoft website.

Instructions for uploading the new version to be used with the Windows Store would be nice to have.

## Code signing

For code signing requirements, I've included the `Kiosk Client Store Key.cer` and `Kiosk Client Store Key.pfx` files. I will supply the password for the code signing pfx via GitHub. Please let me know which variables names I need to sue for the code signing pfx file adn password in an instructions document.

## Settings

The retry timeout should be configurable. The default is 30.
The number of previously successful orchestration URLs saved in the drop down should be configurable. The default is 5.

## About Page

Use the existing About page in the legacy code base, just update it with the latest information. The versions, names, etc. sHould be tied to the build properties.

##  Documentation

Ensure the application is well documented throughout. 

## Example file

The legacy code base had an Example file that could be downloaded. This functionality needs to remain. 

## Icons

Leverage the existing codebase when searching for icons for the app. All icons should be in .png format.

# Finished Product

Once complete, the solution should build successfully, be abel to run via the Visual Studio debugger, can be build a MSIX installer package, and have all tests pass.