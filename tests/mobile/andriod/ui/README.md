# NeuroAccess – Android UI Tests

Shared guide to running this project's tests, including PowerShell commands,
prerequisites, logs, and reports. Open two terminals side by side: **Terminal 1 runs
the tests**, and **Terminal 2 displays the logs**. Start the logs first.

## 1. Prepare the device and test project

- Install Android Studio/Android SDK and configure the project's Java/Gradle environment.
- Install a NeuroAccess **debug app** built for the phone's or emulator's architecture.
  The tests run the real MAUI app, package `com.tag.NeuroAccess`.
- Connect a phone with USB debugging authorized, or start an emulator.
- Prefer one connected device. The standard `connectedDebugAndroidTest` runs on
  connected devices; the dedicated runners support `-PdeviceSerial=<SERIAL>`.
- For tests using an existing account, use English, the current PIN, and the required identity state.
  Quick Login requires camera permission and PIN authentication with biometrics disabled.

List devices from any PowerShell terminal:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices -l
```

The device should appear as `device`, not `unauthorized` or `offline`.
Adjust the ADB path if Android SDK is installed elsewhere.

### Local configuration

Create or update `.env` in
`C:\dev\Neuro-Access\NeuroAccessMaui\tests\mobile\andriod\ui`.
Preserve existing settings needed by other tests.

```dotenv
NEUROACCESS_TEST_PHONE_NUMBER="<phone number without country code, digits only>"
NEUROACCESS_TEST_PIN="<current PIN, exactly six digits>"
NEUROACCESS_TEST_NEW_PIN="<different PIN, exactly six digits>"
NEUROACCESS_TEST_OTP_ENDPOINT="<test environment OTP URL>"
NEUROACCESS_TEST_QUICK_LOGIN_PAGE_URL="<HTTPS URL of the Quick Login page>"
```

- Registration requires the phone number, PIN, and OTP endpoint.
- Existing-account flows require the current PIN; Quick Login does not use the OTP helper.
- Quick Login requires `NEUROACCESS_TEST_QUICK_LOGIN_PAGE_URL`; there is no hard-coded default. Keep the actual URL in the Git-ignored `.env` file. The page URL is omitted from the custom test logs.
- Changing the PIN requires the current and new PIN. The full suite can derive a new PIN if omitted.
- Environment variables take precedence over `.env`.
- `NEUROACCESS_TEST_PERSONAL_NUMBER_AGE_GROUP` and `NEUROACCESS_TEST_SSN` are
  optional identity test-data settings; supported age groups are defined in
  `helper/PersonalIdentityTestDataHelper.kt`.
- Update `NEUROACCESS_TEST_PIN` after a run that changes the PIN. A later test
  failure can occur after the new PIN has already been saved.

## 2. Terminal 2 – follow logs alongside the tests

### General test logs

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" logcat -v time -T 1 "TestRunner:I" "QuickLoginTest:I" "AndroidRuntime:E" "*:S"
```

This shows test starts/failures, Quick Login checks, and Android crashes.
It is a filtered selection: also read the error output in Terminal 1 and the report.

### Quick Login only

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" logcat -v time -T 1 "QuickLoginTest:I" "*:S"
```

With multiple connected devices, insert `-s <SERIAL>` between the quoted ADB path and `logcat`.
In Android Studio's Logcat, filter Quick Login messages with `tag:QuickLoginTest`.

`Ctrl+C` in the log terminal stops log viewing, not the test in Terminal 1.
`-T 1` may show an older line initially; check the timestamp and latest START message.

## 3. Terminal 1 – choose a test run

Run this setup block in the test terminal. The absolute path works even if you are already in `ui`:

```powershell
Set-Location -LiteralPath 'C:\dev\Neuro-Access\NeuroAccessMaui\tests\mobile\andriod\ui'
$TestPackage = 'com.tag.neuroaccess.neuroaccessespressoautomationtests'
```

Then run **one** of the commands below. Do not paste all runs in sequence:
tests require different starting states, and some change the account, identity, or PIN.

### Full suite – multiple flows in the required order

**This run clears NeuroAccess local app data several times and changes the PIN.**
Use a test account/device whose local data can be replaced.

```powershell
.\gradlew.bat :app:fullAndroidTestSuite --console=plain
```

Select a specific device for this run:

```powershell
.\gradlew.bat :app:fullAndroidTestSuite "-PdeviceSerial=<SERIAL>" --console=plain
```

The existing full suite runs:

1. Cold-start checks for 12 languages: en, sv, es, fr, de, da, no, fi, sr, pt, ro, ru.
2. ID provider navigation from clean app data.
3. Registration from clean app data.
4. A personal ID application using the registered account.
5. A PIN change and verification of the new PIN after a real cold start.

Dependent flows are skipped if their prerequisite fails.
The runner is `scripts/run-full-test-suite.ps1`; use the Gradle command above
so that APK paths, configuration, and the report directory are supplied.

**Quick Login, manual ID approval, ID updates, and ID replacement are not yet included
in the full suite.** Their commands are collected below in the same guide. Running
`connectedDebugAndroidTest` without a class filter is not equivalent to the full suite:
it does not guarantee the required order, resets, or separate cold starts.

### Overview of all current test classes

| Test class | Starting state and effect | Run using |
| --- | --- | --- |
| `IdProviderTest` | Onboarding; navigates to phone verification | Class filter below or full suite |
| `RegistrationFlowTest` | Onboarding; registers an account and reaches Home | Class filter below or full suite |
| `LanguageOptionsFlowTest` | Runner clears app data and checks languages across cold starts | `languageOptionsColdStartTest` |
| `PersonalIdApplicationFlowTest` | Registered account on Home; submits an ID application | Class filter below or full suite |
| `PersonalIdApprovalFlowTest` | Registered account on Home; submits an application and waits for admin approval | Class filter + `manualApproval` |
| `PersonalIdUpdateFlowTest` | Registered account on Home; two applications/approvals, Oscar → Albin | Class filter + `manualApproval` |
| `PersonalIdReplacementFlowTest` | Registered account on Home; approves, revokes, and replaces an ID | Class filter + `manualApproval` + `revokeIdentity` |
| `ChangePinFlowTest` | Account with a personal identity; changes PIN and verifies after cold start | `changePinTest` or full suite |
| `QuickLoginFlowTest` | Home, approved identity, and camera permission; approves a web login | Class filter below |

Identity tests must be able to start the application expected by their flow.
An old pending application is not automatically a suitable starting state.

### Languages with cold start

**Clears local app data before each language.**

```powershell
.\gradlew.bat :app:languageOptionsColdStartTest --console=plain
```

Use the runner rather than an unordered execution of the language class's two methods.
It selects the language, stops the app, and verifies translations in a new process.
`-PdeviceSerial=<SERIAL>` is supported.

### ID provider

Start from onboarding:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.onboarding.IdProviderTest" --console=plain
```

### Registration

Start from onboarding. Configure the phone number, PIN, and OTP endpoint:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.onboarding.RegistrationFlowTest" --console=plain
```

### Submit a personal ID application

Start with a registered account on Home:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.identity.PersonalIdApplicationFlowTest" --console=plain
```

### Submit an application and wait for manual approval

Approve the application in admin while the test waits:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.identity.PersonalIdApprovalFlowTest" "-Pandroid.testInstrumentationRunnerArguments.manualApproval=true" "-Pandroid.testInstrumentationRunnerArguments.manualApprovalTimeoutMinutes=10" --console=plain
```

### Update a personal identity

The test first submits an application for Oscar and then one for Albin.
Approve both in admin when each request arrives:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.identity.PersonalIdUpdateFlowTest" "-Pandroid.testInstrumentationRunnerArguments.manualApproval=true" "-Pandroid.testInstrumentationRunnerArguments.manualApprovalTimeoutMinutes=10" --console=plain
```

### Revoke and replace a personal identity

**The test revokes the first ID after approval and creates a replacement.**
Two manual approvals are required. OTP configuration is needed for reverification.

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.identity.PersonalIdReplacementFlowTest" "-Pandroid.testInstrumentationRunnerArguments.manualApproval=true" "-Pandroid.testInstrumentationRunnerArguments.revokeIdentity=true" "-Pandroid.testInstrumentationRunnerArguments.manualApprovalTimeoutMinutes=10" --console=plain
```

The manual approval timeout accepts 1–60 minutes.
These manual tests are skipped without their enabling arguments.

### Change PIN and verify after cold start

The existing account and identity are preserved, but the PIN changes:

```powershell
.\gradlew.bat :app:changePinTest --console=plain
```

The runner handles both phases with a force-stop between them.
Do not run the class's two methods in an unspecified order.
`-PdeviceSerial=<SERIAL>` is supported. Both the current and new PIN are required.

### Quick Login – compare the mobile ID with the web page

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest "-Pandroid.testInstrumentationRunnerArguments.class=$TestPackage.quicklogin.QuickLoginFlowTest" --console=plain
```

The flow is Show ID → Swipe for details → read Neuro-ID → Home → Scan QR →
Enter QR manually → retrieve the page's QR link in the background → Open → Accept → PIN →
check the web page's table.

The test opens the URL configured in `NEUROACCESS_TEST_QUICK_LOGIN_PAGE_URL` in a background WebView.
The page's JavaScript uses the `/QuickLogin` API. The QR link is read from the page's
`quickLoginA`, and the same web session receives the result after approval.
No separate Chrome tab opens.

In Terminal 2, a successful comparison should produce messages like these:

```text
START: Quick Login identity comparison
MOBILE: Show ID / Neuro-ID = <mobile ID>
WEB: loading configured Quick Login page (URL omitted)
WEB: QR link read from #quickLoginA (payload omitted)
WEB: waiting for Successfully logged in. and Identity of user.
WEB: Identity of user. / Id = <web page ID>
WEB: Identity of user. / State = Approved
PASS: mobile Neuro-ID equals web Id; State=Approved; required fields present
```

**MOBILE** is read from the app. **WEB … Id** is read separately from the
**Identity of user.** table after the page displays **Successfully logged in.**
PASS requires an exact ID match, Approved state, a matching Provider, and required fields.
Different IDs or a timeout while waiting for the page produce FAIL. Earlier failures
may abort without a dedicated Quick Login FAIL message; check the test terminal.

Quick Login logging includes identity IDs but not the PIN, temporary QR payload,
or signature values. A successful end-to-end run of the new Quick Login flow has not yet been verified.

## 4. Reports and troubleshooting

Report paths below are relative to `tests/mobile/andriod/ui`:

| Run | HTML report |
| --- | --- |
| Class filter / connectedDebugAndroidTest | `app/build/reports/androidTests/connected/debug/index.html` |
| Full suite | `app/build/reports/androidTests/full-suite/index.html` |
| Languages with cold start | `app/build/reports/androidTests/language-options-cold-start/index.html` |
| PIN change | `app/build/reports/androidTests/change-pin/index.html` |

Connected-test XML results and device logs are under
`app/build/outputs/androidTest-results/connected/debug`.
Runner JUnit XML reports are under `app/build/reports/androidTests/results`.

- **98% EXECUTING:** the instrumentation test is still running; the percentage does not identify the waiting UI step.
- **SKIPPED:** check the arguments and whether an earlier prerequisite in the full suite failed.
- **Incorrect PIN after a run:** update the configuration to the PIN currently stored on the device.
- **Wrong starting screen:** onboarding tests require onboarding; identity/Quick Login tests require a registered account.
- **The app does not appear to start:** read the first error in the report. All flow tests inherit
  `BaseTest`, whose JUnit `@Before` launches the app.
- **Wrong APK/architecture or missing AutomationIds:** build and install the correct MAUI debug version.
  Building the tests does not automatically rebuild the MAUI source.
- **No PASS in the log:** check the latest START and the final test result.
  An old PASS message does not prove the new run succeeded.

### Clear app data for an onboarding test

This removes the local NeuroAccess account and data on the selected device.
Do not use it before Quick Login if you want to keep your approved identity.

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" -s <SERIAL> shell pm clear com.tag.NeuroAccess
```

### Build only the test APK

```powershell
.\gradlew.bat :app:assembleDebugAndroidTest --console=plain
```

### Build the MAUI app separately

Requires the .NET SDK and MAUI Android workload. Build for the device architecture
you use, then install/start the correct debug version through your development environment.

```powershell
dotnet build 'C:\dev\Neuro-Access\NeuroAccessMaui\NeuroAccessMaui\NeuroAccessMaui.csproj' -f net10.0-android -c Debug
```

## 5. Code structure

Source code is under `app/src/androidTest/java/com/tag/neuroaccess/neuroaccessespressoautomationtests`.

| Directory/component | Responsibility |
| --- | --- |
| `common/BaseTest.kt` | Launches the app before each test and closes its ActivityScenario afterward |
| `common/TestData.kt` | Reads configured test data |
| `framework/` | App startup, AutomationId matching, and waits |
| `screens/` | Reusable UI steps |
| `helper/` | Network clients and generated test data, including OTP and Quick Login clients |
| `onboarding/` | ID provider and registration |
| `options/` | Languages and PIN changes |
| `identity/` | Application, approval, update, and replacement |
| `quicklogin/` | Quick Login test flow, browser session, and comparison with the web table |
| `scripts/` in the project root | Execution order, cold starts, and combined reports |

Use MAUI AutomationId where available. The matcher reads native resource IDs or
accessibility-node resource IDs, not localized contentDescription.
Animated views also use bounded direct UI waits and gestures; some steps use English
labels where AutomationIds are unavailable.
