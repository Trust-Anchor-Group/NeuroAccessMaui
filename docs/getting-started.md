# Getting started

This guide takes you from a fresh machine to Neuro-Access running on an emulator, simulator or your desktop. The [README](../README.md) has the short version; read this when you set up a new machine or when something in the short version does not work.

## Prerequisites

### Everyone

- **.NET SDK 10.0.303.** [`global.json`](../global.json) pins this exact version and disables roll-forward, so a newer or older SDK will not build the repository. Check what you have with `dotnet --list-sdks`, and get the right one from the [.NET 10 download page](https://dotnet.microsoft.com/download/dotnet/10.0).
- **Git**, to clone the repository.
- **An editor.** Visual Studio 2026 with the .NET Multi-platform App UI workload is the smoothest option on Windows. VS Code also works, with either the [.NET MAUI extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui) or DotNet Meteor. The repository ships launch configurations for both in [`.vscode/launch.json`](../.vscode/launch.json), and Debug builds turn on the DotNet Meteor XAML hot reload plugin.

### Per platform

| Platform | You also need |
|---|---|
| Android | The Android SDK and an emulator or a device with USB debugging turned on. Visual Studio installs the SDK and an emulator with its MAUI workload. The app supports Android 7.0 (API 24) and later. |
| iOS | A Mac with the Xcode version that the installed .NET iOS workload requires. You can build directly on the Mac, or from Visual Studio on Windows through a paired Mac. The app supports iOS 15 and later. |
| Windows | Windows 10 version 1809 or later. The Windows target is only added when you build on Windows. |

## Get the code

```sh
git clone https://github.com/Trust-Anchor-Group/NeuroAccessMaui.git
cd NeuroAccessMaui
```

Day-to-day work happens on the `dev` branch, while `main` holds released code. See [Contributing](contributing.md) for the branching model.

## Install the workloads

From the repository root, install the MAUI workloads the projects need:

```sh
dotnet workload restore
```

Run it again after an SDK or MAUI update. If the build later complains about missing workloads (for example `NETSDK1147`), this is the command that fixes it.

## Build and run

### From the command line

Always pass a target framework with `-f`. Without it, the build covers every target framework of the current host, and on Windows that includes iOS, which needs a Mac.

```sh
# Android emulator or device
dotnet build NeuroAccessMaui/NeuroAccessMaui.csproj -t:Run -f net10.0-android

# iOS simulator (on a Mac)
dotnet build NeuroAccessMaui/NeuroAccessMaui.csproj -t:Run -f net10.0-ios

# Windows
dotnet build NeuroAccessMaui/NeuroAccessMaui.csproj -t:Run -f net10.0-windows10.0.19041.0
```

Leave out `-t:Run` to build without launching.

### From Visual Studio

1. Open `NeuroAccessMaui.sln`.
2. Set `NeuroAccessMaui` as the startup project.
3. Pick a target in the run menu: an Android emulator or device, an iOS simulator or device through a paired Mac, or **Windows Machine**.
4. Press F5.

### From VS Code

Open the repository folder, wait for the solution to load, and start the **.NET MAUI** or **.NET Meteor Debugger** launch configuration. Each extension has its own way to pick the target device.

## Platform notes

### Android

Debug builds use the JIT and include debug symbols, while Release builds are compiled ahead of time, so expect Release builds to take noticeably longer. The Android application ID is `com.tag.NeuroAccess`.

### iOS

The iOS application ID is `com.trustanchorgroup.NeuroAccess`. The project's signing settings (`CodesignKey` and `CodesignProvision` in [`NeuroAccessMaui.csproj`](../NeuroAccessMaui/NeuroAccessMaui.csproj)) point at Trust Anchor Group's development certificate and provisioning profile. To deploy to a device with your own Apple account, pass your own values on the command line, for example `-p:CodesignKey="Apple Development: ..." -p:CodesignProvision="..."`, instead of committing a change to the project file.

### Windows

The Windows build is unpackaged, so it runs straight from the build output without an MSIX install. Push notifications are not available on Windows.

[`launchSettings.json`](../NeuroAccessMaui/Properties/launchSettings.json) has two profiles, **Windows Machine** and **Windows Machine 2**. In Debug builds on Windows, the app keeps its database in a folder named after the `NEUROACCESS_DATA_PROFILE` environment variable, and the second profile sets it. That lets you run two separate accounts side by side, for example to chat or sign a contract between them.

## Firebase and push notifications

Push notifications use Firebase Cloud Messaging. The Firebase config files are not in the repository:

- `NeuroAccessMaui/google-services.json` for Android
- `NeuroAccessMaui/GoogleService-Info.plist` for iOS

The project only includes each file when it exists, and both are git-ignored. Without them push notifications do not work. Put your own files in place to test push, and never commit them. [Notifications](notifications.md) describes the push pipeline.

## Run the tests

The unit test projects use MSTest and target plain `net10.0`, so they run without the MAUI workloads:

```sh
dotnet test NeuroAccess.Nfc.Test/NeuroAccess.Nfc.Test.csproj
dotnet test IdApp.Cv.Test/IdApp.Cv.Test.csproj
```

## Check your code style

The repository's code style is in [AGENTS.md](../AGENTS.md), and `.editorconfig` enforces most of it. Before you open a pull request, run the style and whitespace checks on the files you changed:

```sh
dotnet format style NeuroAccessMaui.sln --verify-no-changes --include path/to/First.cs path/to/Second.cs
dotnet format whitespace . --folder --verify-no-changes --include path/to/First.cs path/to/Second.cs
```

## Start over with a clean slate

To run the onboarding again, remove the app's data. On Android and iOS, uninstall the app or clear its storage. On Windows, delete the `Data` folder under `%APPDATA%`, or `Data<profile>` when you use a data profile. If the build itself gets into a strange state, close the IDE and delete the `bin` and `obj` folders of the affected projects.

## Troubleshooting

| Problem | What to do |
|---|---|
| `A compatible .NET SDK was not found` or a message about `global.json` | Install SDK 10.0.303 exactly. Roll-forward is disabled, so a newer SDK does not count. |
| Missing workload errors such as `NETSDK1147` | Run `dotnet workload restore` from the repository root. |
| The iOS target fails to build on Windows | Pass `-f net10.0-android` or `-f net10.0-windows10.0.19041.0`, or pair Visual Studio with a Mac. |
| An iOS device build fails with a signing error | Pass your own `CodesignKey` and `CodesignProvision`, as described under iOS above. |
| Push notifications never arrive | Check that the Firebase config file for the platform is in place, then see [Notifications](notifications.md). |

## Related docs

- [Architecture](architecture.md)
- [Services](services.md)
- [Navigation](navigation.md)
- [Contributing](contributing.md)
