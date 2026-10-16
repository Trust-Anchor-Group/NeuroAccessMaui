# UI ID contract

This reference follows the current production-owned `AutomationIds.cs` catalog. `common/UiIds.kt` mirrors its constants for Kotlin tests. The tables below include fixed IDs and dynamic prefixes; a dynamic prefix must be encoded with an instance key before matching.

## Applications and ownership

| Host | Package | Identity behavior |
| --- | --- | --- |
| Main application | `com.tag.NeuroAccess` | Owns production views and their IDs |
| DevTestPlatform | `com.tag.neuroaccess.devtestplatform` | Reuses production IDs; developer chrome/sample controls use `dev_` IDs |
| Existing instrumentation | Targets `com.tag.NeuroAccess` | Launcher and manifest still target the main app |

An ID identifies a logical control, independently of the host package, localized label or entered value. Sharing IDs does not make the current instrumentation target the developer app.

## Naming and native matching

Fixed names follow `<type>_<area>_<view>[_<role>]`, or `dev_<type>_<area>_<view>[_<role>]`. Types are `screen`, `popup`, `button`, `input`, `label`, `image`, `control` and `item`.

Each C# constant declares its owning file in its XML remarks. Assign production metadata at that owner through `x:Static` or a registered keyed binding. Do not put instance IDs in shared styles.

Both hosts call `AutomationMetadata.Configure()`. Android matching reads compiled resource IDs or MAUI's accessibility-node resource ID (`<host package>:id/<ID>`); it does not use spoken `contentDescription`.

```kotlin
onView(AutomationIdMatcher.withAutomationId(
    UiIds.ButtonOnboardingPhoneSendCode,
    UiIds.ScreenOnboardingPhone
)).check(matches(isDisplayed()))
```

Include the owning root where needed. Composite inputs require the appropriate native input matcher or `UiContract.enterText`. Assert visible text separately with independent locale contracts.

## Dynamic instance keys

C# uses `AutomationIds.ForKey`; Kotlin uses `UiIds.forKey`. Both encode each nonempty key as lowercase hexadecimal UTF-8 and preserve case, punctuation and segment boundaries:

```text
<prefix>_key_<hex key 1>[_<hex key 2>...]
```

| Prefix symbol | Key source |
| --- | --- |
| `ItemSettingsLanguageOption` | `Language.Name`, the stable language code |
| `ItemSharedPhonecountryOption` | Country `Alpha2`, e.g. `US` or `SE` |
| `DevItemPlatformCatalogScenario` | Subject key and scenario key as separate segments |
| `LabelIdentityCardFieldname` | Property/schema `Key` |
| `LabelIdentityCardFieldvalue` | Property/schema `Key` |
| `ItemApplicationsCatalogTemplate` | `Source.ItemId` |
| `LabelApplicationsCatalogTemplatename` | `Source.ItemId` |
| `LabelApplicationsCatalogTemplatetype` | `Source.ItemId` |

Examples:

```kotlin
UiIds.forKey(UiIds.ItemSettingsLanguageOption, "sv")
// item_settings_language_option_key_7376

UiIds.forKey(UiIds.ItemSharedPhonecountryOption, "US")
// item_shared_phonecountry_option_key_5553

UiIds.forKey(UiIds.DevItemPlatformCatalogScenario, "onboarding.phone", "valid")
// dev_item_platform_catalog_scenario_key_6f6e626f617264696e672e70686f6e65_76616c6964
```

Use only registered prefixes and stable schema/opaque record keys. Never use translated labels, row indices, user-entered values, PINs or OTPs. `AutomationKeyExtension` leaves a not-yet-bound/empty key unidentified and never writes back to the data source. Encoding is reversible, not anonymization; avoid exposing encoded sensitive keys in logs.

## Migration status

The catalog replaces earlier ad-hoc names. For example:

| Earlier identifier | Current identifier |
| --- | --- |
| `screen_home` | `screen_main_home` |
| `screen_phone_verification` | `screen_onboarding_phone` |
| `input_phone_number` | `input_onboarding_phone_number` |
| `screen_create_pin` | `screen_onboarding_password` |
| `screen_success` | `screen_onboarding_finalize` |
| `screen_view_identity` | `screen_identity_card` |
| `screen_kyc_process` | `screen_kyc_process_form` |
| `option_language_<code>` | `ForKey(ItemSettingsLanguageOption, code)` |

`AutomationIds.Retired` reserves old names/prefixes; do not reuse them for another control. The shared verification page uses `screen_security_verifycode` for both phone and email subjects.

Several screen objects still use uncatalogued literals for KYC pages/fields, contacts, notifications, chat and image cropping. These are migration gaps, not guaranteed IDs in the current app. The complete active catalog is listed below. Do not infer support from a legacy screen object or an older test report.

The existing registration flow explicitly supports `temporary-phone`, not ordinary email onboarding. The developer catalog also has email/verification visual previews; those previews do not extend Espresso registration coverage.

## Maintenance and optional validation

1. Register or change the ID and its owning file in `AutomationIds.cs`.
2. Assign metadata at that owner; preserve production IDs in developer previews.
3. Mirror constants and encoding in `common/UiIds.kt`.
4. Update screen objects, independent locale assets and this reference.
5. Reserve removed names in `Retired` instead of recycling them.

Optional static check from the repository root with PowerShell 7:

```powershell
pwsh -File .\tests\scripts\validate-automation-contract.ps1
```

The script checks names, owners, declarations, references, dynamic prefixes, retired reuse, Kotlin parity and twelve locale assets. Existing uncatalogued literals can cause findings. It does not check this Markdown reference, build either app or verify device behavior.

## Catalog by owning file

Catalog source: [AutomationIds.cs](../../../../../../../../../../../../NeuroAccessMaui/UI/Automation/AutomationIds.cs). Developer behavior: [DevTestPlatform](../../../../../../../../../../../../DevTestPlatform/README.md).

### DevTestPlatform/ProductionAdapters/ProductionScreenAdapter.cs

[Owning source](../../../../../../../../../../../../DevTestPlatform/ProductionAdapters/ProductionScreenAdapter.cs)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `DevButtonComponentsTextbuttonSample` | `dev_button_components_textbutton_sample` |
| `DevInputComponentsCompositeentrySample` | `dev_input_components_compositeentry_sample` |

### DevTestPlatform/UI/Pages/CatalogPage.xaml

[Owning source](../../../../../../../../../../../../DevTestPlatform/UI/Pages/CatalogPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `DevScreenPlatformCatalog` | `dev_screen_platform_catalog` |
| `DevInputPlatformCatalogSearch` | `dev_input_platform_catalog_search` |
| `DevInputPlatformCatalogSection` | `dev_input_platform_catalog_section` |
| `DevInputPlatformCatalogCategory` | `dev_input_platform_catalog_category` |
| `DevInputPlatformCatalogArea` | `dev_input_platform_catalog_area` |
| `DevInputPlatformCatalogSubject` | `dev_input_platform_catalog_subject` |

### DevTestPlatform/UI/Pages/PreviewPage.xaml

[Owning source](../../../../../../../../../../../../DevTestPlatform/UI/Pages/PreviewPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `DevScreenPlatformPreview` | `dev_screen_platform_preview` |
| `DevLabelPlatformPreviewDiagnostics` | `dev_label_platform_preview_diagnostics` |
| `DevButtonPlatformPreviewFullscreen` | `dev_button_platform_preview_fullscreen` |
| `DevButtonPlatformPreviewSimulate` | `dev_button_platform_preview_simulate` |
| `DevControlPlatformPreviewContent` | `dev_control_platform_preview_content` |
| `DevButtonPlatformPreviewBack` | `dev_button_platform_preview_back` |

### DevTestPlatform/UI/ViewModels/CatalogItemViewModel.cs

[Owning source](../../../../../../../../../../../../DevTestPlatform/UI/ViewModels/CatalogItemViewModel.cs)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `DevItemPlatformCatalogScenario` | `dev_item_platform_catalog_scenario` |

### NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenApplicationsCatalog` | `screen_applications_catalog` |
| `LabelApplicationsCatalogTitle` | `label_applications_catalog_title` |
| `ControlApplicationsCatalogScroll` | `control_applications_catalog_scroll` |
| `ButtonApplicationsCatalogCurrent` | `button_applications_catalog_current` |
| `LabelApplicationsCatalogCurrentstate` | `label_applications_catalog_currentstate` |
| `ItemApplicationsCatalogTemplate` | `item_applications_catalog_template` |
| `LabelApplicationsCatalogTemplatename` | `label_applications_catalog_templatename` |
| `LabelApplicationsCatalogTemplatetype` | `label_applications_catalog_templatetype` |

### NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenIdentityCard` | `screen_identity_card` |
| `LabelIdentityCardState` | `label_identity_card_state` |
| `LabelIdentityCardName` | `label_identity_card_name` |
| `ButtonIdentityCardBack` | `button_identity_card_back` |
| `LabelIdentityCardDetails` | `label_identity_card_details` |
| `ControlIdentityCardScroll` | `control_identity_card_scroll` |
| `ImageIdentityCardQr` | `image_identity_card_qr` |
| `ButtonIdentityCardAddcontact` | `button_identity_card_addcontact` |
| `ButtonIdentityCardRemovecontact` | `button_identity_card_removecontact` |
| `ButtonIdentityCardChat` | `button_identity_card_chat` |
| `LabelIdentityCardFieldname` | `label_identity_card_fieldname` |
| `LabelIdentityCardFieldvalue` | `label_identity_card_fieldvalue` |

### NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenKycStatus` | `screen_kyc_status` |
| `LabelKycStatusHeading` | `label_kyc_status_heading` |
| `LabelKycStatusTitle` | `label_kyc_status_title` |
| `ButtonKycStatusOpen` | `button_kyc_status_open` |
| `ButtonKycStatusApplications` | `button_kyc_status_applications` |

### NeuroAccessMaui/UI/Pages/Kyc/KycProcessPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Kyc/KycProcessPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenKycProcess` | `screen_kyc_process_form` |

### NeuroAccessMaui/UI/Pages/Main/HomePage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Main/HomePage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenMainHome` | `screen_main_home` |
| `ButtonMainHomeSettings` | `button_main_home_settings` |
| `ImageMainHomeBanner` | `image_main_home_banner` |
| `ButtonMainHomeApplyidentity` | `button_main_home_applyidentity` |
| `ButtonMainHomeScanqr` | `button_main_home_scanqr` |
| `ButtonMainHomeShowidentity` | `button_main_home_showidentity` |

### NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenMainQrscan` | `screen_main_qrscan` |
| `ControlMainQrscanManual` | `control_main_qrscan_manual` |
| `ButtonMainQrscanManualentry` | `button_main_qrscan_manualentry` |
| `InputMainQrscanPayload` | `input_main_qrscan_payload` |
| `LabelMainQrscanPayload` | `label_main_qrscan_payload` |
| `ButtonMainQrscanOpen` | `button_main_qrscan_open` |

### NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenMainSettings` | `screen_main_settings` |
| `LabelMainSettingsTitle` | `label_main_settings_title` |
| `ButtonMainSettingsLanguage` | `button_main_settings_language` |
| `ButtonMainSettingsRevokeidentity` | `button_main_settings_revokeidentity` |
| `ButtonMainSettingsChangepassword` | `button_main_settings_changepassword` |

### NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenSecurityVerifycode` | `screen_security_verifycode` |
| `ButtonSecurityVerifycodeBack` | `button_security_verifycode_back` |
| `LabelSecurityVerifycodeTitle` | `label_security_verifycode_title` |
| `ControlSecurityVerifycodeDigits` | `control_security_verifycode_digits` |
| `InputSecurityVerifycodeValue` | `input_security_verifycode_value` |
| `ButtonSecurityVerifycodeVerify` | `button_security_verifycode_verify` |
| `ButtonSecurityVerifycodeResend` | `button_security_verifycode_resend` |
| `LabelSecurityVerifycodeDigits` | `label_security_verifycode_digits` |

### NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingRoot` | `screen_onboarding_root` |
| `ButtonOnboardingRootExistingaccount` | `button_onboarding_root_existingaccount` |
| `ButtonOnboardingRootBack` | `button_onboarding_root_back` |
| `ButtonOnboardingRootLanguage` | `button_onboarding_root_language` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingBiometrics` | `screen_onboarding_biometrics` |
| `LabelOnboardingBiometricsTitle` | `label_onboarding_biometrics_title` |
| `ButtonOnboardingBiometricsLater` | `button_onboarding_biometrics_later` |
| `ButtonOnboardingBiometricsEnable` | `button_onboarding_biometrics_enable` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/CreateAccountStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/CreateAccountStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingAccount` | `screen_onboarding_account` |
| `LabelOnboardingAccountTitle` | `label_onboarding_account_title` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingPassword` | `screen_onboarding_password` |
| `LabelOnboardingPasswordTitle` | `label_onboarding_password_title` |
| `InputOnboardingPasswordValue` | `input_onboarding_password_value` |
| `InputOnboardingPasswordConfirmation` | `input_onboarding_password_confirmation` |
| `ButtonOnboardingPasswordKeyboard` | `button_onboarding_password_keyboard` |
| `ButtonOnboardingPasswordCreate` | `button_onboarding_password_create` |
| `LabelOnboardingPasswordValue` | `label_onboarding_password_value` |
| `LabelOnboardingPasswordConfirmation` | `label_onboarding_password_confirmation` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/FinalizeStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/FinalizeStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingFinalize` | `screen_onboarding_finalize` |
| `LabelOnboardingFinalizeTitle` | `label_onboarding_finalize_title` |
| `ButtonOnboardingFinalizeContinue` | `button_onboarding_finalize_continue` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingUsername` | `screen_onboarding_username` |
| `LabelOnboardingUsernameTitle` | `label_onboarding_username_title` |
| `InputOnboardingUsernameValue` | `input_onboarding_username_value` |
| `ButtonOnboardingUsernameContinue` | `button_onboarding_username_continue` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingEmail` | `screen_onboarding_email` |
| `LabelOnboardingEmailTitle` | `label_onboarding_email_title` |
| `InputOnboardingEmailAddress` | `input_onboarding_email_address` |
| `ButtonOnboardingEmailSendCode` | `button_onboarding_email_send_code` |
| `ButtonOnboardingEmailResendCode` | `button_onboarding_email_resend_code` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingPhone` | `screen_onboarding_phone` |
| `LabelOnboardingPhoneTitle` | `label_onboarding_phone_title` |
| `ButtonOnboardingPhoneCountry` | `button_onboarding_phone_country` |
| `InputOnboardingPhoneNumber` | `input_onboarding_phone_number` |
| `ButtonOnboardingPhoneSendCode` | `button_onboarding_phone_send_code` |
| `LabelOnboardingPhoneNumber` | `label_onboarding_phone_number` |

### NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenOnboardingProvider` | `screen_onboarding_provider` |
| `LabelOnboardingProviderTitle` | `label_onboarding_provider_title` |
| `InputOnboardingProviderInvitecode` | `input_onboarding_provider_invitecode` |
| `ButtonOnboardingProviderScanqr` | `button_onboarding_provider_scanqr` |
| `ButtonOnboardingProviderSelect` | `button_onboarding_provider_select` |

### NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ScreenPetitionsSignature` | `screen_petitions_signature` |
| `LabelPetitionsSignatureTitle` | `label_petitions_signature_title` |
| `LabelPetitionsSignaturePurpose` | `label_petitions_signature_purpose` |
| `ButtonPetitionsSignatureAccept` | `button_petitions_signature_accept` |

### NeuroAccessMaui/UI/Pages/Utility/Images/ImageCroppingPage.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Pages/Utility/Images/ImageCroppingPage.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `ButtonMediaImagecropRotate` | `button_media_imagecrop_rotate` |

### NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `PopupSecurityPassword` | `popup_security_password` |
| `LabelSecurityPasswordTitle` | `label_security_password_title` |
| `InputSecurityPasswordValue` | `input_security_password_value` |
| `ButtonSecurityPasswordEnter` | `button_security_password_enter` |
| `ButtonSecurityPasswordCancel` | `button_security_password_cancel` |

### NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `PopupSystemPermission` | `popup_system_permission` |
| `ButtonSystemPermissionSkip` | `button_system_permission_skip` |
| `LabelSystemPermissionTitle` | `label_system_permission_title` |
| `LabelSystemPermissionDescription` | `label_system_permission_description` |

### NeuroAccessMaui/UI/Popups/SelectPhoneCodePopup.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Popups/SelectPhoneCodePopup.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `PopupSharedPhonecountry` | `popup_shared_phonecountry` |
| `InputSharedPhonecountrySearch` | `input_shared_phonecountry_search` |
| `ItemSharedPhonecountryOption` | `item_shared_phonecountry_option` |

### NeuroAccessMaui/UI/Popups/Settings/SelectLanguagePopup.xaml

[Owning source](../../../../../../../../../../../../NeuroAccessMaui/UI/Popups/Settings/SelectLanguagePopup.xaml)

| Symbol | ID or dynamic prefix |
| --- | --- |
| `PopupSettingsLanguage` | `popup_settings_language` |
| `ItemSettingsLanguageOption` | `item_settings_language_option` |
