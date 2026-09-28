# Phone Verification

## Specification ID

ONB-PHONE-001

## Purpose

Collect the country code and phone number required to continue to Phone Code Verification.

## Test Coverage

### Country code

| Test ID | Test name |
|---|---|
| TC-ONB-PHONE-COUNTRY-001 | Open country-code selection |
| TC-ONB-PHONE-COUNTRY-002 | Select a country code |
| TC-ONB-PHONE-COUNTRY-003 | Display the selected country code on Phone Verification |

### Phone number

| Test ID | Test name |
|---|---|
| TC-ONB-PHONE-NUMBER-001 | Accept a valid phone number |
| TC-ONB-PHONE-NUMBER-002 | Reject an empty phone number |
| TC-ONB-PHONE-NUMBER-003 | Reject an invalid phone number |

### Continue action

| Test ID | Test name |
|---|---|
| TC-ONB-PHONE-CONTINUE-001 | Prevent continuation when the phone details are invalid |
| TC-ONB-PHONE-CONTINUE-002 | Continue when the country code and phone number are valid |

### Combined flow

| Test ID | Test name |
|---|---|
| TC-ONB-PHONE-FLOW-001 | Complete the Phone Verification entry flow |

The combined flow covers selecting a country code, entering a phone number, continuing, and reaching Phone Code Verification. Component tests should remain independently executable and define their own preconditions in the test management system.

## Known Issues

Active failures and linked defects are maintained in the test management system. A test ID remains unchanged when its execution status changes.
