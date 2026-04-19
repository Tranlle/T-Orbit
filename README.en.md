# T-Orbit

[中文版 README](https://github.com/Tranlle/T-Orbit/blob/main/README.md)

T-Orbit is a plugin-driven desktop tool host built with `.NET 10` and `Avalonia 12`.
It provides a stable host for built-in tools, external directory plugins, a shared design system, plugin variables, shortcuts, diagnostics, and workspace-oriented plugin pages.

## Overview

T-Orbit is not a single business application. It is a reusable desktop host with these responsibilities:

- Discover, load, start, stop, monitor, and dispose plugins.
- Provide shared navigation, settings, diagnostics, theme, localization, and persistence.
- Allow plugins to contribute main pages, home reports, header actions, page header summaries, and runtime capabilities.

## Current Capabilities

### Host

- Built-in plugin registration and external plugin discovery.
- Shared startup and shutdown coordination.
- Unified settings, monitor, keymap, and home experience.
- Application-level diagnostics collection and display.

### Plugin System

- `Visual` plugins with workspace pages.
- `HomeReport` plugins that contribute cards to the home page.
- Plugin variable declaration, injection, validation, and reminder publishing.
- Extensible page header actions and page summary models.

### Engineering

- SQLite persistence for preferences, key bindings, and plugin variables.
- Per-plugin execution gate for serialized lifecycle operations.
- Shared design system in `TOrbit.Designer`.
- Runtime language switching and theme switching.

## Built-in and Sample Plugins

| Plugin | Description |
| --- | --- |
| `torbit.home` | Home dashboard that aggregates built-in and plugin-provided reports. |
| `torbit.keymap` | Shortcut inspection, override, and reset management. |
| `torbit.settings` | Theme, font, language, close behavior, and plugin variable management. |
| `torbit.monitor` | Plugin runtime state, capability, and diagnostics inspection. |
| `torbit.migration` | EF Core migration management plugin with multi-profile support. |
| `torbit.promptor` | Prompt optimization plugin example. |
| `torbit.runtime-host` | Deploy, run, monitor, stop, and restart local `.NET` release packages. |

## Quick Start

### Requirements

- `.NET 10 SDK`
- A desktop environment capable of running Avalonia applications

### Build

```bash
dotnet build TOrbit.slnx
```

### Run

```bash
dotnet run --project TOrbit.App/TOrbit.App.csproj
```

### Test

```bash
dotnet test TOrbit.Core.Tests/TOrbit.Core.Tests.csproj
```

## Project Structure

```text
T-Orbit/
|- TOrbit.App/                 # Desktop host entry, shell, navigation, startup flow
|- TOrbit.Core/                # Core services: plugins, storage, variables, shortcuts, diagnostics
|- TOrbit.Designer/            # Shared design system, theming, localization, reusable controls
|- TOrbit.Plugin.Core/         # Plugin contracts, metadata, base classes
|- TOrbit.Plugin.Home/         # Built-in home plugin
|- TOrbit.Plugin.KeyMap/       # Built-in keymap plugin
|- TOrbit.Plugin.Settings/     # Built-in settings plugin
|- TOrbit.Plugin.Monitor/      # Built-in monitor plugin
|- TOrbit.Plugin.Migration/    # External migration plugin
|- TOrbit.Plugin.Promptor/     # External promptor plugin
|- TOrbit.Plugin.RuntimeHost/  # External .NET runtime host plugin
|- TOrbit.Core.Tests/          # Core tests
|- TOrbit.wiki/                # Wiki sources
```

## Architecture Summary

- The app creates the shell first, then performs warmup work asynchronously.
- `AppStartupCoordinator` is responsible for built-in registration, external discovery, variable injection, shortcut loading, and home report registration.
- `AppShutdownCoordinator` reuses plugin execution gates to avoid conflicts with runtime lifecycle operations during shutdown.
- `MainViewModel` coordinates navigation state, current page, page header actions, and plugin-level reminders.

## Documentation

- [Chinese README](https://github.com/Tranlle/T-Orbit/blob/main/README.md)
- [Wiki Home](https://github.com/Tranlle/T-Orbit/wiki/Home)
- [English Wiki Home](https://github.com/Tranlle/T-Orbit/wiki/Home-en)
- [Architecture](https://github.com/Tranlle/T-Orbit/wiki/Architecture)
- [Plugin System](https://github.com/Tranlle/T-Orbit/wiki/Plugin-System)
- [Plugin Variable Management](https://github.com/Tranlle/T-Orbit/wiki/Plugin-Variable-Management)
- [Theme Customization](https://github.com/Tranlle/T-Orbit/wiki/Theme-Customization)
- [Migration Plugin](https://github.com/Tranlle/T-Orbit/wiki/Migration-Plugin)
- [Creating a Plugin](https://github.com/Tranlle/T-Orbit/wiki/Creating-a-Plugin)

## License

See [LICENSE](https://github.com/Tranlle/T-Orbit/blob/main/LICENSE).
