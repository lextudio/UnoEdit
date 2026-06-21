# Default font design

This document describes the default font choices and runtime behavior used by UnoEdit and the UnoPropertyGrid.

## Editor default

- Default family: **`Cascadia Code`** (an open-source monospaced code font).

Rationale: a single open-source monospaced family gives consistent glyph advances and caret
metrics across platforms. By the bare family name it resolves from the system when installed;
apps that want deterministic rendering everywhere can bundle the `CascadiaCode-Regular.ttf` asset
and override `TextEditor.EditorFontFamily` with an `ms-appx:///…#Cascadia Code` reference
(the UnoEdit sample vendors the font under `Assets/Fonts/`).

The editor default is produced by `EditorTextMetrics.CreateFontFamily()` in `src/UnoEdit/Rendering/EditorTextMetrics.cs`.

## Line height

Row height is **derived from the editor font's real metrics** (via `CanvasLinePainter.GetLineHeight`,
which measures through `CanvasTextLayout`), multiplied by a small leading factor — not a hardcoded
constant. It is recomputed in `TextView.UpdateTextMetrics()` whenever the font or size changes, so
rows, caret, and selection track the font.

## PropertyGrid / FontFamily property behavior

- At runtime the `Font Family` editor attempts to enumerate system-installed fonts rather than using a static list.
- Enumeration order (attempted, in order):
  1. SkiaSharp `SKFontManager` (`GetFontFamilies()` or `FontFamilies`) via reflection (cross-platform)
  2. `System.Drawing.Text.InstalledFontCollection` via reflection (when available)
  3. A small baked-in fallback list with monospaced fonts prioritized

- The UI renders each ComboBox item using the font it represents so users see a live preview of the font.
- The ViewModel exposes font values as strings; when applying changes the descriptor converts the selected name into a `FontFamily` instance.

## Fallback / priority list

If enumeration is not available the control falls back to a prioritized list that favors monospaced fonts first, then common UI fonts:

`Consolas`, `Menlo`, `DejaVu Sans Mono`, `Liberation Mono`, `Monospace`, `Courier New`, `Courier`, `Segoe UI`, `Arial`, `Calibri`, `Georgia`, `Tahoma`, `Verdana`, `Times New Roman`

## Changing the defaults

- To change the editor default, update `EditorTextMetrics.CreateFontFamily()`.
- To change the built-in fallback list or enumeration behavior, edit `PropertyEditorControl.LoadSystemFontFamilies()`.

---
This file is intended as a short reference for maintainers and designers. If you want platform-specific alternatives added, tell me which platforms and fonts to document.
