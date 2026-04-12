# UnoEdit

UnoEdit is a desktop-first port of AvalonEdit to Uno Platform.

Current scope:

- Target Uno Skia Desktop first, and possibly WinUI 3 next.
- Do not target mobile during the bootstrap phase (v1.x).
- Port the document model and editor core as well as the Uno control shell.

## Supported Platforms

- Windows 11 (Windows 10 may work but is not a primary target)
- macOS, 3 most recent versions from 2023-2025
- Ubuntu latest LTS (other Linux distros may work but are not primary targets)

> If you are looking for support of a specific platform, business sponsorship is the best way to accelerate that work. Please reach out to us at [homepage](https://lextudio.com).

## Deliverables

The main deliverable is a few NuGet packages:

- `UnoEdit` — the core editor component, including document model, editing engine, and platform-agnostic UI logic.
- `UnoEdit.TextMate` — optional TextMate integration library built on top of `UnoEdit`.

## Current Status

- Public API parity against AvalonEdit currently measures `193/193` types and `1295/1295` members.
- Behavioral parity is tracked separately with the local parity detector. The latest stub-aware pass reports `50` suspected stubs.
- The shared regression suite currently passes `268/268`.

## Recent Phase 11 Progress

- Replaced the shared text-run property bag and construction context placeholders with concrete cloneable state.
- Replaced the formatted-text and drawing compatibility shells so formatted runs now carry prepared text, compute bounds, and record draw operations.
- Replaced the next cursor and geometry placeholders so cursor invalidation is observable, geometry figures can be closed explicitly, and text-composition system text is preserved when provided.
