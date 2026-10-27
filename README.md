# Neuro-Access

Neuro-Access is a .NET MAUI application from [Trust Anchor Group](https://trustanchorgroup.com/), based on the original [TAG ID App](https://github.com/Trust-Anchor-Group/IdApp). It provides digital identities, KYC, contracts, tokens, eDaler wallets and XMPP messaging.

## Choose an application

| Project | Purpose | Android package |
| --- | --- | --- |
| [NeuroAccessMaui](NeuroAccessMaui/NeuroAccessMaui.csproj) | Main application with real accounts, services and user flows | `com.tag.NeuroAccess` |
| [DevTestPlatform](DevTestPlatform/README.md) | Developer catalog of current production UI in controlled visual scenarios | `com.tag.neuroaccess.devtestplatform` |
| [Android UI tests](tests/mobile/andriod/ui/README.md) | Kotlin/Espresso instrumentation of the main application | Targets `com.tag.NeuroAccess` |

The developer application has its own icon, splash screen and package, so it can be installed alongside the main application. Its current catalog covers onboarding, verification and two reusable controls.

## Android quick start

Install the .NET SDK pinned by [global.json](global.json), currently **10.0.303**, the MAUI Android workload and Android Studio/Android SDK. Connect an authorized device or start an emulator.

From the repository root:

```powershell
dotnet workload restore .\DevTestPlatform\DevTestPlatform.csproj
adb devices -l
```

Launch the developer catalog:

```powershell
dotnet build .\DevTestPlatform\DevTestPlatform.csproj -t:Run -f net10.0-android -c Debug -p:AdbTarget="-s <DEVICE_SERIAL>"
```

For real account and service flows, launch the main app:

```powershell
dotnet build .\NeuroAccessMaui\NeuroAccessMaui.csproj -t:Run -f net10.0-android -c Debug -p:AdbTarget="-s <DEVICE_SERIAL>"
```

Replace `<DEVICE_SERIAL>` with a serial from `adb devices -l`. Match the APK's ABI to the device. Gradle builds the instrumentation APK separately and does not build either MAUI app.

See [Getting Started](Content/getting_started.md) for other setup details. Commands above reflect the current project files; older examples elsewhere may still reference earlier SDKs.

## UI identity and navigation

[AutomationIds.cs](NeuroAccessMaui/UI/Automation/AutomationIds.cs) owns technical IDs for production views and developer chrome. Production controls retain their IDs inside developer previews; developer controls use `dev_` IDs. [UI_IDS.md](tests/mobile/andriod/ui/app/src/androidTest/java/com/tag/neuroaccess/neuroaccessespressoautomationtests/UI_IDS.md) documents the catalog, dynamic keys, retired identifiers and migration gaps.

`NavigationService` manages `BaseContentPage` navigation through a main-thread queue. `CustomShell` presents pages, global bars, popups, toasts and hardware back handling. `PopupService` coordinates popup transitions and lifecycle hooks. Use these services from view models; see [Navigation](Content/navigation.md). The developer app uses its own `DevShell` and `DevNavigationService` for preview sessions.

## Documentation

| Topic | Guide |
| --- | --- |
| Developer catalog and preview behavior | [DevTestPlatform](DevTestPlatform/README.md) |
| Tests and automation entry points | [Tests](tests/README.md) |
| Local Android runs, configuration, logs and reports | [Android UI tests](tests/mobile/andriod/ui/README.md) |
| Remote emulators and matrices | [VM infrastructure](tests/infrastructure/vm/readme.md) |
| Setup | [Getting Started](Content/getting_started.md) |
| Architecture | [Architecture](Content/architecture.md) |
| Services | [Services](Content/services.md) |
| Onboarding | [Onboarding](Content/Onboarding.md) |
| Contribution workflow | [Contributing](Content/contributing.md) and [AGENTS.md](AGENTS.md) |
| Release history | [Changelog](CHANGELOG.md) |

## Frameworks and SDK

The main project declares `net10.0-android` and `net10.0-ios`, with `net10.0-windows10.0.19041.0` additionally declared on Windows. iOS builds require the Apple toolchain and signing setup. DevTestPlatform currently targets Android only (`net10.0-android`, minimum API 24).

Use an explicit `-f` to build one platform. Keep SDK documentation aligned with `global.json` and the project files.

## Contributing

Follow [AGENTS.md](AGENTS.md) and the [contribution guide](Content/contributing.md). Non-trivial behavioral changes use `plans/<feature-slug>/`. Keep production metadata, the C# catalog, the Kotlin catalog and the ID reference aligned.

## Security and license

Report security-sensitive issues privately through Trust Anchor Group's website. Use GitHub Issues for general bugs. See [LICENSE](LICENSE).
