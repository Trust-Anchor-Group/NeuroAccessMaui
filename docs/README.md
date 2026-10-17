# Neuro-Access developer docs

These guides explain how Neuro-Access is built and how to work on it. Start with getting started and architecture, then read the guide for the area you are about to change. The code style and app patterns every change must follow are in [AGENTS.md](../AGENTS.md).

## Start here

| Guide | What it covers |
|---|---|
| [Getting started](getting-started.md) | Setting up your machine, building and running on each platform, and troubleshooting |
| [Architecture](architecture.md) | The solution's projects, app startup, dependency injection, MVVM base classes and persistence |
| [Contributing](contributing.md) | Branches, pull requests, the changelog, translations and issue reports |

## Inside the app

| Guide | What it covers |
|---|---|
| [Services](services.md) | The app services behind `ServiceRef`, their lifecycle and how to add one |
| [Navigation](navigation.md) | The navigation service, the `CustomShell` presenter, popups, toasts, back handling, safe areas and keyboard insets |
| [Animations](animations.md) | The animation coordinator, built-in animations and how to add your own |
| [Notifications](notifications.md) | The notification service, push notifications through Firebase, and routing taps to pages |

## Identity

| Guide | What it covers |
|---|---|
| [Onboarding](onboarding.md) | The first-run flow, its scenarios and steps, and how to change them |
| [KYC](kyc.md) | Dynamic KYC processes, document scanning, and how brokers author and publish templates |

## For broker operators

| Guide | What it covers |
|---|---|
| [KYC](kyc.md) | Writing and publishing KYC templates |
| [Branding](branding.md) | Publishing your own colors and images to the app |

## Elsewhere in the repository

- [CHANGELOG.md](../CHANGELOG.md) lists notable changes per release.
- [`plans/`](../plans) holds requirements, design and task notes from earlier feature work.
- [AGENTS.md](../AGENTS.md) is the code style and the guide for AI coding agents.
