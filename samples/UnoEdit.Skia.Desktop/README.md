# UnoEdit.Skia.Desktop

Desktop-only Uno Skia host for the UnoEdit port.

Current purpose:

- verify the portable `UnoEdit` core can be referenced from a real Uno desktop app
- provide the first integration point for `TextDocument`
- serve as the place where the future `TextEditor` / `TextArea` / `TextView` shell will land

Current status:

- generated from the official Uno single-project template
- limited to `net10.0-desktop`
- references `src/UnoEdit`
- shows a live `TextDocument` sample and basic document statistics

Planned next steps:

1. replace the placeholder viewer with a minimal custom editor surface
2. add viewport layout and scrolling
3. add caret and selection rendering
4. add keyboard and pointer hit testing
