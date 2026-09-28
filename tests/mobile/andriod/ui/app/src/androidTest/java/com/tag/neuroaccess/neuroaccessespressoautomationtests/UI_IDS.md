# NeuroAccess Espresso UI IDs

MAUI 10.0.10 exposes `AutomationId` through the accessibility node's `viewIdResourceName`. Tests use the shared `AutomationIdMatcher` and `ScreenWaiter` helpers, which support this ID and compiled Android resource IDs. They do not match spoken `contentDescription` values. Keep localized accessibility descriptions separate from technical test IDs.

## ID Provider

| Element | UI ID |
| --- | --- |
| Screen | `screen_onboarding_id_provider` |
| Select for me | `button_id_provider_select_for_me` |
| Change language | `button_onboarding_language` |
| Language selector | `popup_select_language` |

Language options follow the `option_language_<ISO-639-1>` convention, for example `option_language_en` and `option_language_sv`.

## Settings

| Element | UI ID |
| --- | --- |
| Screen | `screen_settings` |
| Open settings from Home | `button_home_settings` |
| Change PIN | `button_settings_change_pin` |

## PIN Authentication

| Element | UI ID |
| --- | --- |
| Popup | `popup_pin_authentication` |
| PIN input | `input_authentication_pin` |
| Confirm | `button_authentication_pin_confirm` |

## Phone Verification

| Element | UI ID |
| --- | --- |
| Screen | `screen_phone_verification` |
| Select country | `button_select_phone_country` |
| Country selector popup | `popup_select_phone_country` |
| Search countries | `input_search_phone_country` |
| Country list | `list_phone_countries` |
| United States option | `option_phone_country_US` |
| Phone number input | `input_phone_number` |
| Send code | `button_send_phone_code` |
| Back to ID Provider | `button_back_onboarding` |

Country options follow this convention:

```text
option_phone_country_<ISO-3166-1-alpha-2>
```

Examples: `option_phone_country_US`, `option_phone_country_SE`.

## Phone Code Verification

| Element | UI ID |
| --- | --- |
| Screen | `screen_phone_code_verification` |
| Code input | `input_phone_verification_code` |
| Verify code | `button_verify_phone_code` |
| Resend code | `button_resend_phone_code` |
| Back to phone verification | `button_back_phone_verification` |

## Username

| Element | UI ID |
| --- | --- |
| Screen | `screen_username` |
| Username input | `input_username` |
| Continue | `button_continue_username` |

## PIN Creation

| Element | UI ID |
| --- | --- |
| Screen | `screen_create_pin` |
| New PIN input | `input_new_pin` |
| Confirm PIN input | `input_confirm_pin` |
| Create PIN | `button_create_pin` |

## Biometrics

| Element | UI ID |
| --- | --- |
| Screen | `screen_biometrics` |
| Later | `button_biometrics_later` |

This screen is only shown when the device supports biometric authentication.

## Registration Success

| Element | UI ID |
| --- | --- |
| Screen | `screen_success` |
| Continue to Home | `button_continue_success` |

## Notification Permission Popup

| Element | UI ID |
| --- | --- |
| App popup | `popup_notification_permission` |
| Skip | `button_notification_permission_skip` |

This covers the app's own permission popup. Android's system permission dialog is outside the MAUI app and has no MAUI `AutomationId`.

## Home

| Element | UI ID |
| --- | --- |
| Screen | `screen_home` |
| Show ID | `button_home_show_id` |
| Apply for personal ID | `button_apply_for_personal_id` |
| Open notifications | `button_home_notifications` |
| Open settings | `button_home_settings` |

## View Identity

| Element | UI ID |
| --- | --- |
| Screen | `screen_view_identity` |

## Identity Applications and KYC

| Element | UI ID |
| --- | --- |
| Apps screen | `screen_apps` |
| Identity applications screen | `screen_identity_applications` |
| Current application | `button_current_identity_application` |
| Available application | `button_available_identity_application` |
| KYC process | `screen_kyc_process` |
| Prepare documents page | `screen_kyc_page_prepareDocuments` |
| Personal information page | `screen_kyc_page_personalInfo` |
| Identity documents page | `screen_kyc_page_identityDocuments` |
| Address page | `screen_kyc_page_addressInformation` |
| Selfie page | `screen_kyc_page_selfiePage` |
| Organization question page | `screen_kyc_page_isOrgPage` |
| Organization information page | `screen_kyc_page_orgInfo` |
| Summary | `screen_kyc_summary` |
| Next | `button_kyc_next` |
| Back | `button_kyc_back` |
| Upload profile photo | `button_kyc_upload_photo_profilephoto` |
| Image crop screen | `screen_image_cropping` |
| Accept image crop | `button_accept_image_crop` |
| Application sent panel | `panel_kyc_application_sent` |

KYC input fields use these IDs:

```text
field_kyc_first
field_kyc_middle
field_kyc_last
field_kyc_pnr
field_kyc_bdate
field_kyc_nationality
field_kyc_gender
field_kyc_documenttype
field_kyc_zip
field_kyc_addr
field_kyc_addr2
field_kyc_area
field_kyc_city
field_kyc_region
```

## Contacts, Petitions, and Chat

| Element | UI ID |
| --- | --- |
| Contacts | `screen_contacts` |
| Open contacts from Apps | `button_apps_contacts` |
| Notifications | `screen_notifications` |
| Identity petition | `screen_petition_identity` |
| Chat | `screen_chat` |
| Message input | `input_chat_message` |
| Send message | `button_chat_send` |

Contact rows use identity-specific IDs:

```text
button_contact_details_<Neuro-ID>
button_contact_chat_<Neuro-ID>
```

## Identity Settings

| Element | UI ID |
| --- | --- |
| Revoke identity | `button_settings_revoke_identity` |

## Change PIN flow mapping

The Change PIN flow uses these IDs in order:

```text
screen_home
  → button_home_settings
  → screen_settings
  → button_settings_change_pin
  → popup_pin_authentication
  → input_authentication_pin
  → button_authentication_pin_confirm
  → screen_create_pin
  → input_new_pin
  → input_confirm_pin
  → button_create_pin
  → screen_success
  → button_continue_success
  → button_home_show_id
  → screen_view_identity
```

After the PIN is changed, the current authenticated session permits the first identity opening without another PIN prompt. The full-suite runner then force-stops the application, verifies that its process has ended, returns to the Android home screen, and relaunches it. The subsequent identity opening uses `popup_pin_authentication` and verifies `NEUROACCESS_TEST_NEW_PIN`.
