# UnoEdit

UnoEdit is a desktop-first port of AvalonEdit to Uno Platform.

Current scope:

- Target Uno Skia Desktop first.
- Do not target mobile during the bootstrap phase.
- Port the document model and editor core before building the Uno control shell.

Repository layout:

- `src/UnoEdit`: shared desktop-targeted library assembled from AvalonEdit's platform-neutral code plus Uno-specific forks
- `src/UnoEdit.TextMate`: optional TextMate integration library built on top of `UnoEdit`
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
- The latest macOS IME regression was fixed at the Uno focus boundary, preserving the shared AvalonEdit/UnoEdit structure while restoring native IME focus correctly
- Phase 9 source convergence has started: upstream `XmlFoldingStrategy.cs`, `TextViewWeakEventManager.cs`, `AbstractMargin.cs`, `HtmlOptions.cs`, and `PixelSnapHelpers.cs` are now linked directly; all except `AbstractMargin.cs` are covered by shared regression tests, and `AbstractMargin.cs` is build-verified against the current shared surface
- The remaining `Utils/ExtensionMethods.cs` backlog item is now closed through an UnoEdit-maintained compatibility subset that preserves the shared math/XML/device-transform helpers without reintroducing WPF visual-tree dependencies
- Highlighted-line HTML export is back: `HtmlRichTextWriter.cs` is now linked, `HighlightedLine.ToHtml()` / `ToRichText()` are restored for the shared highlighting stack, and the minimal rich-text model path is available again without reviving the WPF inline-builder surface
- `RichTextModelWriter.cs` is now linked as well, so the shared highlighting stack can write formatted text back into documents and preserve highlighting state during insertion
- Shared search command definitions now exist again through a narrow `SearchCommands.cs` fork; the command-routing/input-handler half is still deferred until the shared editing surface is expanded
- Shared AvalonEdit command definitions now exist again through a narrow `AvalonEditCommands.cs` fork; this restores the command identifiers and default gestures without pretending the shared `TextEditor` type is already in the library
- Phase 10 is now started with a separate `src/UnoEdit.TextMate` library instead of baking TextMateSharp into the core `src/UnoEdit` assembly
- The sample app now proves the TextMate path end-to-end by attaching a C# TextMate grammar/theme-backed highlighted-line source to the editor
- Phase 11 has started with API parity expansion on the public `TextEditor` surface: core AvalonEdit-style members such as `Text`, `SelectedText`, `CaretOffset`, `SelectionStart`, `SelectionLength`, `Load`/`Save`, `Undo`/`Redo`, `Encoding`, `SyntaxHighlighting`, and `Options` are now exposed on the Uno control
- API parity is now measurable in-repo through `tools/ApiParity` and `scripts/check_api_parity.sh`, including a justifications file for intentional Uno/TextMate surface divergences
- Full solution build is green, including the desktop host
- The NUnit regression suite runs through `NUnitLite` via `dotnet run --project src/UnoEdit.Tests/UnoEdit.Tests.csproj`
- Current regression total: `244` passing tests
- The headless Uno runtime-test host is now green again after fixing the search-panel navigation regression

Next steps:

1. Continue selective source convergence from AvalonEdit into `src/UnoEdit` where that reduces duplication without destabilizing the desktop host.
2. Start the next product-facing track from the stabilized base: ILSpy integration, completion/snippet UI, or deeper editor fidelity work.
3. Expand IME validation and remaining desktop-fidelity checks where they provide real value.
