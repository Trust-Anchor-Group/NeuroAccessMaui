using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroAccessMaui.UI.Automation;

/// <summary>Defines stable technical identities owned by production views and developer chrome.</summary>
/// <remarks>Identifiers describe usage, never localized labels or input values.</remarks>
public static class AutomationIds
{
    /// <summary>Identifies dev button components textbutton sample.</summary>
    /// <remarks>Owner: DevTestPlatform/ProductionAdapters/ProductionScreenAdapter.cs.</remarks>
    public const string DevButtonComponentsTextbuttonSample = "dev_button_components_textbutton_sample";

    /// <summary>Identifies dev input components compositeentry sample.</summary>
    /// <remarks>Owner: DevTestPlatform/ProductionAdapters/ProductionScreenAdapter.cs.</remarks>
    public const string DevInputComponentsCompositeentrySample = "dev_input_components_compositeentry_sample";

    /// <summary>Identifies dev screen platform preview.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevScreenPlatformPreview = "dev_screen_platform_preview";

    /// <summary>Identifies dev label platform preview diagnostics.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevLabelPlatformPreviewDiagnostics = "dev_label_platform_preview_diagnostics";

    /// <summary>Identifies dev button platform preview fullscreen.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevButtonPlatformPreviewFullscreen = "dev_button_platform_preview_fullscreen";

    /// <summary>Identifies dev button platform preview simulate.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevButtonPlatformPreviewSimulate = "dev_button_platform_preview_simulate";

    /// <summary>Identifies dev control platform preview content.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevControlPlatformPreviewContent = "dev_control_platform_preview_content";

    /// <summary>Identifies dev button platform preview back.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/PreviewPage.xaml.</remarks>
    public const string DevButtonPlatformPreviewBack = "dev_button_platform_preview_back";

    /// <summary>Identifies dev screen platform catalog.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevScreenPlatformCatalog = "dev_screen_platform_catalog";

    /// <summary>Identifies dev input platform catalog search.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevInputPlatformCatalogSearch = "dev_input_platform_catalog_search";

    /// <summary>Identifies dev input platform catalog section.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevInputPlatformCatalogSection = "dev_input_platform_catalog_section";

    /// <summary>Identifies dev input platform catalog category.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevInputPlatformCatalogCategory = "dev_input_platform_catalog_category";

    /// <summary>Identifies dev input platform catalog area.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevInputPlatformCatalogArea = "dev_input_platform_catalog_area";

    /// <summary>Identifies dev input platform catalog subject.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/Pages/CatalogPage.xaml.</remarks>
    public const string DevInputPlatformCatalogSubject = "dev_input_platform_catalog_subject";

    /// <summary>Identifies popup system permission.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml.</remarks>
    public const string PopupSystemPermission = "popup_system_permission";

    /// <summary>Identifies button system permission skip.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml.</remarks>
    public const string ButtonSystemPermissionSkip = "button_system_permission_skip";

    /// <summary>Identifies popup shared phonecountry.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/SelectPhoneCodePopup.xaml.</remarks>
    public const string PopupSharedPhonecountry = "popup_shared_phonecountry";

    /// <summary>Identifies input shared phonecountry search.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/SelectPhoneCodePopup.xaml.</remarks>
    public const string InputSharedPhonecountrySearch = "input_shared_phonecountry_search";

    /// <summary>Identifies popup settings language.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Settings/SelectLanguagePopup.xaml.</remarks>
    public const string PopupSettingsLanguage = "popup_settings_language";

    /// <summary>Identifies button media imagecrop rotate.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Utility/Images/ImageCroppingPage.xaml.</remarks>
    public const string ButtonMediaImagecropRotate = "button_media_imagecrop_rotate";

    /// <summary>Identifies screen onboarding root.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml.</remarks>
    public const string ScreenOnboardingRoot = "screen_onboarding_root";

    /// <summary>Identifies button onboarding root existingaccount.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml.</remarks>
    public const string ButtonOnboardingRootExistingaccount = "button_onboarding_root_existingaccount";

    /// <summary>Identifies button onboarding root back.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml.</remarks>
    public const string ButtonOnboardingRootBack = "button_onboarding_root_back";

    /// <summary>Identifies button onboarding root language.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/OnboardingPage.xaml.</remarks>
    public const string ButtonOnboardingRootLanguage = "button_onboarding_root_language";

    /// <summary>Identifies screen onboarding provider.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml.</remarks>
    public const string ScreenOnboardingProvider = "screen_onboarding_provider";

    /// <summary>Identifies label onboarding provider title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml.</remarks>
    public const string LabelOnboardingProviderTitle = "label_onboarding_provider_title";

    /// <summary>Identifies input onboarding provider invitecode.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml.</remarks>
    public const string InputOnboardingProviderInvitecode = "input_onboarding_provider_invitecode";

    /// <summary>Identifies button onboarding provider scanqr.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml.</remarks>
    public const string ButtonOnboardingProviderScanqr = "button_onboarding_provider_scanqr";

    /// <summary>Identifies button onboarding provider select.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/WelcomeStepView.xaml.</remarks>
    public const string ButtonOnboardingProviderSelect = "button_onboarding_provider_select";

    /// <summary>Identifies screen onboarding finalize.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/FinalizeStepView.xaml.</remarks>
    public const string ScreenOnboardingFinalize = "screen_onboarding_finalize";

    /// <summary>Identifies label onboarding finalize title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/FinalizeStepView.xaml.</remarks>
    public const string LabelOnboardingFinalizeTitle = "label_onboarding_finalize_title";

    /// <summary>Identifies button onboarding finalize continue.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/FinalizeStepView.xaml.</remarks>
    public const string ButtonOnboardingFinalizeContinue = "button_onboarding_finalize_continue";

    /// <summary>Identifies screen main home.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ScreenMainHome = "screen_main_home";

    /// <summary>Identifies button main home settings.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ButtonMainHomeSettings = "button_main_home_settings";

    /// <summary>Identifies image main home banner.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ImageMainHomeBanner = "image_main_home_banner";

    /// <summary>Identifies button main home applyidentity.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ButtonMainHomeApplyidentity = "button_main_home_applyidentity";

    /// <summary>Identifies button main home scanqr.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ButtonMainHomeScanqr = "button_main_home_scanqr";

    /// <summary>Identifies button main home showidentity.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/HomePage.xaml.</remarks>
    public const string ButtonMainHomeShowidentity = "button_main_home_showidentity";

    /// <summary>Identifies screen onboarding account.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/CreateAccountStepView.xaml.</remarks>
    public const string ScreenOnboardingAccount = "screen_onboarding_account";

    /// <summary>Identifies label onboarding account title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/CreateAccountStepView.xaml.</remarks>
    public const string LabelOnboardingAccountTitle = "label_onboarding_account_title";

    /// <summary>Identifies screen onboarding username.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml.</remarks>
    public const string ScreenOnboardingUsername = "screen_onboarding_username";

    /// <summary>Identifies label onboarding username title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml.</remarks>
    public const string LabelOnboardingUsernameTitle = "label_onboarding_username_title";

    /// <summary>Identifies input onboarding username value.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml.</remarks>
    public const string InputOnboardingUsernameValue = "input_onboarding_username_value";

    /// <summary>Identifies button onboarding username continue.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/NameEntryStepView.xaml.</remarks>
    public const string ButtonOnboardingUsernameContinue = "button_onboarding_username_continue";

    /// <summary>Identifies screen onboarding phone.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string ScreenOnboardingPhone = "screen_onboarding_phone";

    /// <summary>Identifies label onboarding phone title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string LabelOnboardingPhoneTitle = "label_onboarding_phone_title";

    /// <summary>Identifies button onboarding phone country.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string ButtonOnboardingPhoneCountry = "button_onboarding_phone_country";

    /// <summary>Identifies input onboarding phone number.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string InputOnboardingPhoneNumber = "input_onboarding_phone_number";

    /// <summary>Identifies button onboarding phone send code.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string ButtonOnboardingPhoneSendCode = "button_onboarding_phone_send_code";

    /// <summary>Identifies screen onboarding password.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string ScreenOnboardingPassword = "screen_onboarding_password";

    /// <summary>Identifies label onboarding password title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string LabelOnboardingPasswordTitle = "label_onboarding_password_title";

    /// <summary>Identifies input onboarding password value.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string InputOnboardingPasswordValue = "input_onboarding_password_value";

    /// <summary>Identifies input onboarding password confirmation.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string InputOnboardingPasswordConfirmation = "input_onboarding_password_confirmation";

    /// <summary>Identifies button onboarding password keyboard.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string ButtonOnboardingPasswordKeyboard = "button_onboarding_password_keyboard";

    /// <summary>Identifies button onboarding password create.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string ButtonOnboardingPasswordCreate = "button_onboarding_password_create";

    /// <summary>Identifies screen onboarding email.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml.</remarks>
    public const string ScreenOnboardingEmail = "screen_onboarding_email";

    /// <summary>Identifies label onboarding email title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml.</remarks>
    public const string LabelOnboardingEmailTitle = "label_onboarding_email_title";

    /// <summary>Identifies input onboarding email address.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml.</remarks>
    public const string InputOnboardingEmailAddress = "input_onboarding_email_address";

    /// <summary>Identifies button onboarding email send code.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml.</remarks>
    public const string ButtonOnboardingEmailSendCode = "button_onboarding_email_send_code";

    /// <summary>Identifies button onboarding email resend code.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidateEmailStepView.xaml.</remarks>
    public const string ButtonOnboardingEmailResendCode = "button_onboarding_email_resend_code";

    /// <summary>Identifies screen onboarding biometrics.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml.</remarks>
    public const string ScreenOnboardingBiometrics = "screen_onboarding_biometrics";

    /// <summary>Identifies label onboarding biometrics title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml.</remarks>
    public const string LabelOnboardingBiometricsTitle = "label_onboarding_biometrics_title";

    /// <summary>Identifies button onboarding biometrics later.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml.</remarks>
    public const string ButtonOnboardingBiometricsLater = "button_onboarding_biometrics_later";

    /// <summary>Identifies button onboarding biometrics enable.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/BiometricsStepView.xaml.</remarks>
    public const string ButtonOnboardingBiometricsEnable = "button_onboarding_biometrics_enable";

    /// <summary>Identifies screen security verifycode.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string ScreenSecurityVerifycode = "screen_security_verifycode";

    /// <summary>Identifies button security verifycode back.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string ButtonSecurityVerifycodeBack = "button_security_verifycode_back";

    /// <summary>Identifies label security verifycode title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string LabelSecurityVerifycodeTitle = "label_security_verifycode_title";

    /// <summary>Identifies control security verifycode digits.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string ControlSecurityVerifycodeDigits = "control_security_verifycode_digits";

    /// <summary>Identifies input security verifycode value.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string InputSecurityVerifycodeValue = "input_security_verifycode_value";

    /// <summary>Identifies button security verifycode verify.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string ButtonSecurityVerifycodeVerify = "button_security_verifycode_verify";

    /// <summary>Identifies button security verifycode resend.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string ButtonSecurityVerifycodeResend = "button_security_verifycode_resend";

    /// <summary>Identifies item settings language option.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Settings/SelectLanguagePopup.xaml.</remarks>
    public const string ItemSettingsLanguageOption = "item_settings_language_option";

    /// <summary>Identifies item shared phonecountry option.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/SelectPhoneCodePopup.xaml.</remarks>
    public const string ItemSharedPhonecountryOption = "item_shared_phonecountry_option";

    /// <summary>Identifies dev item platform catalog scenario.</summary>
    /// <remarks>Owner: DevTestPlatform/UI/ViewModels/CatalogItemViewModel.cs.</remarks>
    public const string DevItemPlatformCatalogScenario = "dev_item_platform_catalog_scenario";

    /// <summary>Identifies screen main settings.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml.</remarks>
    public const string ScreenMainSettings = "screen_main_settings";

    /// <summary>Identifies label main settings title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml.</remarks>
    public const string LabelMainSettingsTitle = "label_main_settings_title";

    /// <summary>Identifies button main settings language.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml.</remarks>
    public const string ButtonMainSettingsLanguage = "button_main_settings_language";

    /// <summary>Identifies button main settings revokeidentity.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml.</remarks>
    public const string ButtonMainSettingsRevokeidentity = "button_main_settings_revokeidentity";

    /// <summary>Identifies button main settings changepassword.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/Settings/SettingsPage.xaml.</remarks>
    public const string ButtonMainSettingsChangepassword = "button_main_settings_changepassword";

    /// <summary>Identifies popup security password.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml.</remarks>
    public const string PopupSecurityPassword = "popup_security_password";

    /// <summary>Identifies label security password title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml.</remarks>
    public const string LabelSecurityPasswordTitle = "label_security_password_title";

    /// <summary>Identifies input security password value.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml.</remarks>
    public const string InputSecurityPasswordValue = "input_security_password_value";

    /// <summary>Identifies button security password enter.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml.</remarks>
    public const string ButtonSecurityPasswordEnter = "button_security_password_enter";

    /// <summary>Identifies button security password cancel.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Password/CheckPasswordPopup.xaml.</remarks>
    public const string ButtonSecurityPasswordCancel = "button_security_password_cancel";

    /// <summary>Identifies label onboarding password value.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string LabelOnboardingPasswordValue = "label_onboarding_password_value";

    /// <summary>Identifies label onboarding password confirmation.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/DefinePasswordStepView.xaml.</remarks>
    public const string LabelOnboardingPasswordConfirmation = "label_onboarding_password_confirmation";

    /// <summary>Identifies label onboarding phone number.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Onboarding/Views/ValidatePhoneStepView.xaml.</remarks>
    public const string LabelOnboardingPhoneNumber = "label_onboarding_phone_number";

    /// <summary>Identifies label security verifycode digits.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/VerifyCode/VerifyCodePage.xaml.</remarks>
    public const string LabelSecurityVerifycodeDigits = "label_security_verifycode_digits";

    /// <summary>Identifies label system permission title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml.</remarks>
    public const string LabelSystemPermissionTitle = "label_system_permission_title";

    /// <summary>Identifies label system permission description.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Popups/Permission/ShowPermissionPopup.xaml.</remarks>
    public const string LabelSystemPermissionDescription = "label_system_permission_description";

    /// <summary>Identifies the shared production identity card.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ScreenIdentityCard = "screen_identity_card";

    /// <summary>Identifies the displayed identity lifecycle state.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string LabelIdentityCardState = "label_identity_card_state";

    /// <summary>Identifies the displayed identity friendly name.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string LabelIdentityCardName = "label_identity_card_name";

    /// <summary>Identifies closing the identity card through its real navigation command.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ButtonIdentityCardBack = "button_identity_card_back";

    /// <summary>Identifies the localized header for expanding identity details.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string LabelIdentityCardDetails = "label_identity_card_details";

    /// <summary>Identifies the identity details scrolling region.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ControlIdentityCardScroll = "control_identity_card_scroll";

    /// <summary>Identifies the real identity QR image and its copy interaction.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ImageIdentityCardQr = "image_identity_card_qr";

    /// <summary>Identifies adding the displayed third-party identity as a contact.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ButtonIdentityCardAddcontact = "button_identity_card_addcontact";

    /// <summary>Identifies removing the displayed identity from contacts.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ButtonIdentityCardRemovecontact = "button_identity_card_removecontact";

    /// <summary>Identifies opening chat with the displayed identity.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string ButtonIdentityCardChat = "button_identity_card_chat";

    /// <summary>Namespaces clickable identity field labels by their stable schema key.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string LabelIdentityCardFieldname = "label_identity_card_fieldname";

    /// <summary>Namespaces clickable identity field values by their stable schema key, never their contents.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Identity/ViewIdentity/ViewIdentityPage.xaml.</remarks>
    public const string LabelIdentityCardFieldvalue = "label_identity_card_fieldvalue";

    /// <summary>Identifies the shared production QR scanner.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string ScreenMainQrscan = "screen_main_qrscan";

    /// <summary>Identifies the manual QR entry state of the scanner.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string ControlMainQrscanManual = "control_main_qrscan_manual";

    /// <summary>Identifies the automatic scanner icon that opens manual entry.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string ButtonMainQrscanManualentry = "button_main_qrscan_manualentry";

    /// <summary>Identifies the real manual QR payload input.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string InputMainQrscanPayload = "input_main_qrscan_payload";

    /// <summary>Identifies the separately rendered manual QR input label.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string LabelMainQrscanPayload = "label_main_qrscan_payload";

    /// <summary>Identifies submitting a manually entered QR payload.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml.</remarks>
    public const string ButtonMainQrscanOpen = "button_main_qrscan_open";

    /// <summary>Identifies the production signature petition screen.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml.</remarks>
    public const string ScreenPetitionsSignature = "screen_petitions_signature";

    /// <summary>Identifies the localized signature petition title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml.</remarks>
    public const string LabelPetitionsSignatureTitle = "label_petitions_signature_title";

    /// <summary>Identifies the purpose provided by the real signature request.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml.</remarks>
    public const string LabelPetitionsSignaturePurpose = "label_petitions_signature_purpose";

    /// <summary>Identifies accepting the displayed signature request through production authentication.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Petitions/PetitionSignature/PetitionSignaturePage.xaml.</remarks>
    public const string ButtonPetitionsSignatureAccept = "button_petitions_signature_accept";

    private static readonly UTF8Encoding keyEncoding = new UTF8Encoding(false, true);
    /// <summary>Identifies the production applications catalogue.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string ScreenApplicationsCatalog = "screen_applications_catalog";

    /// <summary>Identifies the localized applications heading.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string LabelApplicationsCatalogTitle = "label_applications_catalog_title";

    /// <summary>Identifies the applications list and its current-application header.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string ControlApplicationsCatalogScroll = "control_applications_catalog_scroll";

    /// <summary>Identifies the single current application action.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string ButtonApplicationsCatalogCurrent = "button_applications_catalog_current";

    /// <summary>Identifies the current application's localized status.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string LabelApplicationsCatalogCurrentstate = "label_applications_catalog_currentstate";

    /// <summary>Identifies an available server template by its stable item key within the active provider's NeuroAccessKyc node.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string ItemApplicationsCatalogTemplate = "item_applications_catalog_template";

    /// <summary>Identifies a server template's visible name independently of its stable item key.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string LabelApplicationsCatalogTemplatename = "label_applications_catalog_templatename";

    /// <summary>Identifies a server template's localized application-type label.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Applications/Applications/ApplicationsPage.xaml.</remarks>
    public const string LabelApplicationsCatalogTemplatetype = "label_applications_catalog_templatetype";

    /// <summary>Identifies the production KYC form and summary host.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycProcessPage.xaml.</remarks>
    public const string ScreenKycProcess = "screen_kyc_process_form";

    /// <summary>Identifies the production identity-application status page.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml.</remarks>
    public const string ScreenKycStatus = "screen_kyc_status";

    /// <summary>Identifies the localized status page heading.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml.</remarks>
    public const string LabelKycStatusHeading = "label_kyc_status_heading";

    /// <summary>Identifies the application status title.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml.</remarks>
    public const string LabelKycStatusTitle = "label_kyc_status_title";

    /// <summary>Identifies the status-specific primary action.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml.</remarks>
    public const string ButtonKycStatusOpen = "button_kyc_status_open";

    /// <summary>Identifies the return-to-applications action.</summary>
    /// <remarks>Owner: NeuroAccessMaui/UI/Pages/Kyc/KycApplicationStatusPage.xaml.</remarks>
    public const string ButtonKycStatusApplications = "button_kyc_status_applications";

    private static readonly HashSet<string> dynamicPrefixes = new HashSet<string>(StringComparer.Ordinal)
    {
        ItemSettingsLanguageOption,
        ItemSharedPhonecountryOption,
        DevItemPlatformCatalogScenario,
        LabelIdentityCardFieldname,
        LabelIdentityCardFieldvalue,
        ItemApplicationsCatalogTemplate,
        LabelApplicationsCatalogTemplatename,
        LabelApplicationsCatalogTemplatetype,
    };

    /// <summary>Gets retired identifiers that must never be assigned to another logical control.</summary>
    public static IReadOnlyCollection<string> Retired { get; } = Array.AsReadOnly(new string[]
    {
        "dev_component_text_button",
        "dev_component_composite_entry",
        "dev_preview",
        "dev_preview_diagnostics",
        "dev_preview_fullscreen",
        "dev_preview_simulate",
        "dev_preview_content",
        "dev_preview_back",
        "dev_catalog",
        "dev_catalog_search",
        "dev_catalog_section",
        "dev_catalog_category",
        "dev_catalog_area",
        "dev_catalog_subject",
        "popup_permission",
        "button_permission_skip",
        "popup_select_phone_country",
        "input_search_phone_country",
        "popup_select_language",
        "RotateImageButton",
        "screen_onboarding",
        "button_onboarding_existing_account",
        "button_back_onboarding",
        "button_onboarding_language",
        "screen_onboarding_id_provider",
        "label_onboarding_welcome_title",
        "input_invite_code",
        "button_onboarding_scan_qr",
        "button_id_provider_select_for_me",
        "screen_success",
        "label_onboarding_success_title",
        "button_continue_success",
        "screen_home",
        "button_home_settings",
        "image_home_banner",
        "button_apply_for_personal_id",
        "button_home_scan_qr",
        "button_home_show_id",
        "screen_create_account",
        "label_create_account_title",
        "screen_username",
        "input_username",
        "button_username_continue",
        "screen_phone_verification",
        "label_phone_verification_title",
        "button_select_phone_country",
        "input_phone_number",
        "button_send_phone_code",
        "screen_create_pin",
        "Password",
        "ConfirmPassword",
        "button_toggle_password_keyboard",
        "button_create_pin",
        "screen_email_verification",
        "label_email_verification_title",
        "input_email_address",
        "button_send_email_code",
        "button_resend_email_code",
        "screen_biometrics",
        "label_biometrics_title",
        "button_biometrics_later",
        "button_biometrics_enable",
        "screen_code_verification",
        "button_back_verification",
        "label_verification_code_title",
        "control_verification_code",
        "input_verification_code",
        "button_verify_code",
        "button_resend_verification_code",
        "option_language_",
        "screen_settings",
        "button_settings_change_pin",
        "button_settings_revoke_identity",
        "popup_pin_authentication",
        "input_authentication_pin",
        "button_authentication_pin_confirm",
        "option_phone_country_",
        "dev_catalog_",
        "screen_view_identity",
        "value_view_identity_name",
        "displayed_identity_",
        "screen_identity_applications",
        "button_current_identity_application",
        "button_available_identity_application",
        "identity_application_",
        "screen_kyc_process",
    });

    /// <summary>Creates a stable instance identity without collapsing punctuation or segment boundaries.</summary>
    /// <param name="Prefix">The catalogued dynamic namespace.</param>
    /// <param name="Keys">Stable schema or opaque record keys, never labels, indices or entered values.</param>
    /// <returns>The contextual identifier with reversible hexadecimal UTF-8 key segments.</returns>
    public static string ForKey(string Prefix, params string[] Keys)
    {
        ArgumentNullException.ThrowIfNull(Keys);
        if (!dynamicPrefixes.Contains(Prefix))
            throw new ArgumentException("The automation prefix is not registered.", nameof(Prefix));
        if (Keys.Length == 0 || Keys.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Stable nonempty automation keys are required.", nameof(Keys));

        return Prefix + "_key_" + string.Join("_", Keys.Select(Key =>
            Convert.ToHexString(keyEncoding.GetBytes(Key)).ToLowerInvariant()));
    }
}
