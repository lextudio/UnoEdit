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
- Phase 6 is complete for the current desktop-first scope: the Uno desktop host now has explicit mouse-selection, navigation, and editing handler files instead of keeping all interaction logic in one code-behind file
- Phase 7 is complete for the current desktop-first scope: the desktop host now includes a real Uno search panel with `Ctrl+F`, `F3`, and `Shift+F3` support
- Phase 8 is complete for the current desktop-first scope: current Uno XAML files are paired explicitly, a shared `Themes/generic.xaml` dictionary exists, and the sample app merges it at startup
- Full solution build is green, including the desktop host
- The NUnit regression suite runs through `NUnitLite` via `dotnet run --project src/UnoEdit.Tests/UnoEdit.Tests.csproj`
- Current regression total: `210` passing tests

Next steps:

1. Start the next post-bootstrap phase on deeper control templating, completion/snippet UI, or broader source convergence.
2. Continue converging more upstream AvalonEdit editing/rendering files into `src/UnoEdit` where that reduces duplication without destabilizing the desktop host.
3. Tighten rendering fidelity and remaining IME/runtime edge cases in the Uno Skia host.
