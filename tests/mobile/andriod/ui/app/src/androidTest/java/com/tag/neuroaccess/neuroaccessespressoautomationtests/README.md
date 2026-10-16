# Android UI automation source guide

Kotlin/Espresso instrumentation exercises the real MAUI application, package `com.tag.NeuroAccess`. Use the [runner guide](../../../../../../../../README.md) for setup, environment values, commands, logs and reports.

[DevTestPlatform](../../../../../../../../../../../../DevTestPlatform/README.md) is a separate preview application. These tests are not configured to launch or instrument its package.

## Structure

```text
neuroaccessespressoautomationtests/
├── common/                    # Lifecycle, configuration, IDs and expected text
├── framework/                 # Launcher, matching, contracts, waits and scrolling
├── helper/                    # Service clients and generated data
├── flows/                     # Reusable workflows
├── onboarding/                # ID provider and registration entry points
├── identity/                  # Application, approval, update and replacement
├── contacts/                  # Coordinated two-device flow
├── options/                   # Languages and PIN changes
├── quicklogin/                # Web session and identity comparison
└── screens/                   # Feature-specific screen objects
```

| Component | Responsibility |
| --- | --- |
| `BaseTest` | Launches the main app and closes its ActivityScenario afterward |
| `NeuroAccessApp` | Uses the explicit main-app launcher component |
| `UiIds` | Mirrors C# constants and dynamic key encoding |
| `AutomationIdMatcher` | Reads compiled resource IDs or MAUI accessibility-node resource IDs |
| `UiContract` | Checks displayed roots, localized text, enabled states and native inputs |
| `UiText` | Loads independent expectations from `assets/ui-contracts/<locale>.json` |
| `ScreenWaiter` | Waits for observable UI state with bounded timeouts |
| `TestData` | Reads and validates instrumentation arguments |

## Selectors and expected text

```kotlin
onView(AutomationIdMatcher.withAutomationId(
    UiIds.ButtonOnboardingUsernameContinue,
    UiIds.ScreenOnboardingUsername
)).check(matches(isDisplayed()))
```

Use the owning root to disambiguate controls. Composite inputs may need `withAutomationIdOrAncestor` combined with the native input type. Technical matching does not use localized `contentDescription`.

[UI_IDS.md](UI_IDS.md) lists current constants and owners. Dynamic IDs use `UiIds.forKey(...)` with stable schema/record keys. Retired names are reserved.

Use IDs for identity and independent locale assets for expected text. Do not derive expected assertions from observed text.

## Flow constraints

- Registration requires `testRegistrationMode=temporary-phone`. The temporary-phone OTP path skips email; ordinary email onboarding is not yet migrated.
- Registration and PIN changes validate six-digit credentials against repetition/sequence policy.
- KYC selection requires `testKycTemplateItemId` and independent `testKycTemplateName`. Status checks require `testKycReviewStage` (`pending-review` or `finalizing`) where used.
- Locale assets cover en, sv, es, fr, de, da, no, fi, sr, pt, ro and ru.
- The full suite preserves state between dependent phases and requires an explicit new PIN. Use its runner for ordering.
- Mutual contacts uses a dedicated two-device coordinator and is excluded from the single-device full suite.
- Some KYC, contacts, chat and media selectors remain outside the catalog. See the migration notes in `UI_IDS.md`.

## Maintain screen objects

1. Read the C# catalog and `UI_IDS.md`.
2. Change metadata at its owning production view; retain production IDs in developer previews.
3. Mirror constants and dynamic prefixes in `common/UiIds.kt`.
4. Update screen actions and independent locale expectations.
5. Update ID documentation and relevant runner instructions.
6. Select focused validation appropriate to the changed flow.

See [ESPRESSO.md](ESPRESSO.md) for Android Studio usage and the [runner guide](../../../../../../../../README.md) for execution commands and starting states.
