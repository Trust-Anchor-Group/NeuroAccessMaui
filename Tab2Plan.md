# Tab2 Plan

## Overview
`TabNavigationHost` delivers a data-driven, MVVM-friendly tab surface that mirrors the Sharpnado.TabHost + ViewSwitcher pattern while staying aligned with NeuroAccessMaui’s architecture. The control renders tabs, coordinates selection through `SelectedIndex` bindings, and leaves navigation to the owning view model.

## Requirements
- Provide a `TabNavigationHost` control that visually represents tabs and synchronizes selection with `ViewSwitcher` (or any consumer) via shared bindings.
- Expose bindable `SelectedIndex` and `SelectedItem` properties with two-way binding support.
- Accept an `ItemsSource` of lightweight `TabDefinition` instances to keep templates reusable.
- Ship default templates for common scenarios: bottom pill, top underline, vertical tabs, segmented style, and icon-only bars.
- Offer host-level customization through `TabPlacement`, `IsScrollable`, `TouchEffectType`, `BadgeTemplate`, and `TabAnimationStyle`.
- Keep the control visual-only: no navigation service coupling, no code-behind requirements.
- Honor existing safe-area and keyboard inset helpers without duplicating logic.
- Work seamlessly with lazy loading patterns exposed by `ViewSwitcher`.

## Goal Example Usage
```xml
<tabs:TabNavigationHost SelectedIndex="{Binding SelectedTabIndex, Mode=TwoWay}"
                        TabPlacement="Bottom"
                        TouchEffectType="OpacityHighlight"
                        AnimationStyle="FadeScale"
                        ItemTemplate="{StaticResource DefaultBottomPillTabTemplate}">
    <tabs:TabNavigationHost.ItemsSource>
        <x:Array Type="{x:Type models:TabDefinition}">
            <models:TabDefinition Title="Home"
                                  Icon="home.png"
                                  BadgeCount="{Binding HomeBadgeCount}" />
            <models:TabDefinition Title="Contracts"
                                  Icon="contract.png"
                                  BadgeCount="{Binding ContractsBadgeCount}" />
            <models:TabDefinition Title="Profile"
                                  Icon="profile.png" />
        </x:Array>
    </tabs:TabNavigationHost.ItemsSource>
</tabs:TabNavigationHost>

<controls:ViewSwitcher SelectedIndex="{Binding SelectedTabIndex, Mode=TwoWay}">
    <views:HomeView />
    <views:ContractsView />
    <views:ProfileView />
</controls:ViewSwitcher>
```

## Implementation Summary
- Added `TabDefinition` (observable descriptor with key, title, icon, badge count, selection state, prominent flag, command + parameter, tag).
- Introduced `TabNavigationHost` with configurable layout (grid + optional scrollview), bindable properties, and touch feedback animations.
- Supported animation selection via `TabAnimationStyle` (`None`, `UnderlineSlide`, `FadeScale`) and touch feedback via `TabTouchEffectType` (`None`, `OpacityHighlight`, `Scale`).
- Delivered default templates in `Resources/Templates/TabNavigationTemplates.xaml`, each honoring optional `BadgeTemplate` overrides and built-in badge visibility via converters.
- Wired `TabNavigationHost` selection changes to update `TabDefinition.IsSelected`, execute optional commands, and trigger simple animations without owning navigation.

## Customization Strategy
- Start with `ItemsSource` + `ItemTemplate`; switch templates using supplied defaults or app-specific ones.
- Use `TabPlacement` (`Top`, `Bottom`, `Left`, `Right`) to control orientation; toggle `IsScrollable` to wrap the layout in an orientation-aware `ScrollView`.
- Supply a `BadgeTemplate` to override the default badge while retaining `BadgeCount` binding; default badges disappear when either `BadgeTemplate` is present or the count is zero/null.
- Choose touch feedback (`OpacityHighlight` or `Scale`) for tap animations and `AnimationStyle` for post-selection emphasis (currently `FadeScale` pulse; underline visuals handled inside default templates).
- Expose navigation by mapping `TabDefinition.Key` or `Command` in the view model; tabs themselves remain pure selection widgets.

## v1 Specification
| Feature | Details |
| --- | --- |
| Control | `TabNavigationHost : ContentView` |
| Items | `ItemsSource`, `ItemTemplate`, optional `BadgeTemplate` |
| Selection | `SelectedIndex` + `SelectedItem` (two-way) |
| Layout | `TabPlacement`, `IsScrollable` with automatic grid/stack orchestration |
| Feedback | `TabTouchEffectType` (`None`, `OpacityHighlight`, `Scale`) |
| Animations | `TabAnimationStyle` (`None`, `UnderlineSlide`, `FadeScale`) |
| Descriptor | `TabDefinition` (Title, Icon, BadgeCount, IsEnabled, IsProminent, IsSelected, Command, CommandParameter, Key, Tag) |
| Default templates | `DefaultBottomPillTabTemplate`, `DefaultTopUnderlineTabTemplate`, `DefaultVerticalTabTemplate`, `DefaultSegmentedTabTemplate`, `DefaultIconOnlyTabTemplate` |
| Resources | `Resources/Templates/TabNavigationTemplates.xaml`, converters (`NumberToBoolConverter`, `NullToBooleanConverter`) |

## Usage Notes
- Bind `SelectedIndex` on both `TabNavigationHost` and `ViewSwitcher` to a shared view-model property for synchronized transitions.
- Use `TabDefinition.IsProminent` inside templates to style accent tabs (e.g., larger center button).
- When using `IsScrollable="True"`, templates maintain intrinsic sizing; non-scrollable layouts distribute tabs evenly via grid star sizing.
- Commands execute after selection; combine with `CommandParameter` or `Key` to trigger navigation orchestration in the view model.
- Safe-area and keyboard inset management remains with existing page infrastructure; host exposes clean layout hooks for consumers to apply padding if necessary.

## Open Questions
- Should a future v2 add a dedicated layout template hook to simplify asymmetric compositions (floating action tabs, custom chrome)?
- Do we need built-in support for indicator translation (e.g., a programmatic underline) beyond template-driven visuals?
- Are additional accessibility affordances required (automation ids, announce patterns) at the control level or should they stay template-driven?
