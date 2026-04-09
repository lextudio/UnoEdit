# UnoEdit

UnoEdit is a desktop-first port of AvalonEdit to Uno Platform.

Current scope:

- Target Uno Skia Desktop first.
- Do not target mobile during the bootstrap phase.
- Port the document model and editor core before building the Uno control shell.

Repository layout:

- `src/UnoEdit`: shared desktop-targeted library assembled from AvalonEdit's platform-neutral code plus Uno-specific forks
- `src/UnoEdit.Tests`: desktop-targeted regression suite for the shared library
- `src/UnoEdit.Sample`: Uno Skia Desktop host for the first editor shell

Current status:

- `UnoEdit`, `UnoEdit.Tests`, and `UnoEdit.Sample` all target `net10.0-desktop`
- Phase 4 shared/WPF splits from AvalonEdit are being reused in UnoEdit with Uno-specific `.uno.cs` follow-through
- The shared rendering host now includes Uno-side `TextView` collection/service plumbing plus `VisualLine.uno.cs` and `VisualLineText.cs`
- Folding, highlighting, navigation, selection, editing, clipboard, undo/redo, references, and theme plumbing are all integrated in the desktop sample
- Full solution build is green, including the desktop host
- The NUnit regression suite runs through `NUnitLite` via `dotnet run --project src/UnoEdit.Tests/UnoEdit.Tests.csproj`
- Current regression total: `210` passing tests

Next steps:

1. Continue Phase 5 by completing more `.uno.cs` counterparts for the Phase 4 shared rendering/editor splits.
2. Keep the desktop regression suite green as more shared AvalonEdit files move under UnoEdit.
3. Tighten rendering fidelity and remaining IME/runtime edge cases in the Uno Skia host.
