# UnoEdit

UnoEdit is a desktop-first port of AvalonEdit to [Uno Platform](https://platform.uno).

![UnoEdit on macOS](https://github.com/lextudio/UnoEdit/blob/master/images/macos.png?raw=true)

Current scope:

- Target Uno Skia Desktop (WinUI 3 port is included but not the primary focus).
- Do not target mobile during the bootstrap phase (v0.x.x).
- Port the document model and editor core as well as the Uno control shell.

## Supported Platforms

- Windows 11 (Windows 10 may work but is not a primary target)
- macOS, 3 most recent versions from 2023-2025
- Ubuntu latest LTS (other Linux distributions may work but are not primary targets)

> If you are looking for support of a specific platform, business sponsorship is the way to accelerate that work. Please reach out to us at [homepage](https://lextudio.com).

## Get Started

The main deliverable is a few NuGet packages:

- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UnoEdit.svg?label=LeXtudio.UnoEdit)](https://www.nuget.org/packages/LeXtudio.UnoEdit) The core editor component, including document model, editing engine, and platform-agnostic UI logic.
- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UnoEdit.TextMate.svg?label=LeXtudio.UnoEdit.TextMate)](https://www.nuget.org/packages/LeXtudio.UnoEdit.TextMate) Optional TextMate integration library built on top of UnoEdit.
- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UI.Text.Core.svg?label=LeXtudio.UI.Text.Core)](https://www.nuget.org/packages/LeXtudio.UI.Text.Core) The core text rendering and layout engine, shared across UnoEdit and potentially other text-based controls.

Clone this repo and check out the sample projects to see how to use UnoEdit in your own applications (Uno Platform and WinUI 3).

## Current Status

- Public API parity against AvalonEdit currently measures `193/193` types and `1296/1296` members.
- Behavioral parity is tracked separately with the local parity detector. The latest stub-aware pass reports `0` suspected stubs.
- The shared regression suite currently passes `281/281`.

## TODO Items Before v1.0.0

- [ ] Right-to-left text support
- [ ] IME support improvements (currently functional but not fully polished)
- [ ] Accessibility support (screen readers, keyboard navigation, etc.)
- [ ] Performance optimizations for large documents (virtualization, incremental layout, etc.)
- [ ] TextMate integration (functional but needs heavy polishing)

## Credits & Third-Party

UnoEdit builds on and includes code from the AvalonEdit project (ICSharpCode.AvalonEdit).
AvalonEdit is licensed under the MIT License. See `avalonedit/LICENSE` and
`THIRD_PARTY_NOTICES.md` for attribution and full license text. The NuGet packages
produced by this repository also include the third-party license text and notices.
