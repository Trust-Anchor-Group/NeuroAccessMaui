# Provisioned Android and iOS hosts

All test execution uses [the shared .NET/Appium runner](../../README.md). Point `.runsettings` or environment settings at a local or remote Appium endpoint. GitHub Actions or the organization's CI scheduler provides job queues and report retention.

Existing devices: use `tests/scripts/run-device-matrix.ps1` with a JSON target list. The .NET runner can be on Windows, Linux or macOS. iOS Appium normally runs on a provisioned Mac with Xcode and signed WebDriverAgent. A remote `APP_PATH` must refer to a file on the device host.

## Android profile matrices

The five JSON matrices under `android/config/matrices/` configure hardware profiles, API levels, ABIs, system images, enabled flags, storage policies and MSTest category filters. The smoke and onboarding hardware matrices each contain 90 combinations. API-specific matrices cover supported application versions starting at API 24. Device configuration lives directly in each runnable profile.

On a Linux/macOS device host with Bash, PowerShell 7, Android SDK tools on PATH, .NET and an already running Appium server:

```powershell
$env:NEUROACCESS_TEST_APP_PATH = '/build/com.tag.NeuroAccess.apk'
$env:NEUROACCESS_TEST_ALLOW_RESET = 'true'
pwsh -File tests/scripts/run-android-profiles.ps1 -Matrix tests/infrastructure/vm/android/config/matrices/smoke/64-bit/device-profiles-x86_64.json
```

The emulator runs headless unless you pass `-ShowEmulator`. `prepare-emulator.sh` creates and boots the AVD, and `wait-for-android.sh` waits for boot, turns off animations and keeps the screen on (`svc power stayon true`) so long KYC waits never reach the lock screen. On physical devices, turn on Stay awake (Android) or set Auto-Lock to Never (iOS). The C# runner clears only the configured application's storage before each selected scenario. Registration matrices also need the registration settings and opt-in described in `tests/README.md`; `NEUROACCESS_TEST_PHONE_NUMBER` is the first number, and each registering profile uses the next one with its own username, so accounts never collide.

Each profile receives fresh TRX output. Failures do not prevent independent later profiles from running. A `profiles.json` summary includes failed and blocked entries, and any incomplete profile makes the matrix command fail. Profiles below the application's API 24 minimum are rejected. For API 24/25 supply `NEUROACCESS_TEST_APPIUM_URL_LEGACY` pointing to a separate server with UiAutomator2 5.0.0. Other profiles use the pinned current driver, which requires API 26 or newer.

The runner stops only its managed emulator. `storage_policy=delete` removes only that named `neuro_appium_*` AVD. Shared SDK images and unrelated AVDs are retained. `cache` keeps the AVD but app data is still reset for each profile. APK ABI compatibility remains a prerequisite; use separate runs/artifacts for x86 and x86_64. Run one managed matrix per host at a time to avoid competing AVD/port allocation.
