# Phone Code Verification

## Specification ID

ONB-PHONE-CODE-001

## Purpose

Verify the code associated with the phone number submitted during Phone Verification.

## Reached From

Phone Verification (ONB-PHONE-001).

## Test Coverage

| Test ID | Test name |
|---|---|
| TC-ONB-PHONE-CODE-001 | Accept a valid phone verification code |
| TC-ONB-PHONE-CODE-002 | Reject an incomplete phone verification code |
| TC-ONB-PHONE-CODE-003 | Reject an invalid phone verification code |
| TC-ONB-PHONE-CODE-NAV-001 | Reach Phone Code Verification from Phone Verification |

Resend, expiry, attempt-limit, and back-navigation test IDs should be added when those behaviours have been confirmed.

## Known Issues

Active failures and linked defects are maintained in the test management system.
