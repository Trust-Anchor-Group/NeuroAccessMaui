# Neuro-Access

**Neuro-Access** is a .NET MAUI application (C#/.NET 8) provided by [Trust Anchor Group](https://trustanchorgroup.com/), based on the original [TAG ID App](https://github.com/Trust-Anchor-Group/IdApp).

The app gives users secure access to Neuro services: digital identities, smart contracts, tokens, eDaler wallets, messaging (XMPP) and more.

---

## Quick Start

1. Install .NET SDK (see `global.json` – currently 8.0.413).
1. Clone the repo:

```bash
git clone https://github.com/Trust-Anchor-Group/NeuroAccessMaui.git
cd NeuroAccessMaui
```

1. Restore & build:

```bash
dotnet restore
dotnet workload restore
dotnet build
```

1. Run (Android on Windows by default):

```bash
dotnet build -t:Run -f net8.0-android
```

1. See extended guidance in [Getting Started](Content/getting_started.md).

---

## Navigation & UI Infrastructure

Neuro-Access uses a custom presentation layer that sits on top of MAUI navigation:

- `NavigationService` keeps an internal stack of `BaseContentPage` screens and serializes navigation requests via a main-thread queue.
- `CustomShell` implements the `IShellPresenter` interface and hosts page content, global bars, popup/toast layers, and hardware back handling in one place.
- `PopupService` pushes and dismisses popups through the presenter, ensuring consistent overlay transitions and lifecycle hooks.

To navigate or show popups from a view model or service, resolve the appropriate service (for example `ServiceRef.NavigationService` or `ServiceRef.PopupService`) and call the async helpers (`GoToAsync`, `PushAsync`, `PopAsync`, etc.). See [Content/navigation.md](Content/navigation.md) for full examples and diagrams showing how these pieces interact.

---

## Documentation Index

Core docs:

- Getting Started: [Content/getting_started.md](Content/getting_started.md)
- Architecture: [Content/architecture.md](Content/architecture.md)
- Navigation & Presenter layer: [Content/navigation.md](Content/navigation.md)
- Services overview: [Content/services.md](Content/services.md)
- Contributing Guide: [Content/contributing.md](Content/contributing.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- License: [LICENSE](LICENSE)

Additional (planned) topics – placeholders not yet created (PRs welcome):

- Dependencies (NuGet package rationale)
- Platform-specific code notes
- API reference (public service & model surfaces)
- Deployment (build, signing, store submission)
- Troubleshooting / FAQ

If you start one of these, create a file under `Content/` (e.g. `Content/dependencies.md`) and link it here in a PR.

---

## Contributing

See [Content/contributing.md](Content/contributing.md) for branching model (Git Flow), coding style and PR process.

---

## Versioning & SDK

Target frameworks: Android (`net8.0-android`) by default on Windows. iOS is enabled automatically on non-Windows hosts (see `NeuroAccessMaui.csproj`). The locked SDK version is governed by `global.json`. Update both `global.json` and the Getting Started guide together when bumping SDK.

---

## Security & Reporting Issues

For security-sensitive issues do not open a public issue. Instead contact Trust Anchor Group via the website. For general bugs use GitHub Issues.

---

## License

See [LICENSE](LICENSE).
