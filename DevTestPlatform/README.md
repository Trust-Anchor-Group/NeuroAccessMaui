# NeuroAccess Dev Test Platform

An independent Android developer application for inspecting **current production views and controls** in repeatable visual states. It references `NeuroAccessMaui.csproj` and reuses production XAML and concrete view models directly.

| Setting | Value |
| --- | --- |
| Title | NeuroAccess Dev Test Platform |
| Android package | `com.tag.neuroaccess.devtestplatform` |
| Target framework | `net10.0-android` |
| Minimum Android API | 24 |
| SDK | [global.json](../global.json), currently 10.0.303 |

## Build and launch

From the repository root, with the pinned SDK, MAUI Android workload and Android SDK installed:

```powershell
dotnet workload restore .\DevTestPlatform\DevTestPlatform.csproj
adb devices -l
dotnet build .\DevTestPlatform\DevTestPlatform.csproj -t:Run -f net10.0-android -c Debug -p:AdbTarget="-s <DEVICE_SERIAL>"
```

Build without launching:

```powershell
dotnet build .\DevTestPlatform\DevTestPlatform.csproj -f net10.0-android -c Debug
```

Choose the device ABI when building an APK for direct installation. The developer package can coexist with `com.tag.NeuroAccess`.

[DevTestPlatform.csproj](DevTestPlatform.csproj) supplies `StorybookReferenceBuild=true`, builds production as a library and uses separate reference output/intermediate directories. Build through the developer project; its reference properties are already configured. Debug embeds assemblies into the developer APK.

## Use the catalog

1. Select Screens, Components or Scenarios.
2. Filter by area, subject or category, or search by name, stable key or tags.
3. Open a scenario to create a fresh production view and view model.
4. Switch to fullscreen to hide scenario chrome. The preview exit action returns to framed mode first, then to the catalog.
5. Use Simulate to display a synthetic outcome in developer diagnostics.

Developer labels use `DevResources.resx` with Swedish translations. Production previews retain production resources.

## Current catalog

| Stable subject key | Scenario keys |
| --- | --- |
| `onboarding.provider` | `default` |
| `onboarding.phone` | `default`, `empty`, `valid`, `invalid`, `loading`, `code-sent`, `backend-error`, `network-error`, `country-us`, `country-se`, `read-only` |
| `verification.phone-code` | `default`, `empty`, `invalid`, `verifying`, `verified`, `expired`, `backend-error` |
| `verification.email-code` | `default`, `empty`, `invalid`, `verifying`, `verified`, `expired`, `backend-error` |
| `onboarding.email` | `default`, `empty`, `valid`, `invalid` |
| `onboarding.username` | `empty`, `valid`, `invalid` (suggestion) |
| `onboarding.account` | `creating` |
| `onboarding.pin` | `empty`, `matching`, `mismatch` |
| `onboarding.biometrics` | `default` |
| `onboarding.finalize` | `default` |
| `components.text-button` | `enabled`, `disabled`, `loading` |
| `components.composite-entry` | `empty`, `valid`, `invalid`, `disabled`, `focused` |

[CatalogRegistry](Catalog/CatalogRegistry.cs) owns these subjects and their deterministic scenario order. Outcome names describe configured fixtures/diagnostics, not successful backend operations.

## Preview behavior and limits

[ProductionScreenAdapter](ProductionAdapters/ProductionScreenAdapter.cs) creates real views with fixture values. [PreviewViewModel](UI/ViewModels/PreviewViewModel.cs) hosts them without replacing their binding contexts or invoking the production page initialization/appearance lifecycle.

Screen previews are input-transparent visual previews. Component previews allow local interaction; the sample button uses a no-op command. Home, KYC, contacts, chat and complete registration flows are outside the current catalog.

[ScenarioSimulationService](Fakes/ScenarioSimulationService.cs) returns localized diagnostic outcomes. Busy simulations stay pending until cancelled. Simulation does not drive production backend commands. [RenderSession](Rendering/RenderSession.cs) owns loaded callbacks and disposes the production view model when released.

Use the shared [Appium/MSTest tests](../tests/README.md) against the production Android/iOS app for real user flows. Sharing IDs does not make the preview catalog an end-to-end target.

## Automation IDs

Production controls retain production IDs. Developer chrome and sample controls use `dev_` IDs. Both hosts configure [AutomationMetadata](../NeuroAccessMaui/UI/Automation/AutomationMetadata.cs).

| Root | ID |
| --- | --- |
| Catalog | `dev_screen_platform_catalog` |
| Preview | `dev_screen_platform_preview` |
| Production phone view | `screen_onboarding_phone` |
| Production verification view | `screen_security_verifycode` |

Catalog rows identify both subject and scenario:

```csharp
AutomationIds.ForKey(
    AutomationIds.DevItemPlatformCatalogScenario,
    "onboarding.phone",
    "valid");
```

See [Automation metadata](../tests/contracts/automation-ids.md) for the full catalog and key encoding. Avoid translated labels, input values and row positions as keys.

## Extend the catalog

1. Register unique subject/scenario keys in `CatalogRegistry`.
2. Add an adapter creating the real production view and concrete view model.
3. Keep fixture values in `Fixtures/` and synthetic outcomes in the simulation service.
4. Add default and Swedish developer resource labels.
5. Register metadata and its owner in `AutomationIds.cs`; the C# tests link this source directly.
6. Update this guide and the automation metadata guide.

Optional static check from the repository root, using PowerShell 7:

```powershell
pwsh -File .\tests\scripts\validate-automation-contract.ps1
```

The check covers ownership, references, dynamic prefixes, retired-ID reuse and production resource-key references. The unit suite validates resource contents separately. Native rendering and end-to-end behavior require device tests.
