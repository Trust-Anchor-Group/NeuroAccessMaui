# NeuroAccess tests and developer UI

Start here to choose visual inspection, local instrumentation or remote Android runs.

| Goal | Entry point | Application |
| --- | --- | --- |
| Inspect production UI in controlled scenarios | [DevTestPlatform](../DevTestPlatform/README.md) | `com.tag.neuroaccess.devtestplatform` |
| Run Android user flows and inspect reports | [Android UI tests](mobile/andriod/ui/README.md) | `com.tag.NeuroAccess` |
| Work with Espresso selectors and screen objects | [Source guide](mobile/andriod/ui/app/src/androidTest/java/com/tag/neuroaccess/neuroaccessespressoautomationtests/README.md) | Main app |
| Review ID ownership and migration | [UI ID contract](mobile/andriod/ui/app/src/androidTest/java/com/tag/neuroaccess/neuroaccessespressoautomationtests/UI_IDS.md) | Shared catalog |
| Run remote emulators and matrices | [VM infrastructure](infrastructure/vm/readme.md) | Main app + test APK |

## Layout

```text
tests/
├── docs/                     # Supporting documentation
├── infrastructure/vm/        # Windows clients, Linux runners and Android matrices
├── mobile/andriod/ui/         # Gradle/Kotlin instrumentation project
├── regression/               # Regression workspace
└── scripts/                  # Static automation contract check
```

Existing directory names `mobile/andriod` and `android/config/devices/andriod` retain their spelling. Use the actual paths in commands.

Plain .NET test projects also live at the repository root, including `NeuroAccess.Nfc.Test`, `NeuroAccessMaui.Test` and `IdApp.Cv.Test`. The Android project uses Kotlin/Espresso; repo MSTest conventions apply to the .NET test projects.

## Current boundaries

DevTestPlatform reuses production UI for visual inspection and returns synthetic developer diagnostics. The existing Espresso manifest and launcher target the main app.

Registration requires `NEUROACCESS_TEST_REGISTRATION_MODE=temporary-phone`; ordinary email onboarding is not migrated in the Espresso registration flow. The stateful full suite also needs configured KYC template identity/name and an explicit new PIN.

Some KYC, contacts, chat and media selectors remain outside the shared catalog. Consult `UI_IDS.md` before treating those flows as covered by the new contract.

## Configuration and execution

Use the SDK pinned in [global.json](../global.json). Build the correct MAUI app separately from the test APK and match its ABI to the device. Keep credentials and service URLs in local ignored `.env` or environment variables.

Dedicated runners manage flow ordering, cold starts and reports. Read the [runner guide](mobile/andriod/ui/README.md) for effects on account data and PINs.

Optional static check from the repository root, using PowerShell 7:

```powershell
pwsh -File .\tests\scripts\validate-automation-contract.ps1
```

It checks metadata and locale assets without launching an app. Existing migration gaps may produce findings. It does not establish device test success.
