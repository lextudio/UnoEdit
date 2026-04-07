# UnoEdit

UnoEdit is a desktop-first port of AvalonEdit to Uno Platform.

Current scope:

- Target Uno Skia Desktop first.
- Do not target mobile during the bootstrap phase.
- Port the document model and editor core before building the Uno control shell.

Repository layout:

- `src/UnoEdit`: portable core assembled from AvalonEdit's non-WPF document and utility layers
- `tests/UnoEdit.Tests`: cross-platform regression tests for the portable core
- `samples/UnoEdit.Skia.Desktop`: planned Uno Skia Desktop host for the first editor shell

Current status:

- Portable `UnoEdit` core project created on .NET 10
- Cross-platform test project created on .NET 10
- WPF-only `LogicalDirection` and weak-event plumbing replaced with small compatibility shims for the core port

Next steps:

1. Keep expanding the portable source set until the document model tests are green.
2. Keep the portable test suite green as more shared source is added.
3. Add a desktop-only Uno Skia sample host.
4. Build the minimal `TextEditor` / `TextArea` / `TextView` shell on top of the portable core.
