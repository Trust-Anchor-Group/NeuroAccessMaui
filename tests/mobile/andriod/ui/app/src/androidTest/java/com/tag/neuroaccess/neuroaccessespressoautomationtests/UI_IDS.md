# NeuroAccess Espresso UI IDs

Espresso locates MAUI `AutomationId` values through Android `contentDescription`.

## ID Provider

| Element | UI ID |
| --- | --- |
| Screen | `screen_onboarding_id_provider` |
| Select for me | `button_id_provider_select_for_me` |

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