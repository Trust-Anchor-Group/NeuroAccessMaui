# Onboarding Guide

This guide explains how the Neuro-Access onboarding experience is stitched together. It is written for new contributors who need to understand the moving parts before touching any code.

---

## High-Level Flow

1. `OnboardingPage.xaml` hosts the chrome (language switcher, help/back buttons) and a `ViewSwitcher` containing each onboarding page.
2. `OnboardingViewModel` acts as the coordinator. It builds a deterministic sequence of `OnboardingStep` values for the current scenario, evaluates skip guards at navigation time, and exposes commands (`GoToNext`, `GoBack`, `GoToStep`).
3. Each visual step has its own view model under `UI/Pages/Onboarding/ViewModels`. They inherit from `BaseOnboardingStepViewModel`, handle validation and networking, and call back to the coordinator when it is time to advance.
4. Shared services (`ServiceRef`) provide persistence (`TagProfile`), networking, localization, popups, etc. The step view models never talk directly to UI controls.

Keep the PlantUML diagram in `docs/OnboardingStateMachine.puml` open while reading; it mirrors the logic described here.

---

## Scenarios & Default Sequences

`OnboardingScenario` controls which steps are included initially. `OnboardingViewModel.GetBaseSequence` produces the ordered list:

| Scenario | When it is used | Default sequence |
| --- | --- | --- |
| `FullSetup` | First run on a fresh installation. | Welcome → ValidatePhone → ValidateEmail → NameEntry → CreateAccount → DefinePassword → Biometrics → Finalize |
| `ReverifyIdentity` | Users re-run identity proofing to refresh their legal identity. | ValidatePhone → ValidateEmail → CreateAccount → Finalize |
| `ChangePin` | Only reset the device PIN / password and biometric enrollment. | DefinePassword → Biometrics → Finalize |

The coordinator always keeps the base sequence intact so that analytics and logging stay deterministic. Skip logic is evaluated when navigating: if a guard fires (e.g., the profile already has an approved identity), the step is transparently bypassed but still appears in logs.

---

## Step Summary

- **Welcome** (`WelcomeOnboardingStepViewModel`)
  - Handles QR / deep-link invitations, validates the payload, and creates an `OnboardingTransferContext` containing imported credentials.
  - Applies the transfer immediately; if it includes both an account and an approved identity, onboarding jumps to `DefinePassword`.
  - Never skipped to ensure every user sees branding, language selection, and help affordances.
- **Validate Phone** (`ValidatePhoneOnboardingStepViewModel`)
  - Renders country picker + phone validation, then sends OTPs through `/ID/SendVerificationMessage.ws` and gathers the code via `VerifyCodePage`.
  - Writes phone + domain info to `TagProfile` and stamps `TestOtpTimestamp` when the backend marks the code as `Temporary` (used for +1555 test numbers).
  - Temporary/test OTPs skip `ValidateEmail`, taking users to `NameEntry` during FullSetup or `CreateAccount` when re-verifying an identity.
  - Guard: skipped when not `ReverifyIdentity` and the profile (or transfer payload) already has an established identity.
- **Validate Email** (`ValidateEmailOnboardingStepViewModel`)
  - Mirrors the phone flow. The step is automatically skipped when `TagProfile.TestOtpTimestamp` is set, because temporary/test OTPs already cover email validation.
  - When a transfer is pending it calls `TryFinalizeTransferAsync`; success means the flow jumps to `DefinePassword`.
- **Name Entry** (`NameEntryOnboardingStepViewModel`)
  - Suggests or accepts an account name, kicking off account creation.
  - Only appears in `FullSetup`; re-verification skips it entirely because the account is already established.
- **Create Account** (`CreateAccountOnboardingStepViewModel`)
  - Ensures the XMPP connection is established, creates/logs into the account, and submits the legal identity application.
  - Guard: skipped when `ReverifyIdentity` is not active and either a transfer payload already includes a legal identity or the profile contains an approved one.
- **Define Password** (`DefinePasswordOnboardingStepViewModel`)
  - Collects and validates the device PIN/password strength, writes it to `TagProfile`, and optionally shows a warning if a test OTP was used.
  - Guard: skipped in FullSetup when a local password already exists and we are not resuming a verified identity.
- **Biometrics** (`BiometricsOnboardingStepViewModel`)
  - Prompts for biometric enrollment when supported.
  - Guard: skipped if biometrics are already enabled or if the platform does not support them.
- **Finalize** (`FinalizeOnboardingStepViewModel`)
  - Summary/CTA screen. On completion, `OnboardingViewModel` navigates back to `LoadingPage` so the standard startup pipeline resumes.

Any additional helper screens (e.g., `ContactSupport`) must still be registered with the coordinator and `OnboardingPage.xaml` even if they are rarely shown.

---

## Skip Guards & Logging

`TryGetSkipReason` centralizes **every** guard, ensuring the same decision is respected during initial sequence building and at runtime:

- Completed steps are remembered via `MarkStepCompleted` so re-entry skips them instantly.
- Transfer payloads can provide accounts, domains, and even approved legal identities; these scenarios set reasons like `AccountTransferred` or `IdentityAlreadyEstablished`.
- Temporary OTPs mark `TestOtpTimestamp`, and the email step reports `TemporaryOtpUsed` so telemetry clearly shows why the step is skipped.
- Platform/feature capability checks (biometrics) record `BiometricsUnavailable`.

Whenever a guard short-circuits a step, the coordinator logs the reason exactly once. This is invaluable when correlating onboarding telemetry with customer reports.

---

## Transfers & Temporary OTPs

- **Transfers** start at the Welcome step. After decrypting an `onboarding:` payload the app builds `OnboardingTransferContext`. The coordinator then:
  - Applies the provided domain/account credentials via `ApplyTransferContextAsync`.
  - Rebuilds the sequence (which typically removes account/name/password steps if they came from the transfer).
  - Calls `TryFinalizeTransferAsync` whenever the email verification or transfer process might have enough data to finish immediately.
- **Temporary/Test OTPs** (phone numbers beginning with +1555 in non-production environments) are flagged by the backend. When the phone step receives `Temporary == true` it sets `TestOtpTimestamp` and the coordinator automatically jumps over ValidateEmail. The DefinePassword step later reminds the tester that the app will expire after roughly a week unless reinstalled.

---

## Adding or Modifying Steps

1. **Create the View + ViewModel**
   - Add a new view model under `UI/Pages/Onboarding/ViewModels` inheriting from `BaseOnboardingStepViewModel`.
   - Add the corresponding XAML view under `UI/Pages/Onboarding/Views` and register it inside `OnboardingPage.xaml`’s `ViewSwitcher`.
2. **Update the Coordinator**
   - Extend `OnboardingStep` enum if needed.
   - Update `GetBaseSequence` ordering for the relevant scenarios.
   - Add a call to `AddDescriptor` in `InitializeDescriptors` and define skip logic in `TryGetSkipReason`.
3. **Respect MVVM Rules**
   - Do not manipulate UI elements directly from view models. Use bindings, commands, and services (`ServiceRef.UiService`, `PopupService`, etc.).
4. **Keep Documentation in Sync**
   - Update this guide and the PlantUML diagram when the flow changes.

---

## Debugging Tips

- Enable verbose logging: onboarding logs include `"Onboarding step skipped"`, `"Runtime skipped onboarding step"`, and `"Onboarding step marked as completed"` entries. They expose the step name and reason, which makes diagnosing guard issues straightforward.
- Use +1555 phone numbers in non-production to exercise the temporary OTP path. Confirm that `ValidateEmail` is skipped and that `DefinePassword` shows the warning toast.
- When testing transfers, keep the debugger open in `ApplyTransferContextAsync` to verify which steps drop out of the sequence.

---

## Related Files

- `NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml`
- `NeuroAccessMaui/UI/Pages/Onboarding/OnboardingViewModel.cs`
- `NeuroAccessMaui/UI/Pages/Onboarding/ViewModels/*`
- `docs/OnboardingStateMachine.puml`
- `Onboarding.md` (deep-dive reference)

Refer to these files whenever you need concrete implementation details beyond what this guide covers.
