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

