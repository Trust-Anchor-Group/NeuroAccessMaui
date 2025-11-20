# Dynamic KYC Processes

NeuroAccess ships with a dynamic Know Your Customer (KYC) system that lets each broker publish its own onboarding forms. This document explains how the pipeline works, how to author templates, and how brokers should publish them so every client automatically loads the appropriate flow. Use it alongside the implementation in `../NeuroAccessMaui/Services/Kyc/KycService.cs`, `../NeuroAccessMaui/Services/Kyc/Models`, and the schema files in `../NeuroAccessMaui/Resources/Raw/Schemas`.

## Table of Contents

- [Runtime Overview](#runtime-overview)
- [Architecture](#architecture)
- [KYC Process Schema](#kyc-process-schema)
- [Authoring Templates](#authoring-templates)
- [Publishing Workflow](#publishing-workflow)
- [Runtime Behavior & Caching](#runtime-behavior--caching)
- [Validation, Mapping & Submission](#validation-mapping--submission)
- [Troubleshooting](#troubleshooting)

## Runtime Overview

1. **Discovery** – When a user signs in, `KycService.LoadKycApplicationsPageAsync` requests templates from the broker’s PubSub node (`NeuroAccessKyc`). Each PubSub item carries a full KYC XML payload.
2. **Schema validation** – Payloads are validated against `NeuroAccessKycProcess.xsd` (namespace `https://paiwise.tagroot.io/Schema/NeuroAccessKycProcess.xsd`). Legacy `urn:neuroaccess:kyc:1.0` remains a fallback but should be avoided for new work.
3. **Parsing** – `KycProcessParser` materializes the XML into `KycProcess`, `KycPage`, `KycSection`, and `ObservableKycField` objects. Pages wire up visibility conditions, localized text, and validation rules.
4. **Local storage** – The chosen template is stored as a `KycReference` with serialized field values. An autosave worker captures snapshots whenever the user progresses, ensuring the flow is resume-safe.
5. **UI binding** – `KycProcessViewModel` consumes the parsed process to drive `KycProcessPage.xaml`. Navigation, summary, peer review, and submission all operate on the same dynamic structure.
6. **Submission** – On apply, the service builds `Property` and `LegalIdentityAttachment` payloads from field mappings and uploads them through `XmppService`.

## Architecture

| Component | Responsibility |
|-----------|----------------|
| `KycService` (`../NeuroAccessMaui/Services/Kyc/KycService.cs`) | Fetches templates, applies them to references, validates XML, schedules snapshots, and prepares submission payloads. |
| `KycApplicationItem` (`../NeuroAccessMaui/Services/Kyc/KycApplicationItem.cs`) | Wraps a PubSub item (node `NeuroAccessKyc`). Extracts localized display names and exposes the raw process XML. |
| `KycProcessParser` (`../NeuroAccessMaui/Services/Kyc/KycProcessParser.cs`) | Parses schema-compliant XML into observable objects, wiring conditions, validation rules, and localization. |
| `KycProcessViewModel` (`../NeuroAccessMaui/UI/Pages/Kyc/KycProcessViewModel.cs`) | Coordinates UI navigation, validation, autosave calls, and submission. |
| `KycReference` (`../NeuroAccessMaui/Services/Kyc/KycReference.cs`) | Persisted snapshot of the currently active application: XML, field values, friendly name, progress, last page, and backend review metadata. |
| `SettingsViewModel.ClearCacheAsync` (`../NeuroAccessMaui/UI/Pages/Main/Settings/SettingsViewModel.cs`) | Provides a manual way to wipe cached templates and autosave data. |

## KYC Process Schema

Templates must conform to `NeuroAccessKycProcess.xsd`. Highlights:

- **Root `<KYCProcess>`** – Optional `<Name>` with localized `<Text lang="xx">` entries followed by one or more `<Page>` nodes.
- **Pages (`<Page id="">`)** – Provide `<Title>`, `<Description>`, optional `<Condition>`, standalone `<Field>` elements, and `<Section>` groups.
- **Fields** – Required attributes: `id`, `type`. Supported `type` values include text, date, picker, file/image uploads, phone, country, checkbox/radio, and info/label elements for static text. Fields may include:
  - `<Label>`, `<Hint>`, `<Placeholder>`, `<Description>` – Localized content.
  - `<Default>` – Seed value.
  - `<Metadata>` – Free-form payload consumed by specialized renderers.
  - `<ValidationRules>` – `LengthRule`, `RegexRule`, `DateRangeRule`, or `PersonalNumberRule`.
  - `<Options>` – Pickers with `<Option value="">` containing localized `<Text>`.
  - `<Condition>` – Show/hide logic evaluating other field values.
  - `<Mapping key="">` – Links field output to specific `LegalIdentity` properties. Transform hooks are reserved for downstream processors.
- **Sections** – Provide a `<Label>`/`<Description>` and contain nested `<Field>` elements.
- **Conditions** – Each `<Condition>` references existing field IDs, enforced by schema `xs:keyref`.

> Tip: Keep page/field IDs stable. They become dictionary keys in `KycProcess.Values`, are used for autosave, and appear in backend validation responses.

## Authoring Templates

1. **Start from the schema** – Copy one of the reference processes (e.g., `TestKYCNeuro.xml`) and adjust pages/fields as needed. Validate against `NeuroAccessKycProcess.xsd` using your preferred XML tooling.
2. **Localize content** – Every display string should use `<Text lang="xx">` entries. The parser automatically picks the user’s language with English fallback.
3. **Leverage conditions** – Set `<Condition>` blocks on pages, sections, or fields to create adaptive flows (for example, show company details only when `entityType == company`).
4. **Map outputs** – Ensure fields that should become identity claims define `<Mapping key="">` entries (e.g., `EMAIL`, `ADDR`, `BDAY`). The submission pipeline only transmits values that map to properties or attachments.
5. **File & image fields** – Use `type="file"` or `type="image"` for uploads. These automatically surface in `PreparePropertiesAndAttachmentsAsync` and become `LegalIdentityAttachment` entries.
6. **Validation rules** – Apply schema-provided rules for immediate client-side checks. Additional custom logic can run via metadata + `ObservableKycField` special cases if needed.

### Minimal example

```xml
<KYCProcess xmlns="https://paiwise.tagroot.io/Schema/NeuroAccessKycProcess.xsd">
  <Name>
    <Text lang="en">Consumer Onboarding</Text>
    <Text lang="sv">Kundregistrering</Text>
  </Name>
  <Page id="contact">
    <Title>
      <Text lang="en">Contact Details</Text>
    </Title>
    <Field id="firstName" type="text" required="true">
      <Label><Text lang="en">First name</Text></Label>
      <Mapping key="FN" />
      <ValidationRules>
        <LengthRule min="2" />
      </ValidationRules>
    </Field>
    <Field id="email" type="email" required="true">
      <Label><Text lang="en">Email</Text></Label>
      <Mapping key="EMAIL" />
    </Field>
  </Page>
</KYCProcess>
```

## Publishing Workflow

1. **Prepare the XML** – Produce one file per template. Keep IDs unique per file.
2. **Upload to PubSub** – Publish each XML document as a PubSub item under the node `NeuroAccessKyc`. Item IDs can follow any naming convention (for example, `KycConsumer`, `KycBusiness`).
   - If the broker exposes multiple PubSub services, ensure the user’s `TagProfile.PubSubJid` resolves to the service hosting `NeuroAccessKyc`. `KycService` first queries the configured JID, then falls back to the domain’s default component.
3. **Optional metadata** – Include a `<Name>` element so the template list can present localized titles before parsing.
4. **Versioning** – Publish a new item (or update the existing one) whenever requirements change. Clients fetch templates at runtime, so updates take effect on the next load or whenever cached data is cleared.
5. **Fallback** – Shipping a single “default” template is recommended so older clients can still onboard if the broker removes all custom forms.

## Runtime Behavior & Caching

- **Autosave** – `KycService` maintains a background autosave queue. `ScheduleSnapshotAsync` coalesces edits per reference, writing `KycReferenceSnapshot` entries serialized through `FileCacheManager`. This enables resume across app restarts.
- **Manual flush** – When the user submits or navigates away, `FlushSnapshotAsync` immediately persists the most recent state.
- **Cache clearing** – From Settings → “Clear Branding & KYC Cache”, the app deletes cached templates, invalidates PubSub-derived entries, and removes autosaved references so the next launch fetches fresh data.
- **Fallback template** – If the broker’s PubSub node is unavailable, the service loads `TestKYCNeuro.xml` from the app package (`backupKyc`). This ensures onboarding remains functional offline.
- **Localization** – Template `<Name>` values are used to label drafts and summaries. `KycService` updates `KycReference.FriendlyName` whenever a localized process name is available.

## Validation, Mapping & Submission

- **Client validation** – `KycProcessViewModel` calls `KycService.GetFirstInvalidVisiblePageIndexAsync` before allowing navigation to the summary. Each `ObservableKycField` exposes validation status and error text.
- **Snapshot metadata** – Snapshots capture `Progress`, `LastVisitedPageId`, and current navigation state (`Form`, `Summary`, `PendingSummary`). This data is surfaced in the UI and reused after restarts.
- **Preparing payloads** – `PreparePropertiesAndAttachmentsAsync` walks visible pages and sections, translating each mapped field into `Waher.Networking.XMPP.Contracts.Property` instances while collating attachments. Ordering is enforced by `KycOrderingComparer` to match backend expectations.
- **Submission** – Once the payload is built, `KycProcessViewModel` sends it through `XmppService`, then calls `ApplySubmissionAsync` so the reference reflects the newly created identity’s ID/state. Application reviews received later flow back through `ApplyApplicationReviewAsync`.

## Troubleshooting

- **Template not shown** – Verify the PubSub node `NeuroAccessKyc` exists and is reachable from the configured PubSub JID. Use `ServiceRef.XmppService.GetItemsPageAsync` manually or inspect broker logs.
- **Schema validation failures** – Device logs include `KycXmlValidation...` events indicating whether the primary or legacy schema failed. Fix the XML to match `NeuroAccessKycProcess.xsd`.
- **Stale forms** – Ask the user to clear the Branding/KYC cache or bump the PubSub item ID to force re-fetch.
- **Missing localization** – If localized `<Text>` entries are absent, UI labels fall back to English or the first language defined. Always supply at least English plus broker target languages.
- **Attachments ignored** – Ensure file/image fields are `required="true"` when needed and include `<Mapping>` entries if the backend expects them under specific property keys.
- **Conditional flows misbehaving** – Remember that conditions evaluate stored field values; blank/`null` fields default to `false`. Initialize defaults via `<Default>` or metadata if a condition depends on non-empty input.

By following this workflow, brokers can tailor onboarding to their compliance requirements, and developers maintain a single implementation that automatically adapts to each form definition.***
