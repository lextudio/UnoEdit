# TextMate Integration: Failure Analysis and Recovery Plan

## Status

The current integration is not reliable for large or stateful documents.

The most visible symptom is that the first part of a document is highlighted while
later lines remain unhighlighted, sometimes permanently. This is not merely a slow
paint or a missing redraw. UnoEdit currently mixes two tokenization strategies in a
way that violates the ordering and ownership assumptions of TextMateSharp 2.0.3.

The integration should be treated as correctness work, not as a timeout-tuning or
rendering optimization problem.

## Sources Reviewed

- UnoEdit:
  - `src/UnoEdit.TextMate/TextMateLineHighlighter.cs`
  - `src/UnoEdit.TextMate/TextDocumentLineList.cs`
  - `src/UnoEdit/Rendering/TextView.xaml.uno.cs`
  - TextMate-related tests and the relevant Git history
- AvaloniaEdit:
  - `AvaloniaEdit.TextMate/TextEditorModel.cs`
  - `AvaloniaEdit.TextMate/TextMateColoringTransformer.cs`
- TextMateSharp:
  - version 2.0.3, commit
    [`4532112`](https://github.com/danipen/TextMateSharp/tree/4532112f6ee96c8d7847ee66a79719dfe58e9f43)
  - version 2.0.4, commit
    [`622d1b2`](https://github.com/danipen/TextMateSharp/tree/622d1b240a5be474939857c0b2780249022f14a3)

## How TextMate State Actually Works

TextMate grammars are line-oriented but not line-independent. A line's end state
feeds the next line's start state. Examples include:

- block comments
- multiline strings
- embedded languages
- preprocessor and region-like constructs

For a document with lines `0..N`, correct tokens for line `N` may require valid
states for every preceding line back to the nearest known-good checkpoint.

`TMModel` represents this with a token list and state on each `ModelLine`. Its
background worker normally starts at an invalid line and propagates state forward.
It deliberately limits a background slice to about 5 ms and requeues the next line,
so a large document is expected to become ready over multiple callbacks.

`ModelTokensChangedEvent.Range` uses **1-based inclusive line numbers**. UnoEdit's
public invalidation event also uses 1-based inclusive line numbers, so no conversion
is required at that boundary.

## Current UnoEdit Pipeline

The current flow is:

1. `TextView` publishes a visible line range.
2. `TextDocumentLineList` calls `TMModel.ForceTokenization(start, end)` for that
   viewport.
3. Rendering calls `TextMateLineHighlighter.HighlightLine()` for individual lines.
4. If tokens are `null`, `HighlightLine()`:
   - invalidates that line again;
   - calls `WarmLineRange()` for that line;
   - reads the tokens again.
5. `TMModel` also continues its own background tokenization.
6. `ModelTokensChanged()` intersects changed lines with the current viewport and
   queues a partial repaint.

This looks like proactive asynchronous highlighting, but several details make the
pipeline unstable.

## Root Causes

### 1. TextMateSharp 2.0.3 permits concurrent tokenization

UnoEdit currently references TextMateSharp 2.0.3. In that version:

- the background worker tokenizes lines;
- `ForceTokenization()` may tokenize synchronously on the caller;
- the tokenization/state update path is not protected by one common lock.

UnoEdit can therefore run viewport tokenization on the UI thread while
TextMateSharp's worker is updating the same tokenizer and `ModelLine` state.

TextMateSharp 2.0.4 contains a large thread-safety rewrite, including serialized
model/tokenization operations. Upgrading is necessary. It is not sufficient on its
own, because the remaining UnoEdit scheduling problems below still exist.

### 2. UnoEdit force-tokenizes from an unsafe starting line

`WarmLineRange(firstVisible, lastVisible)` starts directly at the viewport. The
first visible line may not yet have a valid incoming grammar state.

Forcing lines 500-540 before state propagation has reached line 500 can produce
tokens based on a missing or stale state. A viewport request must instead start at:

- the earliest invalid predecessor; or
- a preceding line whose end state is known to be valid.

The public `TMModel` API does not expose enough checkpoint/state validity
information to implement that robustly from UnoEdit.

### 3. Reading a pending line mutates and floods the work queue

`HighlightLine()` currently calls `model.InvalidateLine()` whenever
`GetLineTokens()` returns `null`.

A read operation must not create more invalidation work. During a repaint, every
pending visible line can be enqueued again. Repeated refreshes add more duplicates,
which can delay the sequential worker that would otherwise advance from the start
of the document. This explains the characteristic "the first few lines work, the
large remainder never catches up" failure.

Pending tokens are a normal state and should simply return `Pending`; they are not
evidence that the line needs to be invalidated again.

### 4. Viewport-range deduplication records a request as completion

`TextDocumentLineList` stores `lastTokenizedViewportStartLine` and
`lastTokenizedViewportEndLine` before calling `ForceTokenization()`.

Later requests for the same viewport are skipped even when:

- tokenization has not completed;
- the document was edited;
- the grammar or theme changed;
- the previous call raced with background work;
- one or more lines still return `null`.

This is the same race exposed by
`HighlightLine_ReturnsSections_ForCSharpKeywords` in CI: the two-second polling loop
appears to retry, but its warm requests can be suppressed as duplicates.

Request deduplication is valid only while an identical request is already queued.
It must be cleared after execution and must never be used as a readiness cache.

### 5. Readiness is inferred from token presence only

`IsVisibleLineRangeReady()` currently checks only whether `GetLineTokens()` is
non-null. That does not prove that the token list was produced from the correct
incoming state or from the current document/grammar generation.

A robust readiness result needs a generation and a contiguous state-valid frontier,
not just a non-null token reference.

### 6. The repaint loop can continuously reschedule itself

While the initial visible range is not ready, `TextView` queues another low-priority
refresh. That refresh may find the same state and queue another refresh again.

This polling loop:

- consumes UI work while no state has changed;
- can amplify invalidation queue flooding through `HighlightLine()`;
- makes test success depend on timing;
- obscures missing token-change notifications.

Repainting should be event-driven. A pending viewport should repaint only when:

- the model reports progress affecting that viewport;
- the viewport changes;
- the document/grammar generation changes; or
- a bounded watchdog detects a lost notification.

### 7. Exceptions are hidden

Both rendering and parts of TextMateSharp 2.0.3 catch exceptions and either ignore
them or write only to debug output. A tokenizer failure can therefore look exactly
like a permanently pending line.

The supplied `exceptionHandler` must receive tokenization, dispatcher, and paint
conversion failures. Diagnostics should include document generation, requested
range, valid frontier, pending queue size, and elapsed time.

## Why Copying AvaloniaEdit Was Not Enough

AvaloniaEdit provides the correct high-level pattern:

- keep tokenization asynchronous;
- warm the viewport;
- redraw changed visible ranges on the UI thread;
- tolerate temporarily missing tokens.

It does not by itself guarantee that every arbitrary viewport is a safe
tokenization starting point. UnoEdit also has a different rendering lifecycle,
cache, deferred rebuild path, and explicit readiness protocol. Copying only the
viewport and redraw portions left two competing schedulers in place.

The useful AvaloniaEdit principles should remain, but UnoEdit needs one owner for
tokenization state and an explicit progress model.

## Recommended Complete Solution

### Implemented stabilization

The first stabilization pass is now implemented:

- `TextMateSharp` and `TextMateSharp.Grammars` are upgraded to 2.0.4.
- `HighlightLine()` treats missing tokens as an observational pending result and no
  longer invalidates or force-tokenizes the line.
- UnoEdit no longer force-tokenizes arbitrary viewport ranges.
- persistent viewport-request deduplication is removed.
- the initial view is rendered immediately with pending lines unstyled; token
  progress events repaint completed visible ranges.
- the low-priority refresh polling loop is removed.
- `TextMateLineHighlighter` now advertises its range-invalidation capability, and
  `TextView` subscribes before attaching the document so initial token events cannot
  be lost.
- full viewport rebuilds preserve the invalidation flag until fresh line view
  models have been constructed, preventing reuse of the pre-TextMate uncolored
  cache.
- a regression test jumps directly to line 2,000 of a stateful C# document and
  repeatedly reads the pending line before verifying eventual correct highlighting.
- a mounted Uno Runtime integration test switches the same editor/document from
  XSHD to TextMate and verifies colored visible runs appear without scrolling.

The coordinator and explicit generation/checkpoint model described below remain the
recommended second phase if UnoEdit needs prioritized far-viewport tokenization or
strong stale-result guarantees beyond TextMateSharp 2.0.4.

### Phase 1: Stabilize the existing integration

1. Upgrade both `TextMateSharp` and `TextMateSharp.Grammars` from 2.0.3 to at least
   2.0.4.
2. Remove `model.InvalidateLine()` from `HighlightLine()`.
3. Remove persistent `lastTokenizedViewport*` deduplication. Coalesce only queued,
   not-yet-executed requests.
4. Stop self-polling `QueueHighlightedRangeRefresh()` while tokens are pending.
5. Repaint only in response to `ModelTokensChanged`, viewport changes, or generation
   changes.
6. Route all caught exceptions to the configured exception handler and structured
   diagnostics.

This should eliminate races, queue flooding, CI flakiness, and most permanently
unhighlighted regions. It is the minimum acceptable fix.

### Phase 2: Introduce a single tokenization coordinator

For deterministic correctness, UnoEdit should stop allowing the renderer,
viewport events, and `TMModel` background worker to independently schedule work.

Add a coordinator with these properties:

- one serialized tokenization lane;
- a monotonically increasing document/grammar generation;
- per-line status: `Unknown`, `Pending`, `Ready`, or `Failed`;
- a contiguous valid-state frontier;
- coalesced viewport demand;
- cancellation/obsolescence of work from older generations;
- progress events carrying 1-based inclusive changed ranges.

When the viewport requests lines `Vstart..Vend`, the coordinator should:

1. Find the nearest valid checkpoint at or before `Vstart`.
2. Tokenize forward from that checkpoint through `Vend`.
3. Commit tokens only if the generation still matches.
4. Advance the valid frontier.
5. publish exactly the ranges whose committed tokens changed.

Document edits invalidate the changed line, normally the preceding line, and every
downstream state that depends on it. A theme change does not retokenize; it only
invalidates styled-line caches. A grammar change creates a new tokenization
generation starting at line 0.

### Phase 3: Own the state cache if `TMModel` cannot expose checkpoints

TextMateSharp's public `TMModel` API does not expose enough information to ask for
"the nearest valid predecessor" or to attach generation metadata to tokens.

There are two viable choices:

- contribute the required state/checkpoint API upstream and consume it from
  UnoEdit; or
- implement a small UnoEdit tokenization model over TextMateSharp's grammar/tokenizer
  APIs, with UnoEdit owning line states, generations, scheduling, and events.

The second option is more work but gives UnoEdit deterministic behavior and
testability. If highlighting correctness is a release requirement, it is the most
complete solution.

## Required Behavioral Contract

The renderer must see three distinct results:

- `Ready(highlighted sections)`: tokens and style conversion are current.
- `Ready(no styled sections)`: tokenization completed, but the theme produces no
  styled section for the line.
- `Pending`: tokenization for the current generation is incomplete.

`null` must not ambiguously mean all three states.

While a line is pending, UnoEdit may retain a complete result from the same document
generation to avoid flashing. It must not reuse results across text, grammar, or
document generations. Theme changes may reuse tokens but must rebuild styled
sections.

## Test Plan

### Deterministic unit tests

- A 5,000-line C# document eventually reaches `Ready` for the last line.
- Requesting a viewport near the end before initial tokenization reaches it still
  produces the same tokens as sequential tokenization from line 0.
- A multiline comment/string beginning above the viewport affects visible lines
  correctly.
- Repeated reads of a pending line do not enqueue or invalidate work.
- Repeated requests for the same viewport are coalesced while queued but can run
  again after an edit.
- Editing line 10 invalidates and recomputes downstream state.
- Grammar changes discard callbacks and tokens from the old generation.
- Theme changes preserve tokens and rebuild only styled results.
- Empty-but-complete highlighting is distinguishable from pending highlighting.

Use controllable scheduler/barrier fakes instead of `Thread.Sleep` and two-second
polling loops.

### Stress tests

- Tokenization, scrolling, editing, grammar switching, and disposal overlap.
- Rapid scrolling alternates between the beginning and end of a large document.
- The same test runs hundreds of times with randomized scheduler yields.
- No stale callback mutates a disposed or newer-generation model.
- No line remains pending after the coordinator reports idle.

### Integration tests

- The sample opens the large `docs/textmate.md` file and verifies highlighted
  sections near the beginning, middle, and end.
- Scrolling directly to the end before background completion eventually repaints
  that viewport without further user input.
- A changed range repaints only intersecting visible rows.
- Diagnostics contain no swallowed tokenizer exceptions.

## Acceptance Criteria

The issue is fixed only when all of the following hold:

- every line in a finite unchanged document eventually becomes ready;
- visible lines are tokenized from a valid predecessor state;
- no pending read creates invalidation work;
- tokenization has a single serialized owner;
- old-generation results cannot be committed;
- repainting is progress-driven rather than a continuous polling loop;
- large-document and multiline-state tests pass deterministically on Windows,
  macOS, and Linux.

Increasing timeouts, forcing each visible line independently, caching `null`, or
adding more redraw calls do not satisfy these criteria.
