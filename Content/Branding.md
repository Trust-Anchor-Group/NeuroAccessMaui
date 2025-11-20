# Branding & Theme Service

Dynamic branding in NeuroAccess lets each broker deliver its own color palette and imagery without republishing the app. The `ThemeService` (`../NeuroAccessMaui/Services/Theme/ThemeService.cs`) fetches provider descriptors, downloads the referenced resource dictionaries, and merges them with the bundled light/dark themes. This guide explains how the system works today (Branding V2), how to author your own theme assets, how to publish them through PubSub, and what to expect when rolling changes out to clients.

## Table of Contents

- [Runtime Overview](#runtime-overview)
- [Branding Descriptor v2](#branding-descriptor-v2)
- [Create Color Palettes](#create-color-palettes)
- [Branding Images](#branding-images)
- [Publishing Workflow](#publishing-workflow)
- [Refresh & Rollout Expectations](#refresh--rollout-expectations)
- [Troubleshooting](#troubleshooting)

## Runtime Overview

1. **Boot & onboarding** – After the user completes onboarding (`LoadingPage`), `ThemeService.ApplyProviderTheme` runs once for the active domain.
2. **Descriptor fetch** – The service probes `https://{domain}/PubSub/NeuroAccessBranding/BrandingV2` (preferred) and falls back to `/Branding` (V1). XML payloads are validated against the bundled schemas (`Resources/Raw/Schemas/NeuroAccessBrandingV2.xsd`).
3. **Palette download** – Each `<ColorsUri>` is downloaded (HTTP or `xmpp:`). The referenced `ResourceDictionary` replaces the built-in light/dark dictionaries, so apps instantly use the provided brushes.
4. **Image registration** – `<ImageRef>` entries are mapped by ID. View models call `ThemeService.GetImageUri(id)` to show provider banners/logos as soon as `ThemeService.ThemeLoaded` completes.
5. **Caching & resilience** – Descriptors, dictionaries, and images are cached for 7 days per PubSub JID. The service retries transient failures (0s/2s/2s) and falls back to bundled colors if the provider theme is missing or malformed.

## Branding Descriptor v2

Version 2 descriptors live under the namespace `https://paiwise.tagroot.io/Schema/NeuroAccessBrandingV2.xsd`. The schema supports multiple palettes and optional metadata while remaining backward compatible with the original urn (`urn:neuroaccess:branding:2.0`). Key elements:

| Element | Purpose | Notes |
|---------|---------|-------|
| `<Version major="" minor="" patch="" />` | Declares the minimum NeuroAccess build expected to consume the theme. | The client currently logs the version but still applies the descriptor even if the app is newer than the payload. |
| `<ColorPalettes>` → `<ColorsUri theme="light|dark" isDefault="true|false">` | Points to a MAUI `ResourceDictionary` for a specific theme. | Provide at least one `light` and one `dark` entry; the `theme` attribute is required for V2. |
| `<Images>` → `<ImageRef id="" uri="" contentType="">` | Registers arbitrary branding assets. | IDs are case-insensitive and must be unique. URIs can be HTTP(S) or XMPP. |

### Sample descriptor

```xml
<?xml version="1.0" encoding="utf-8"?>
<BrandingDescriptor xmlns="https://paiwise.tagroot.io/Schema/NeuroAccessBrandingV2.xsd">
  <Version major="2" minor="5" patch="1" />
  <ColorPalettes>
    <ColorsUri theme="light" isDefault="true">
      https://neuron.example.com/branding/MyLightTheme.xaml
    </ColorsUri>
    <ColorsUri theme="dark">
      https://neuron.example.com/branding/MyDarkTheme.xaml
    </ColorsUri>
  </ColorPalettes>
  <Images>
    <ImageRef id="banner_large_light"
              uri="https://neuron.example.com/branding/banners/banner-large-light.png"
              contentType="image/png"
              width="1125"
              height="720" />
    <ImageRef id="banner_large_dark"
              uri="xmpp:NeuroAccessBranding@pubsub.example.com/banner-large-dark.png"
              contentType="image/png"
              width="1125"
              height="720" />
  </Images>
</BrandingDescriptor>
```

### V1 fallback

If `BrandingV2` is missing, the app falls back to `/Branding` (namespace `urn:neuroaccess:branding:1.0`). V1 accepts a single `<ColorsUri>` pointing to a dictionary that contains both light and dark keys (suffix `Light`/`Dark`). Maintaining a V1 item is deprecated and will not be supported in future versions of access.

## Create Color Palettes

Resource dictionaries define every color key the app expects (`Resources/Styles/Colors.xaml` shows the canonical set). When you publish provider themes you replace these dictionaries entirely, so ensure the new files contain the same keys—even if some values remain identical to the defaults.

1. **Start from the shipped palette** – Copy `NeuroAccessMaui/Resources/Styles/Colors.xaml` and adjust the colors that should be branded.
2. **Split per theme** – For V2 we ship independent files (one for light, one for dark). Keep file names descriptive such as `ContosoLight.xaml` / `ContosoDark.xaml`.
3. **Keep resource keys intact** – The UI references keys like `BrandColorsNeuroAccessWL`, `ButtonAccessPrimarybg`, etc. Changing the key names will result in missing colors at runtime.
4. **Validate locally** – Load the dictionary from a throwaway MAUI page or unit harness to ensure it compiles; malformed XAML will be rejected by `ThemeService`.

### Example palette snippet (light)

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
  <Color x:Key="BrandColorsNeuroAccessWL">#114B5F</Color>
  <Color x:Key="BrandColorsNeuroAccessv500WL">#0C3A4B</Color>
  <Color x:Key="ButtonAccessPrimarybg">#FF6F59</Color>
  <Color x:Key="ButtonAccessPrimaryContentWLLight">#FFFFFF</Color>
  <!-- continue defining the rest of the palette keys -->
</ResourceDictionary>
```

For the dark dictionary adjust the values to maintain sufficient contrast, but keep the same key set.

## Branding Images

All branded surfaces (home banner, onboarding cards, logos) request URIs from `ThemeService.ImageUris`. Provide high-resolution assets and register them with the IDs below to replace the stock graphics.

| ImageRef ID | Recommended usage |
|-------------|-------------------|
| `banner_large_light` / `banner_large_dark` | Hero image shown on the home page (`HomeViewModel`). |
| `banner_small_light` / `banner_small_dark` | Smaller banners used across onboarding, applications, and identity views. |
| `logo_light` / `logo_dark` | Future-proof slots for provider-specific logos (currently reserved). |

You may publish additional IDs; retrieve them via `ServiceRef.ThemeService.GetImageUri("custom_id")` in view models. Always host both a light and dark variant so the UI stays legible in either theme.

## Publishing Workflow

1. **Host the color dictionaries**  
   - Upload the `.xaml` files to HTTPS storage (`https://{domain}/branding/...`) or publish them as PubSub items (so the URI becomes `xmpp:NeuroAccessBranding@pubsub.example.com/ColorsLight`).  
   - Ensure the endpoints are publicly reachable; `ThemeService` uses `InternetContent.GetAsync`, so any registered `IContentGetter` (HTTP, HTTPS, XMPP) will work.

2. **Host branding images**  
   - Prefer HTTPS with caching headers. XMPP URIs are also supported if you want to keep everything on the broker.  
   - Keep filenames stable. When a URI changes the app treats it as a brand-new image and re-downloads it.

3. **Author & validate the descriptor**  
   - Reference the hosted URIs under `<ColorPalettes>` and `<Images>`.  
   - Validate against `NeuroAccessBrandingV2.xsd` before uploading to catch schema issues early. Any validation error is logged and causes the app to fall back to the built-in theme.

4. **Publish to PubSub**  
   - Node name: `NeuroAccessBranding`.  
   - Primary item ID: `BrandingV2`.  
   - Optional fallback: `Branding` (V1).  
   - The HTTP endpoint (`https://{domain}/PubSub/NeuroAccessBranding/{ItemId}`) is expected to proxy the PubSub item; most brokers expose this automatically.

5. **Verify from the app**  
   - Sign in with a device that points to the domain you just branded.  
   - Wait for the home page to load; `ThemeService.ThemeLoaded.Task` resolves once colors and images are ready.  
   - Inspect banners, buttons, and dialogs to confirm they use the new palette.

## Refresh & Rollout Expectations

- **Cache lifetime** – Descriptors, dictionaries, and images share the `Constants.Cache.DefaultImageCache` TTL (7 days). Once downloaded, a client will continue using the cached assets until the entry expires or is manually cleared.
- **Session behavior** – `ThemeService` only attempts to apply a provider theme once per domain per app session. Restarting the app (or switching broker domain) triggers a fresh attempt.
- **Manual invalidation** – Users can open **Settings → Clear Branding & KYC Cache**. This command removes internet cache entries, invalidates PubSub-scoped cache records, calls `ThemeService.ClearBrandingCacheForCurrentDomain`, and deletes cached KYC drafts.
- **Publishing updates** – To force clients onto a new palette immediately, publish the updated assets, ask users to clear the branding cache, then relaunch the app. Otherwise, devices will pick up the change automatically after the 7-day TTL expires.

## Troubleshooting

- **No branding applied** – Confirm the `NeuroAccessBranding` node exposes `BrandingV2`. If the HTTP endpoint returns 404, the app logs `Unsupported domain` and sticks to the built-in theme.
- **Validation errors** – Check the device logs for `BrandingXmlValidation...` entries. These indicate schema mismatches (missing attributes, invalid namespace, malformed XML).
- **Images missing** – Ensure every `<ImageRef>` uses an absolute URI, correct `contentType`, and matches the IDs consumed by the UI. Remember that the `ImageUris` dictionary is case-insensitive, but empty strings are ignored.
- **Dark mode looks wrong** – Verify your dark dictionary overrides all keys that were light-only. Because we swap the entire resource dictionary, any missing key falls back to the light palette and may create low contrast surfaces.
- **Updates not visible** – Cached assets persist for 7 days. Clear the cache from Settings or bump the URI (e.g., versioned file name) to force a re-download.
- **Fallback behavior** – When both V2 and V1 fail, `ThemeService` reinstates the built-in `Light.xaml`/`Dark.xaml` dictionaries and logs the failure. This ensures the UI remains functional even if branding assets are temporarily unavailable.

For implementation details review `ThemeService` and `IThemeService` under `../NeuroAccessMaui/Services/Theme`. These classes document every retry, caching, and merging rule that the app applies when consuming your branding assets.
