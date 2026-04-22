# TextMate Integration Design

## Goal

Make UnoEdit's TextMate integration behave like AvaloniaEdit's integration:

- accept that the TextMate engine is asynchronous
- avoid treating that as a full highlighter failure
- pretokenize and redraw around the viewport instead of trying to make the whole pipeline synchronous
- marshal redraw work back to the UI thread
- redraw only visible changed ranges instead of rebuilding everything

Reference implementation reviewed:

- `/Users/lextm/New-ILSpy/ProjectRover/thirdparty/AvaloniaEdit/src/AvaloniaEdit.TextMate/TextMate.cs`
- `/Users/lextm/New-ILSpy/ProjectRover/thirdparty/AvaloniaEdit/src/AvaloniaEdit.TextMate/TextEditorModel.cs`
- `/Users/lextm/New-ILSpy/ProjectRover/thirdparty/AvaloniaEdit/src/AvaloniaEdit.TextMate/TextMateColoringTransformer.cs`

## What AvaloniaEdit Does

### 1. Keeps TextMate asynchronous

AvaloniaEdit does not try to make TextMate act like the classic synchronous `DocumentHighlighter`.

Instead:

- the editor model invalidates line ranges incrementally
- viewport lines are tokenized opportunistically
- token-change notifications trigger redraw only when needed

That means the async nature of `TMModel` is part of the design, not something hidden behind a fake synchronous abstraction.

### 2. Tokenizes the viewport proactively

`TextEditorModel` listens to `TextView.ScrollOffsetChanged` and calls `ForceTokenization()` for the visible document-line range.

This is important because it reduces the chance that the renderer asks for a visible line before TextMate has finished building tokens for it.

### 3. Tracks visible lines explicitly

`TextMateColoringTransformer` listens to `TextView.VisualLinesChanged` and stores:

- first visible line index
- last visible line index
- whether visual lines are valid

That visible-range state is then used to filter token-change work.

### 4. Redraws only visible changed ranges

When `ModelTokensChanged()` fires, AvaloniaEdit:

- inspects the changed line ranges from `ModelTokensChangedEvent`
- ignores the notification if the changed lines are completely outside the visible range
- posts a redraw to the UI thread
- redraws only the intersected visible line region

This is the core async-compatibility behavior. The TextMate engine may finish whenever it wants, but the editor only repaints the part of the viewport that is actually affected.

### 5. Tolerates missing tokens without escalating them into a hard failure

In AvaloniaEdit's transformer path:

- `GetLineTokens()` returning `null` simply means "do not apply TextMate transforms for this pass"
- that is different from telling the editor that the whole line highlighter failed

This is important because it avoids turning temporary token unavailability into a visually stronger failure signal than necessary.

## Design Decision For UnoEdit

We will stop pursuing the temporary UnoEdit-specific experiment that tried to smooth the async behavior by:

- eager visible-range warmup on highlighter attachment
- opt-in deferred invalidation via `UNOEDIT_DEFER_HIGHLIGHT_INVALIDATION`

That experiment was useful for learning, but it is not the design we want to keep.

Instead, UnoEdit will adopt AvaloniaEdit's structure directly.

## Planned UnoEdit Direction

### 1. Move toward viewport-driven tokenization

UnoEdit should tokenize visible lines proactively when the viewport changes, not only when the highlighter is attached.

Equivalent AvaloniaEdit concept:

- `TextEditorModel.TokenizeViewPort()`

### 2. Use changed ranges from TextMate to drive redraw

UnoEdit should stop treating every `HighlightingInvalidated` event as a generic full-rebuild trigger.

Instead, the TextMate path should:

- observe `ModelTokensChangedEvent.Ranges`
- intersect those ranges with visible lines
- redraw only affected visible lines

### 3. Marshal redraw work to the UI thread

AvaloniaEdit explicitly posts token-change redraw work back to the UI thread.

UnoEdit should do the same as part of the default design, not as an environment-variable fallback.

### 4. Distinguish temporary token unavailability from real highlighting failure

UnoEdit currently collapses several cases into `null`:

- no tokens yet
- empty token list
- tokens present but no styled sections

The AvaloniaEdit approach suggests these should not all be treated the same way.

## What We Removed

The previous UnoEdit-specific async-integration attempt has been removed:

- `IHighlightedLineSourceWarmup`
- `WarmupLineRange()` in `TextMateLineHighlighter`
- `WarmHighlightedLineSourceVisibleRange()` in `TextView`
- `UNOEDIT_DEFER_HIGHLIGHT_INVALIDATION`
- queued/coalesced deferred invalidation logic in `TextView`

This leaves the codebase ready for a cleaner reimplementation that follows AvaloniaEdit more directly.

## Next Implementation Steps

1. Add viewport tokenization to the UnoEdit TextMate model layer.
2. Preserve visible-line bounds in the rendering/highlighting integration layer.
3. Change TextMate invalidation to carry changed ranges rather than a generic full invalidation.
4. Post redraw work onto the UI thread and redraw only visible affected ranges.
5. Revisit how UnoEdit represents "no tokens yet" vs "no styled sections".
