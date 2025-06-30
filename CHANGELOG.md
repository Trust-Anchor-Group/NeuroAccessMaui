# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.6.1] - 2025/06/30

### Added

- Added Open Chat button when viewing an identity
- Added popup explaing that app might need to be restarted after changing language
- Added Popup if cache is cleared successfully
- Added Bottom bar with navigation options
- Added Apps page
- Added a toggle in settings for beta features
- Added button on main page to open Settings
- Added Info popups when clicking a disabled or beta app
- Added a way to open contracts refrenced in a contract reference parameter

### Changed

- Make Share QR popup automatically close after a set interval
- Moved wallet to bottom bar
- Moved flyout menu contents to new app page
- Fixed various layout issues
- Fixed some wrong colors
- Updated Waher nugets to latest version
- On windows machines only .net8.0-Android is targeted
- Improved date handling
- Fixes style inheritance and triggers.
- Fixed chat input background color.

### Fixed

- Fixed contacts not being added properly
- Fixed various bugs and errors regarding contract creation, signing and validation.
- Removed underlines from picker controls on Android devices

### Removed

- Removed flyout menu

## [2.5.1] - 2025-06-16

### Added

- PubSub Support in XmppService
- Fetching of dynamic branding material, colors & images
- InternetContentService, to easily cache files fetched from the web
- XmppGetter, A InternetContentGetter for xmpp items (Currently only PubSub)
- ThemeService to handle loading of branding and themes

### Changed

- Refactored AttachmentCacheService
- Updated .NET sdk to 8.0.411

### Fixed

- Updated all localization resources

## [2.4.1] - 2025-04-16

