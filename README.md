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

## Documentation Index

Core docs:

- Getting Started: [Content/getting_started.md](Content/getting_started.md)
- Architecture: [Content/architecture.md](Content/architecture.md)
- Services overview: [Content/services.md](Content/services.md)
- Contributing Guide: [Content/contributing.md](Content/contributing.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- License: [LICENSE](LICENSE)

Additional (planned) topics – placeholders not yet created (PRs welcome):

- Dependencies (NuGet package rationale)
- Navigation flow
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
