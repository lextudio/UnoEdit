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
- includes the first minimal `TextEditor` / `TextArea` / `TextView` shell
- renders a live `TextDocument` sample with line numbers and basic document statistics

Planned next steps:

1. add viewport-aware line virtualization instead of rebuilding the whole visible document
2. add caret and selection rendering
3. add keyboard navigation and pointer hit testing
4. move editor controls from sample-host code into reusable library code
