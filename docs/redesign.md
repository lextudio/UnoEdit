# UnoEdit Text View Redesign — Skia/Uno2D Glyph Rendering

## Why redesign

UnoEdit's text view today cannot match ILSpy/AvalonEdit because it does not render
text the way AvalonEdit does. The WPF AvalonEdit `TextView` formats each line with
`System.Windows.Media.TextFormatting.TextFormatter`, obtaining exact glyph positions,
baseline, and font-derived line height, then paints onto a WPF `DrawingContext`
(`DrawGlyphRun`). All lines share one coordinate space.

Uno/WinUI exposes no `TextFormatter`. The current implementation substitutes a
virtualized `ItemsControl` (`LinesItemsControl`) where each visible row is its own
XAML control (`HighlightedTextBlock` → WinUI `TextBlock`, or the `RichTextBlock`
shim on Uno desktop) built from colored `<Run>`s. Glyph shaping/positioning is
delegated to the platform text stack, **not** to AvalonEdit.

### Concrete defects this causes

1. **Geometry vs. rendered glyphs disagree.** Caret/selection/highlight positions are
   computed from approximations that do not match where glyphs actually land:
   - `TextView.AvalonEditPipeline.cs:127` uses `charWidth = EditorFontSize * 0.6`
     (a monospace guess).
   - `TextView.xaml.uno.cs` `MeasureCharacterWidth` averages `'0' x 32`;
     `MeasureDisplayTextWidth` measures per-prefix via a probe `TextBlock`.
   These three sources disagree with each other and with the row's own shaping.
2. **Hardcoded line height.** `TextView.xaml.uno.cs:214` `const double LineHeight = 22d`
   regardless of font; AvalonEdit derives line height from font metrics.
3. **Proportional default font.** `EditorTextMetrics.cs:11` defaults to `Open Sans`
   (variable-width), not a monospace code font.
4. **Per-row layout jitter.** Each row is centered (`VerticalAlignment=Center`) in a
   fixed 22px box and rounded independently; no shared baseline grid.
5. **Pipeline mostly inert.** `BackgroundRenderers`/`LineTransformers`/`ElementGenerators`
   added via the AvalonEdit-parity surface are not consumed by the Skia draw path
   (admitted in `TextView.AvalonEditPipeline.cs:9-14`). ILSpy relies on these.

## Target architecture

Run the **real** AvalonEdit visual-line pipeline and draw glyph runs onto a Skia
surface, instead of approximating with one TextBlock per line.

```
TextDocument
   ↓  (real AvalonEdit pipeline — already partly ported)
VisualLine / VisualLineElement / TextLine          (VisualLine.uno.cs, FormattedTextTypes.cs)
   ↓  shape + measure  ← THE MISSING PRIMITIVE
Glyph runs: positions, advances, baseline, hit-testing   (HarfBuzz/Skia, via Uno2D)
   ↓  draw
DrawingContext-shaped adapter  →  CanvasDrawingSession (Uno2D)  →  SKCanvas
   ↓
CanvasControl (one Skia surface, virtualized by visible-line window)
```

### Terminology: there is no `DrawingContext` on Uno

| Abstraction | Origin | Call shape | Backend |
|---|---|---|---|
| `DrawingContext` | WPF | `OnRender(dc)` → `dc.DrawGlyphRun` | DirectX |
| `CanvasDrawingSession` | Win2D (= Uno2D) | `e.DrawingSession.DrawText(...)` | Direct2D; Uno2D → SkiaSharp |
| `SKCanvas` | SkiaSharp | `canvas.DrawTextBlob(...)` | Skia |

The plan is **not** to "port `DrawingContext`" — it does not exist on Uno. We provide a
`DrawingContext`-shaped adapter (`DrawGlyphRun`/`DrawRectangle`/`DrawLine`/`PushClip`)
so ported AvalonEdit render code compiles unchanged, backed by `CanvasDrawingSession`.

### Why Uno2D is the right backend

- AvalonEdit's renderer issues immediate-mode draw calls; Uno2D's `CanvasDrawingSession`
  maps almost 1:1 (`DrawText/DrawRectangle/DrawLine/DrawGeometry`).
- Uno2D is already SkiaSharp-backed and cross-platform (Win/mac/Linux + Uno desktop).
- It already ships a `CanvasControl` (a redrawable Skia surface) to replace the
  TextBlock-per-line `ItemsControl`.

## The shared foundation: a real text-layout primitive

The single missing piece — needed by **both** UnoEdit and UnoRichText's Florence engine —
is a glyph-level layout primitive on HarfBuzz/Skia. Today `Uno2D`'s
`CanvasTextLayout` (`Win2D.UnoCompat/Canvas/Text/CanvasTextFormat.cs:101`) is a stub:
only `CreatePath()` and whole-string `SKFont.MeasureText`.

It must be extended to expose:

- per-glyph / per-cluster **advance widths** and positions,
- **baseline** and font-metric **line height**,
- **hit-testing** both ways: `HitTest(x) → caret index` and `GetCaretPosition(index) → x`,
- a shaping **cache** keyed by (text, font, size).

### Relationship to Florence (UnoRichText)

Florence (`MS.Internal.Florence`, in the WPF shim) replaces WPF's **PTS** — the
FlowDocument flow/pagination engine. PTS is a *different* engine from `TextFormatter`:

| Engine | Job | Consumer |
|---|---|---|
| PTS (Florence replaces this) | flow/paginate rich documents, nested inlines | RichTextBox/FlowDocument |
| TextFormatter (UnoEdit needs this) | line-level glyph layout | AvalonEdit/code editor |

AvalonEdit deliberately avoids PTS in WPF. So **Florence itself is not reusable in
UnoEdit** (wrong layer — flow vs. line-grid). But Florence has the *same* alignment bug
(its `run.X/run.Width` overlay geometry disagrees with native TextBlock shaping —
`docs/RichTextBox/session42.md:112-113,177-178`), which confirms both engines lack the
same primitive. **Build the Skia shaping/hit-testing primitive once; Florence and
UnoEdit both consume it.**

## What parity is achievable

| Goal | Achievable | Note |
|---|---|---|
| Exact caret/selection/highlight alignment | Yes | We own glyph metrics; removes the `*0.6` / probe mismatch |
| Full AvalonEdit pipeline (markers, brackets, ref underlines, rulers) | Yes | Real VisualLine pipeline instead of TextBlock approximation |
| Pixel-identical to ILSpy/WPF | No | WPF rasterizes via DirectWrite; Skia uses its own rasterizer + HarfBuzz. Avalonia ILSpy isn't pixel-identical to WPF either. |

Target: "looks like a proper code editor, geometry is exact, pipeline is complete" —
not pixel-equality, which is neither feasible nor necessary.

## Existing scaffolding to build on

- `Rendering/FormattedTextTypes.cs` — reduced `TextLine` / `FormattedTextElement` model.
- `Rendering/VisualLine.uno.cs` — `ConstructVisualElements` already ported (one
  `UnoTextLine` per `VisualLine`, no word-wrap).
- `Rendering/VisualLineElementTextRunProperties.cs` — run properties.

The codebase currently runs *half* this pipeline and *half* the TextBlock-per-line
approximation; the two parallel paths are a source of the inconsistency.

## Proposed sequencing

1. **Foundation:** extend Uno2D `CanvasTextLayout` to a real shaping/measure/hit-test
   layer (glyph advances, baseline, `HitTest`/`GetCaretPosition`, shaping cache).
   Independently testable; benefits Florence too.
2. **Adapter:** implement a `DrawingContext`-shaped wrapper over `CanvasDrawingSession`
   (`DrawGlyphRun`/`DrawRectangle`/`DrawLine`/`PushClip`).
3. **Wire pipeline:** feed the real `VisualLine`/`TextLine` path with metrics from (1);
   render via (2).
4. **Surface swap:** replace `LinesItemsControl` (TextBlock-per-line) with a
   `CanvasControl` draw surface virtualized over the visible-line window.
5. **Remove approximations:** delete `EditorFontSize * 0.6`, probe-based char width, and
   hardcoded `LineHeight`; default font → monospace; line height → font-derived.
6. **Consume the pipeline:** make `BackgroundRenderers`/`LineTransformers`/
   `ElementGenerators` actually drive the draw path (bracket match, markers, ref
   underlines).

## Dual-target backend decision (Uno desktop vs WinUI 3)

Shared editor code is written **once** against the Win2D API surface
(`Microsoft.Graphics.Canvas.*` — `CanvasControl`, `CanvasDrawingSession`,
`CanvasTextLayout`). The concrete backend is selected per target framework:

| Target | Backend | Text stack |
|---|---|---|
| `net10.0-desktop` (Uno) | Uno2D (`LeXtudio.Win2D.UnoCompat`) | SkiaSharp + (HarfBuzz) |
| `net10.0-windows10.0.19041.0` (WinUI 3) | **real Win2D** (`Microsoft.Graphics.Win2D`) | native DirectWrite |

Rationale (decided): on Windows, real Win2D gives native, correct DirectWrite metrics
and hit-testing for free. The two targets therefore use two rasterizers and will not be
pixel-identical to each other — accepted. The cost this imposes: **Uno2D's
`CanvasTextLayout` must mirror the real Win2D `CanvasTextLayout` API exactly**
(signatures + types) so the shared source compiles unchanged on both. Specifically the
members the editor relies on:

- `Rect LayoutBounds { get; }`, `Rect DrawBounds { get; }`
- `System.Numerics.Vector2 GetCaretPosition(int characterIndex, bool trailingSideOfCharacter)`
- `CanvasTextLayoutRegion HitTest(float x, float y)`
- `CanvasTextLayoutRegion[] GetCharacterRegions(int characterIndex, int characterCount)`
- `CanvasLineMetrics[] LineMetrics { get; }`

### Where shared code lives

Both UnoEdit and UnoRichText already reference **WindowsShims** (`LeXtudio.Windows`,
itself dual-targeted `net10.0-desktop` + `net10.0-windows10.0.19041.0`), and Florence
already lives there (`MS.Internal/Florence/FlorenceEngine.cs`). So:

- The **shared text-layout abstraction + DrawingContext adapter** go into
  `LeXtudio.Windows` (WindowsShims).
- WindowsShims gains a conditional backend reference: `LeXtudio.Win2D.UnoCompat` on
  desktop, `Microsoft.Graphics.Win2D` on the WinUI 3 target.
- The low-level Skia shaping/measure/hit-test lives in **Uno2D** (extending
  `CanvasTextLayout`); real Win2D supplies the equivalent natively on WinUI 3.

## Risks / scope

Large effort: shaping cache, tab expansion, hit-testing, folding/collapsing,
virtualization, redraw invalidation, IME caret-rect reporting. The cheaper partial win
(monospace font + font-derived line height + unified measured geometry, keeping the
TextBlock approach) gets ~80% of the *visual* improvement without the rewrite, and is a
valid fallback if the full Skia path is deferred.
