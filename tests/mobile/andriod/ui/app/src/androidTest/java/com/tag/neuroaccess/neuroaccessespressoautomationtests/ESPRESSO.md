# Espresso and Android Studio

Use Espresso for real main-app flows. See the [runner guide](../../../../../../../../README.md) for configuration and commands, and the [source guide](README.md) for screen objects.

## Open the project

1. Open `tests/mobile/andriod/ui` in Android Studio, in the directory containing `settings.gradle.kts` and `gradlew.bat`.
2. Configure local `local.properties` with `sdk.dir` and sync Gradle.
3. Use the daemon JVM configured in `gradle/gradle-daemon-jvm.properties` (currently Java 25). Source compile compatibility is a separate setting.
4. Start an emulator or connect an authorized device.
5. Build and deploy the main MAUI app for the device ABI.

Instrumentation targets `com.tag.NeuroAccess` and uses the MAUI debug keystore configured in `app/build.gradle.kts`. Installing DevTestPlatform alone does not satisfy the target.

## Choose an entry point

Use the gutter icon for standalone tests whose starting-state requirements are met. For ordering and cold starts, use dedicated tasks/run configurations:

| Gradle task | Purpose |
| --- | --- |
| `:app:fullAndroidTestSuite` | Ordered single-device suite with resets and dependencies |
| `:app:languageOptionsColdStartTest` | Language selection and separate cold-start verification |
| `:app:changePinTest` | PIN change and new-process verification |
| `:app:mutualContactsTest` | Coordinated two-device contact/chat flow |

Running all classes from the package gutter or unfiltered `connectedDebugAndroidTest` does not reproduce suite ordering. Read the runner guide before runs that clear data, change PINs or alter identities.

Compile instrumentation sources from the test project directory:

```powershell
.\gradlew.bat :app:compileDebugAndroidTestKotlin
```

## Selector examples

```kotlin
onView(AutomationIdMatcher.withAutomationId(
    UiIds.InputOnboardingUsernameValue,
    UiIds.ScreenOnboardingUsername
)).check(matches(isDisplayed()))

onView(AutomationIdMatcher.withAutomationId(
    UiIds.ButtonOnboardingUsernameContinue,
    UiIds.ScreenOnboardingUsername
)).perform(click())
```

For composite inputs, use screen helpers or `UiContract.enterText` to select the native input correctly. Expected text comes from independent locale contracts; identity comes from [UI_IDS.md](UI_IDS.md).

## Troubleshooting

- Missing element: check the owning root, current ID and installed main-app APK. Uncatalogued selectors are migration gaps.
- Ambiguous element: include the owning root and encoded stable key for dynamic rows.
- Device unavailable: use `adb devices -l` and resolve `offline` or `unauthorized`.
- Missing target: install the main app with matching ABI and debug signing identity.
- Missing configuration: read the first instrumentation error and the runner guide's `.env` section.
- Wrong starting screen or PIN: restore the flow's required account state and configure the PIN currently on the device.
- Developer preview issue: consult [DevTestPlatform](../../../../../../../../../../../../DevTestPlatform/README.md). A main-app result does not verify the developer host.
