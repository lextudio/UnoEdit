# Unify LeXtudio.UI.Text.Core with Windows.UI.Text.Core

## Background

In `/Users/lextm/new-AvalonEdit/UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs` we still use quite different code paths for WinUI 3 and Uno Skia desktop platforms (Windows/macOS/Linux) to bridge the text input system. The WinUI 3 path uses `Windows.UI.Text.Core` APIs, while the other uses Core Text APIs for Uno Platform.

This is really messy.

## Goal

We should revise Core Text APIs for Uno Platform, so its consuming API surface works the same way as the WinUI 3 path. This will allow us to unify the text input code paths and reduce platform-specific branching in the main codebase.

## Feasibility and approach

This document outlines a practical, low-risk path to unify the text-input surface used by WinUI 3 and Uno Skia hosts.

Important constraint: the `Windows.UI.Text.Core` API surface on Windows is fixed and must not be changed. We will treat it as the reference implementation. The plan below therefore treats the Windows surface as authoritative and adapts our Uno-side implementation to that surface.


Summary: treat `Windows.UI.Text.Core` as the authoritative, fixed API surface on Windows and update `LeXtudio.UI.Text.Core` (the Uno/Core Text library) to implement the same API surface and semantics for the subset used by the product. There will be no adapter layer — we will change the Uno-side library so that `TextView.PlatformInput` can consume `LeXtudio.UI.Text.Core` using the same APIs and behavior as the Windows path. Refactor `TextView.PlatformInput` to call the updated Uno APIs and validate on Windows/macOS/Linux. Packaging and CI changes ensure macOS native assets are built and included.

Key constraints and assumptions:

- The unified API only needs to cover the features used by `TextView.PlatformInput` and related input plumbing (composition lifecycle, text requests/updates, caret notifications, selection/candidate coordination). It does not need to be a full implementation of the entire `Windows.UI.Text.Core` surface.
- Backwards compatibility: existing consumers of `LeXtudio.UI.Text.Core` should continue to work; the new interface and adapters will be additive.
- Native runtime assets (macOS dylib) must be produced on macOS and packaged under `runtimes/{rid}/native/` in the `LeXtudio.UI.Text.Core` NuGet package.

Scope
-----

In scope:

- Design and implement a small compatibility interface inside the `LeXtudio.UI.Text.Core` assembly (e.g., `ITextEditContext` / `ICoreTextEditContextAdapter`).
- Implement two adapters:
	- `WinUiCoreTextAdapter` — thin wrapper around `Windows.UI.Text.Core` APIs (used on WinUI hosts).
	- `CoreTextAdapter` — uses existing Core Text bridge + `libUnoEditMacInput` runtime logic for Uno Skia hosts on macOS/Linux.
- Refactor `TextView.PlatformInput` to take the interface instead of branching on platform at call sites.
- Add automated tests (unit + integration) that verify composition, caret notifications, and text update semantics on each host.
- Update packaging/CI to ensure macOS native assets are built on macOS and included in the nupkg.

Out of scope (for the initial pass):

- Replacing or reimplementing unrelated text-rendering or layout code.
- Making the unified interface a perfect one-to-one mirror of every `Windows.UI.Text.Core` surface — only what `TextView.PlatformInput` requires.

Design and approach
-------------------

1) Windows-parity implementation approach

- Because `Windows.UI.Text.Core` is fixed on Windows, the correct approach is to update `LeXtudio.UI.Text.Core` so that it exposes the same API surface and behavior (for the subset we need). In practice this means:

	- Add the public types, event names, and members expected by `TextView.PlatformInput` (or provide binary-compatible aliases) so the Uno library can be used interchangeably with the Windows surface for that subset.
	- Match semantics: event ordering, argument semantics, commit behavior, caret notification timing, and disposal semantics should mimic Windows where feasible. Document any unavoidable semantic deviations.

- Example surface to match (illustrative subset):

	- Events and shapes used by `TextView.PlatformInput`:
		- `CompositionStarted`, `CompositionCompleted`
		- `TextRequested`, `TextUpdating`

	- Methods expected by hosts:
		- `NotifyCaretRectChanged(double x, double y, double width, double height, double scale)`
		- Candidate/candidate-window control APIs if used by the host
		- `IDisposable` / `Dispose()` behavior for deterministic cleanup

Rationale: implementing Windows-parity directly inside `LeXtudio.UI.Text.Core` avoids runtime adapters and reduces complexity for consumers — `TextView.PlatformInput` can call the same APIs on all hosts and the Uno library is responsible for providing correct behavior on macOS/Linux.



2) Implementation details for `LeXtudio.UI.Text.Core`

- Add or expose public APIs in `LeXtudio.UI.Text.Core` that match the names and shapes used by `Windows.UI.Text.Core` for the product's needs. Prefer source-compatible names so calling code (e.g., `TextView.PlatformInput`) requires minimal changes.

- When binary compatibility is required (e.g., to minimize code changes in many call sites), provide thin compatibility shims that forward to the new APIs with the same public names.

- Ensure event arg types mirror the fields hosts expect. If the Windows API uses types not available cross-platform, expose portable wrappers with the same property names.

- Implement Windows-like semantics in the Uno codepaths by reusing the existing Core Text bridge and native helpers (`libUnoEditMacInput.dylib`) on macOS; emulate behavior on Linux as applicable.


3) Refactor host usage

- Make `TextView.PlatformInput` depend on the `LeXtudio.UI.Text.Core` API surface (the Windows-parity types) and remove platform-specific branching where the unified API covers the case.

- Where source changes are required (e.g., different namespaces), prefer minimal call-site edits: add `using` aliases or thin shims to avoid widespread refactors.

4) Tests and validation

- Unit tests for the adapter translation layer (mapping WinUI/CoreText events to adapter events).
- Integration tests:
	- Windows: exercise composition lifecycle with WinUI 3 host.
	- macOS/Linux: exercise composition lifecycle using the sample app that loads `LeXtudio.UI.Text.Core` from the local `dist/` NuGet feed and verifies the presence of `libUnoEditMacInput.dylib` and runtime behavior.

Packaging & CI
--------------

- Ensure `LeXtudio.UI.Text.Core` packing keeps the existing MSBuild `BuildTextCoreMacInputBridge` and runtime mapping so macOS packaging includes the dylib under `runtimes/osx/native/` (or `runtimes/osx-x64/native`, `runtimes/osx-arm64/native` as you already do).
- Adjust CI to split responsibilities:
	- macOS job: run `dist.all.sh` (or `dotnet pack /p:PackageVersion=...`) to produce unsigned nupkg(s) with native assets.
	- Windows job: sign nupkg(s) using `sign-existing-nupkgs.ps1` and push.
- Add an integration job that restores the sample from the generated `dist/` feed and runs the sample smoke tests.

Step-by-step implementation plan (detailed)
-----------------------------------------

Phase A — Investigation & mapping (1-2 days)

- Inventory: enumerate all APIs that `TextView.PlatformInput` currently calls on both paths. (Search for `CoreText`, `CoreTextEditContext`, `NotifyCaretRectChanged`, `TextRequested`, `TextUpdating`, composition events.)
- Produce a one-page mapping table: WinUI API → Adapter event/method → Core Text implementation notes.

Deliverable: `docs/unification.md` updated with mapping table and a follow-up ticket for uncovered gaps.

Mapping table (WinUI → LeXtudio.UI.Text.Core)
-------------------------------------------

| Windows API (WinUI) | Current `LeXtudio.UI.Text.Core` | Action needed |
|---|---|---|
| `CoreTextServicesManager.GetForCurrentView()` / `CreateEditContext()` | `CoreTextServicesManager.GetForCurrentView()` / `CreateEditContext()` (existing) | No change — present in `external/coretext/src/CoreTextServicesManager.cs` |
| `CoreTextEditContext` events: `TextRequested`, `TextUpdating`, `SelectionRequested`, `SelectionUpdating`, `LayoutRequested`, `CompositionStarted`, `CompositionCompleted`, `FocusRemoved`, `CommandReceived` | `CoreTextEditContext` exposes `TextRequested`, `TextUpdating`, `SelectionRequested`, `SelectionUpdating`, `LayoutRequested`, `CompositionStarted`, `CompositionCompleted`, `FocusRemoved`, `CommandReceived` (see `external/coretext/src/CoreTextEditContext.cs`) | No change — events present; semantics must be kept Windows-like |
| `CoreTextEditContext.Attach(nint hwnd)` | `CoreTextEditContext.Attach(nint hwnd, nint displayHandle = 0)` | No change — present (Linux needs displayHandle) |
| `CoreTextEditContext.NotifyCaretRectChanged(x,y,w,h)` | `CoreTextEditContext.NotifyCaretRectChanged(x,y,w,h, scale=1.0)` | No change — present |
| `CoreTextEditContext.NotifyFocusEnter()` / `NotifyFocusLeave()` | `CoreTextEditContext.NotifyFocusEnter()` / `NotifyFocusLeave()` | No change — present |
| `CoreTextEditContext.NotifyLayoutChanged()` | (missing) | Add `NotifyLayoutChanged()` to `CoreTextEditContext` (or provide a compat method) to match WinUI calls in `TextView.PlatformInput.cs` |
| `CoreTextEditContext.NotifySelectionChanged(CoreTextRange range)` | (missing) — LeXtudio currently uses `CoreTextSelectionRequest` (`Start`/`Length`) | Add `NotifySelectionChanged(CoreTextRange)` and introduce `CoreTextRange` (with `StartCaretPosition`/`EndCaretPosition`) or provide a binary/source-compatible alias to avoid call-site churn |
| `CoreTextTextRequestedEventArgs.Request.Text` (mutable request object) | `CoreTextTextRequestedEventArgs` / `CoreTextTextRequest.Text` — present (`external/coretext/src/CoreTextTextRequest.cs`) | Matches — confirm mutable semantics and deferral behavior |
| `CoreTextTextUpdatingEventArgs.Range` (`StartCaretPosition`/`EndCaretPosition`), `NewSelection` (range), `Text` | `CoreTextTextUpdatingEventArgs` currently exposes only `NewText` (`external/coretext/src/CoreTextTextUpdatingEventArgs.cs`) | Extend `CoreTextTextUpdatingEventArgs` to include `Range` and `NewSelection` (use `CoreTextRange` shape) and a `Text` property name matching WinUI to avoid duplicate per-callsite logic such as `_coreTextOffsetDelta` adjustments.
| `CoreTextSelectionRequestedEventArgs.Request.Selection` (CoreTextRange with StartCaretPosition/EndCaretPosition) | `CoreTextSelectionRequestedEventArgs.Request` uses `CoreTextSelectionRequest { Start, Length }` | Provide `CoreTextRange` (StartCaretPosition/EndCaretPosition) or add convenience properties that map `Start/Length` to `StartCaretPosition/EndCaretPosition` for source compatibility.
| `CoreTextLayoutRequestedEventArgs.Request.LayoutBounds.TextBounds` / `ControlBounds` (Rects) | `CoreTextLayoutRequest` currently has `Width` / `Height` (`external/coretext/src/CoreTextLayoutRequest.cs`) | Replace/extend layout request to include `LayoutBounds` with `TextBounds` and `ControlBounds` (Rects) so `TextView.PlatformInput` can set exact screen rects like on WinUI.
| `CoreTextCommandReceivedEventArgs.Command` + `Handled` | `CoreTextCommandReceivedEventArgs.Command` + `Handled` — present (`external/coretext/src/CoreTextCommandReceivedEventArgs.cs`) | Matches — keep semantics and document platform selector mapping (AppKit selectors → command names)
| `CoreTextDeferral` / async deferral pattern | `CoreTextDeferral` exists (`external/coretext/src/CoreTextDeferral.cs`) | Matches — ensure `IsCompleted` semantics match WinUI deferrals

Notes and priorities:

- High-priority parity gaps: `NotifyLayoutChanged`, `NotifySelectionChanged`, `CoreTextRange` shape, and `CoreTextTextUpdatingEventArgs` (add Range/NewSelection/Text). These are required to let `TextView.PlatformInput` use identical call sites on Windows and Uno without per-branch corrections (e.g., `_coreTextOffsetDelta`).
- Medium-priority: extend `CoreTextLayoutRequest` to carry `LayoutBounds` with `TextBounds` and `ControlBounds` (Rects) so caret/layout code need not differ by platform.
- Low-effort compatibility helpers: provide property aliases or thin DTO shims (`StartCaretPosition`/`EndCaretPosition`) that map to existing `Start`/`Length` to minimize consumer diffs during early rollout.

Next action for Phase A deliverable
----------------------------------

- I will now scan `TextView.PlatformInput.cs` (already done) and the `external/coretext/src` sources (done) to produce a minimal patch list for `LeXtudio.UI.Text.Core` to add the missing members. After that I can implement the API additions (small PR) or prepare a compatibility-shim patch depending on your preference.


Phase B — Interface & adapter scaffolding (2-4 days)

- Add `ITextEditContextAdapter` to `LeXtudio.UI.Text.Core` with portable event args.
- Implement `WinUiCoreTextAdapter` and `CoreTextAdapter` skeletons that compile and forward basic events.
- Add unit tests asserting event translation and basic lifecycle.

Deliverable: PR with adapter interfaces and tests.

Phase C — Refactor host usage (2-3 days)

- Modify `TextView.PlatformInput` to accept the adapter via constructor/DI.
- Replace platform-specific branches with adapter calls. Keep changes minimal and well-covered by tests.

Deliverable: PR updating `TextView.PlatformInput`, with tests and a compatibility shim so existing behavior remains identical.

Phase D — Integration & platform verification (3-5 days)

- Build and pack `LeXtudio.UI.Text.Core` on macOS with native runtime assets; produce `dist/LeXtudio.UI.Text.Core.<version>.nupkg`.
- Restore the sample from `dist/` and run smoke/integration tests on macOS (verify candidate window placement, composition completion, and text commit flows).
- Run corresponding tests on Windows using the `WinUiCoreTextAdapter`.

Deliverable: test reports and validated sample runs on Windows and macOS.

Phase E — CI, packaging, docs, and merge (2-4 days)

- Add CI jobs to build/pack on macOS, run unit/integration tests on each platform, and sign/publish the nupkg from Windows.
- Finalize migration documentation and update `docs/unification.md` with spot-tested examples and troubleshooting notes.

Deliverable: CI pipeline changes, updated docs, and merged PRs.

Timeline (rough)
----------------

- End-to-end estimated: 10–16 working days across phases, parallelizing investigation, adapter work, and CI changes where possible.

Risks & mitigations
-------------------

- Risk: API mismatch — certain WinUI APIs may not have exact equivalents on Core Text. Mitigation: implement best-effort mapping, document unsupported features, and fall back to a conservative behavior.
- Risk: Native runtime packaging issues on CI. Mitigation: ensure macOS CI job exists that runs `dist.all.sh` and verify produced nupkg contains `runtimes/*/native/libUnoEditMacInput.dylib` before pushing.
- Risk: Regressions in IME behavior. Mitigation: add integration tests that exercise composition/candidate flows and validate against known-good behavior on each platform.

Acceptance criteria
-------------------

- `TextView.PlatformInput` consumes the new adapter interface and no longer contains dense platform-specific branching for composition/IME handling.
- Both `WinUiCoreTextAdapter` and `CoreTextAdapter` are implemented, exercised by tests, and behave equivalently for the adapter surface.
- The `LeXtudio.UI.Text.Core` package produced on macOS contains the native `libUnoEditMacInput.dylib` under `runtimes/*/native/` and the sample can restore from a local `dist/` feed and run the integration smoke tests.

Next steps (what I can do now)
------------------------------

- Produce the mapping table by scanning `TextView.PlatformInput` and related files for the exact set of required APIs and events (I can run that scan and add the table to this doc).
- Create the initial `ITextEditContextAdapter` interface skeleton in `LeXtudio.UI.Text.Core` and a small unit test harness.

If you want, I can start with the mapping/inventory scan now and then open a PR with the interface skeleton. Which do you prefer me to do first: generate the mapping table, or create the adapter skeleton and tests?

