# Tests

All mobile tests are plain `net10.0` MSTest projects. None references the MAUI app or needs a MAUI workload.

| Project | What it holds | Runs on |
|---|---|---|
| [NeuroAccessMaui.UnitTests](NeuroAccessMaui.UnitTests/NeuroAccessMaui.UnitTests.csproj) | Checks of the automation ID catalog and every `AppResources*.resx` file | Any .NET host |
| [NeuroAccessMaui.UITests](NeuroAccessMaui.UITests/NeuroAccessMaui.UITests.csproj) | Appium scenarios for Android (UiAutomator2) and iOS (XCUITest) | .NET host plus an Appium server and a device |
| [NeuroAccessMaui.Testing](NeuroAccessMaui.Testing/NeuroAccessMaui.Testing.csproj) | Shared support: run settings, the linked app resources and `AutomationIds`, the OTP client | Referenced by the tests only |

`NeuroAccess.Nfc.Test` and `IdApp.Cv.Test` keep their own library suites. All test projects are in `NeuroAccessMaui.sln` under Tests.

## Unit tests

```powershell
pwsh -File tests/scripts/run-tests.ps1 -Suite Unit
pwsh -File tests/scripts/validate-automation-contract.ps1
```

`dotnet test tests/NeuroAccessMaui.UnitTests/NeuroAccessMaui.UnitTests.csproj` works too. `LocalizationResourceTests` catches duplicate or unknown keys, empty values and broken format placeholders in every language.

## UI tests

### Layout

- `Tests/<Feature>`: one test class per feature. Tests read as the user journey and hold the assertions that matter to it.
- `Workflows`: journeys across several screens (registration, languages, PIN change, KYC application, revocation, contacts).
- `Screens`: one class per area of the app. A screen finds and operates its own controls and never creates another screen.
- `Infrastructure`: `BaseUiTest` opens and closes sessions, `UiSession` owns one Appium driver, `MobileUi` finds and operates controls by automation ID, and `Platforms/AndroidPlatform` and `IosPlatform` hold everything that differs between the platforms. Nothing outside `Platforms` branches on the platform.

Controls are found by the IDs in [AutomationIds.cs](../NeuroAccessMaui/UI/Automation/AutomationIds.cs); see [the ID conventions](contracts/automation-ids.md). Expected texts come from the app's own resources through `AppTranslations`, so editing a translation never breaks a test. Build the app and the tests from the same revision.

### Prepare a host

Install the .NET SDK from `global.json`, PowerShell 7 and Node.js 24 with npm 10+. Then install the pinned Appium server and driver on the device host:

```powershell
pwsh -File tests/scripts/install-appium.ps1 -Platform android   # or -Platform ios on the Mac
appium --address 127.0.0.1 --port 4723 --log-level warn
```

Versions are pinned in [toolchain.json](config/toolchain.json). UiAutomator2 8.7 needs API 26 or later; for API 24 and 25 run a second server installed with `-LegacyAndroid`. Never expose an Appium port to the internet.

Use dedicated test devices and accounts. An APK installed by Appium must embed its assemblies: build it with `-p:EmbedAssembliesIntoApk=true` or use a Release APK. When the app version has not changed, reinstall it yourself (`adb -s <device> install -r <apk>`), because Appium keeps an installed copy.

### Run

```powershell
$env:NEUROACCESS_TEST_DEVICE_ID = 'emulator-5554'
$env:NEUROACCESS_TEST_APP_PATH = 'C:\build\com.tag.NeuroAccess.apk'   # optional when already installed
pwsh -File tests/scripts/run-tests.ps1 -Suite UI -Platform android -Filter 'TestCategory=Smoke'
```

For iOS, set the simulator or device UDID and the Mac's `APPIUM_URL`, and use `-Platform ios`. Set `NEUROACCESS_TEST_IOS_SIMULATOR=false` for a physical device and pass signing capabilities as JSON in `NEUROACCESS_TEST_CAPABILITIES`, for example `{"xcodeOrgId":"TEAM_ID","xcodeSigningId":"Apple Development"}`.

The runner writes a TRX report under `TestResults/mobile`, fails on failed tests, and exits with 2 when any selected test was skipped or inconclusive (`-AllowIncomplete` accepts that). Plain `dotnet test` with `--settings tests/config/android.runsettings` and `NEUROACCESS_TEST_ENABLED=true` also works, as does Test Explorer with a local copy of a runsettings file. Without `ENABLED=true` every UI test is skipped. Run one category at a time, because many tests change the account.

### Categories

| Filter | Needs | Changes |
|---|---|---|
| `Smoke` | Provider screen; app language matches `LANGUAGE_CODE` | Nothing |
| `Languages` | Dedicated emulator or simulator, `ALLOW_RESET=true` | Clears app data for each of the twelve languages |
| `Registration` | `ALLOW_REGISTRATION=true`, `REGISTRATION_MODE=temporary-phone`, `PHONE_NUMBER`, `PIN`, `OTP_ENDPOINT` | Creates an account |
| `Pin` | Approved identity, biometrics off, `PIN`, `NEW_PIN`, `ALLOW_PIN_CHANGE=true` | Changes the PIN |
| `QuickLogin` | Approved identity, `PIN`, camera permission, `QUICK_LOGIN_PAGE_URL`, Chrome or `BROWSER_URL` | Signs in on the web page |
| `Contacts` | Two approved accounts, English UI, `ALLOW_CONTACTS=true`, the `_A`/`_B` settings below | Adds contacts and exchanges messages |
| `KycCamera` | An In Progress draft saved at its photo step with no photo, `ALLOW_KYC_DRAFT_UPDATE=true` | Saves a photo to the draft |
| `KycApplication` | KYC settings below | Submits an application |
| `KycApproval` | KYC settings plus `MANUAL_APPROVAL=true` and an administrator | Submits and waits for approval |
| `KycUpdate` | As KycApproval; two approvals | Approves Oscar, then Albin |
| `KycReplacement` | As KycApproval plus `REVOKE_IDENTITY=true`, `PHONE_NUMBER`, `OTP_ENDPOINT` | Revokes the identity and approves a replacement |
| `Kyc` | All four KYC scenarios above, in no fixed order | |
| `FullSuite` | Automation build, `ALLOW_RESET`, the registration, KYC and PIN settings | Every language, registration, an application and a PIN change, in order |

After a PIN change, swap `PIN` and `NEW_PIN` before the next run. Keep biometrics off so the PIN popup appears. The system notification prompt is only handled in English.

### KYC scenarios

Start each KYC scenario on Home of a prepared account with no pending application. Approval, update and replacement need an account that has not had a personal identity approved yet, so the app shows its first Unlocked notification. An administrator approves each application by hand within `MANUAL_APPROVAL_TIMEOUT_MINUTES` (default 10, 1 to 60). Use the automation build (below), English app and English (United States) device locale, and biometrics off.

| Setting | Meaning |
|---|---|
| `ALLOW_KYC_SUBMISSION` | `true` to allow submitting applications |
| `KYC_TEMPLATE_ITEM_ID`, `KYC_TEMPLATE_NAME` | The photo-based template to apply with, by ID and English name |
| `KYC_PHOTO_FIELD_ID` | The photo field; defaults to `selfieUpload` |
| `PERSONAL_NUMBER_AGE_GROUP` | `adult-2000s` (default), `older-adult`, `extreme-old` or `infant-2026` |
| `SOCIAL_SECURITY_NUMBER` | Optional nine digits; names and gender are derived from it |
| `KYC_NEW_APPLICATION` | `false` resumes the current application instead (application and approval only) |
| `KYC_REVIEW_STAGE` | `pending-review` or `finalizing`, required when resuming |

The template needs the personal and address mappings listed in the [coverage inventory](../plans/mobile-ui-testing/coverage.md), a document type `None` and no organization. Inputs are found by their identity mapping (`FIRST`, `PNR`, `BDATE`), so renamed fields keep working. `extreme-old` uses 1890-01-01, below the app's 1900 minimum, so it is expected to fail at the date picker.

### Automation build

Normal builds use the device camera. The automation build returns a fixed JPEG from the KYC camera page instead, so KYC runs work on emulators and give the same photo every time:

```powershell
dotnet build NeuroAccessMaui/NeuroAccessMaui.csproj -f net10.0-android -c Debug -p:EnableUiAutomation=true
```

The APK is under `NeuroAccessMaui/bin/UiAutomation/Debug/net10.0-android/`. The build also adds an onboarding marker that shows the profile's identity state, which the replacement scenario reads. Everything else, including QR scanning and the backend, is the real app. Release builds and publishing refuse `EnableUiAutomation=true`. The automation build keeps the app's package ID, so install it only on test devices.

`KycCamera` runs on either build: with the normal build, grant camera permission first.

### Settings

Each setting is a `<Parameter name="NAME" value="..."/>` in a `.runsettings` file or a `NEUROACCESS_TEST_NAME` environment variable, which wins. Keep personal values in `*.local.runsettings` or `*.local.json`, which git ignores.

Common settings: `PLATFORM` (`Android` or `iOS`), `DEVICE_ID`, `APPIUM_URL`, `APP_ID`, `APP_PATH`, `PLATFORM_VERSION`, `LANGUAGE_CODE`, `SYSTEM_LANGUAGE_CODE`, `APP_ACTIVITY`, `SYSTEM_PORT`, `WDA_LOCAL_PORT`, `IOS_SIMULATOR` and `CAPABILITIES` (extra scalar capabilities as JSON). `RESET_BEFORE_TEST=true` clears app data when a session opens; any reset needs `ALLOW_RESET=true`, and on iOS only runs on a simulator.

`PHONE_NUMBER` is digits without the country code; registration selects the USA and sends `+1`. `REGISTRATION_USERNAME_TIMESTAMP` overrides the generated `yyyyMMddHHmmss` username.

Contacts uses two roles: `DEVICE_ID_A`/`_B`, `PIN_A`/`_B`, and optionally `PLATFORM`, `APP_ID`, `APPIUM_URL`, `APP_PATH`, `CAPABILITIES`, `SYSTEM_PORT` and `WDA_LOCAL_PORT` with `_A`/`_B`. A role falls back to the common setting. Two sessions on one Appium server need different `SYSTEM_PORT` or `WDA_LOCAL_PORT` values. On a physical iPhone, `WDA_BUNDLE_ID` names the signed WebDriverAgent used to read the clipboard.

`CAPTURE_SENSITIVE_ARTIFACTS=true` saves a screenshot and the page source when a test fails. They can show personal data, so it is off by default. Failure messages show expected and actual values except for PINs and verification codes.

## CI and device matrices

The [Tests workflow](../.github/workflows/tests.yml) runs the unit tests and the automation ID check on pull requests. Started by hand, it also runs one UI category on a self-hosted runner labelled `mobile-ui` plus `android` or `ios`, using the `mobile-ui-android` or `mobile-ui-ios` environment for device variables, test secrets and opt-ins. It installs nothing: build and install the app, or set `APP_PATH`, before the UI job. With `ALLOW_RESET=true` it clears app data before Smoke and Registration.

To run a category on several existing devices, copy [devices.example.json](config/devices.example.json) to `devices.local.json`, edit it, and run:

```powershell
pwsh -File tests/scripts/run-device-matrix.ps1 -Matrix tests/config/devices.local.json -Filter 'TestCategory=Smoke'
```

For emulator matrices across Android versions and hardware, see [managed emulators](infrastructure/vm/readme.md).

## Adding a test

1. Add any new controls to `AutomationIds.cs` with their owner file, set them in XAML with `AutomationId="{x:Static automation:AutomationIds.Name}"`, and run `validate-automation-contract.ps1`.
2. Add the steps to the screen that owns the controls. Let `Ui.Click` wait for a control to be enabled; assert `Enabled(..., false)` only where the disabled state matters.
3. If the journey crosses several screens and other tests need it, add or extend a workflow.
4. Add the test to `Tests/<Feature>` as `Test_NN_Description` in a class that derives from `BaseUiTest` and has a `TestCategory`. Read and validate the settings the test needs before calling `this.Open()`, so a missing setting fails before the app is touched.
5. Put any Android or iOS difference in `AndroidPlatform` and `IosPlatform`, never in a test or screen.

Logic that does not need a device belongs in a unit test. Link a small platform-free source file into the unit test project rather than copying it.
