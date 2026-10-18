# Automation metadata contract

`NeuroAccessMaui/UI/Automation/AutomationIds.cs` is the single catalog. `NeuroAccessMaui.Testing` links that platform-free source directly; there is no second catalog to synchronize. Every constant declares its owning production/developer view. Fixed identifiers are unique within the active tree. Dynamic controls use registered prefixes and stable schema or opaque record keys through `AutomationIds.ForKey`.

Android selectors use native resource IDs (`<application package>:id/<identifier>`). iOS selectors use accessibility identifiers. Spoken descriptions and translated text are not used as technical IDs. Language UI checks compare representative controls against the real app resources for an explicitly requested culture; ordinary journeys assert state and business outcomes without cosmetic wording snapshots.

Naming is `<type>_<area>_<view>[_<role>]`, with `dev_` reserved for developer-only chrome. Types are screen, popup, button, input, label, image, control and item. Never reuse a value from `AutomationIds.Retired`. Assign metadata on its owner, not in shared styles. Scope controls to their screen and require a unique visible match. `MobileUi.Click` waits for a control to be enabled, so assert enabled state only where the disabled state matters.

`AutomationIds.ForKey(Prefix, Keys...)` encodes each nonempty UTF-8 key as lowercase hexadecimal, with `_key_` before the first segment. This preserves punctuation, case and segment boundaries. Keys must be schema keys or opaque record IDs, never entered text, PINs, OTPs, translated labels or row indices. Encoding is reversible and is not anonymization; protected artifacts may contain these keys.

Dynamic key sources:

| Prefix | Key |
|---|---|
| ItemSettingsLanguageOption | Language code |
| ItemSharedPhonecountryOption | Country Alpha2 |
| DevItemPlatformCatalogScenario | Subject and scenario keys |
| LabelIdentityCardFieldname / Fieldvalue | Property schema key |
| ItemApplicationsCatalogTemplate / LabelApplicationsCatalogTemplatename / Templatetype | Source.ItemId |
| ItemContactsListContact / ButtonContactsListDetails / Chat | Opaque legal identity |
| LabelContactsChatPeer | Opaque peer legal identity |
| ItemContactsChatSent / Received | First persisted message's ObjectId in the direction group |
| ControlKycProcessPage | Current page's schema ID |
| InputKycProcessField | Identity-property mapping, composite `BDATE`, or unmapped schema ID |
| LabelKycSummaryValue | Summary identity-property mapping |
| ControlOnboardingProfileIdentity | Actual profile state and opaque legal identity ID (automation builds only) |

For example, `AutomationIds.ForKey(AutomationIds.ItemSettingsLanguageOption, "sv")` produces `item_settings_language_option_key_7376`. `AutomationKeyExtension` converts any bound key with `ToString()`, leaves an unbound or empty key unidentified and never writes back to the view model. Message frames use their persisted ID; no message contents enter their technical identifiers.

`tests/scripts/validate-automation-contract.ps1` checks ownership, declarations, dynamic namespaces, references and retired-ID reuse. The unit suite validates the actual localization resources separately. Every fixed identifier requires exactly one owner. The KYC status screen checks its shared-shell heading text and uses IDs for its title and actions.

Every ID is emitted in every build: fixed IDs with `{x:Static automation:AutomationIds.Name}`, keyed IDs with the `AutomationKey` or `KycFieldKey` converters. The only automation-only metadata is `ControlOnboardingProfileIdentity`, set by `UiAutomation/OnboardingPage.Automation.cs`, which shows the profile's identity state on existing onboarding chrome and never changes account state. The `UiAutomation/**` folder, which also holds the fixed-photo KYC camera, compiles only with `EnableUiAutomation=true`.

Metadata does not make a scenario pass or substitute for native rendering checks. Android and iOS require actual device verification, especially for composite controls, animated identity cards, hidden focused OTP entry and dynamic contact/chat controls.
