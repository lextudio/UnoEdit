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

| Windows API (WinUI) | Current `LeXtudio.UI.Text.Core` | Status / Notes |
|---|---:|---|
| `CoreTextServicesManager.GetForCurrentView()` / `CreateEditContext()` | `CoreTextServicesManager.GetForCurrentView()` / `CreateEditContext()` | Implemented — see `external/coretext/src/CoreTextServicesManager.cs` |
| `CoreTextEditContext` events: `TextRequested`, `TextUpdating`, `SelectionRequested`, `SelectionUpdating`, `LayoutRequested`, `CompositionStarted`, `CompositionCompleted`, `FocusRemoved`, `CommandReceived` | `CoreTextEditContext` (event surface) | Implemented — see `external/coretext/src/CoreTextEditContext.cs` |
| `CoreTextEditContext.Attach(nint hwnd)` | `CoreTextEditContext.Attach(nint hwnd, nint displayHandle = 0)` | Implemented (Linux requires `displayHandle`) |
| `CoreTextEditContext.NotifyCaretRectChanged(x,y,w,h)` | `CoreTextEditContext.NotifyCaretRectChanged(x,y,w,h, scale=1.0)` | Implemented — used by `TextView.PlatformInput` to position candidate windows |
| `CoreTextEditContext.NotifyFocusEnter()` / `NotifyFocusLeave()` | `NotifyFocusEnter()` / `NotifyFocusLeave()` | Implemented |
| `CoreTextEditContext.NotifyLayoutChanged()` | `NotifyLayoutChanged()` | Implemented — see `CoreTextEditContext.NotifyLayoutChanged` |
| `CoreTextEditContext.NotifySelectionChanged(CoreTextRange range)` | `NotifySelectionChanged(CoreTextRange)` + `CoreTextRange` | Implemented — `CoreTextRange` provided in `external/coretext/src/CoreTextRange.cs` |
| `CoreTextTextRequestedEventArgs.Request.Text` (mutable request) | `CoreTextTextRequest.Text` | Implemented — see `external/coretext/src/CoreTextTextRequest.cs` |
| `CoreTextTextUpdatingEventArgs.Range`, `NewSelection`, `Text` | `CoreTextTextUpdatingEventArgs` (NewText / Range / NewSelection / Text alias) | Implemented — see `external/coretext/src/CoreTextTextUpdatingEventArgs.cs` |
| `CoreTextSelectionRequestedEventArgs.Request.Selection` (StartCaretPosition/EndCaretPosition) | `CoreTextSelectionRequestedEventArgs.Request` with compatibility helpers | Implemented — convenience mapping available between `Start`/`Length` and `CoreTextRange` |
| `CoreTextLayoutRequestedEventArgs.Request.LayoutBounds.TextBounds` / `ControlBounds` | `CoreTextLayoutRequest.LayoutBounds.{TextBounds,ControlBounds}` | Implemented — see `external/coretext/src/CoreTextLayoutRequest.cs` and `CoreTextLayoutBounds.cs` |
| `CoreTextCommandReceivedEventArgs.Command` + `Handled` | `CoreTextCommandReceivedEventArgs.Command` + `Handled` | Implemented |
| `CoreTextDeferral` / async deferral pattern | `CoreTextDeferral` | Implemented |

Notes and priorities:

- Current status: the majority of the WinUI-parity surface required by `TextView.PlatformInput` has been implemented inside `LeXtudio.UI.Text.Core` (see files under `external/coretext/src`). The previous gaps (layout/selection/Range/Updating shapes) have been addressed.
- Remaining work (high value):
	- Remove as much platform branching from `TextView.PlatformInput` as practical (move host differences into adapters).
	- Add a comprehensive integration test matrix that exercises composition, candidate placement, caret updates, and selection across WinUI, macOS, and Linux hosts.
	- Verify CI packaging produces `libUnoEditMacInput.dylib` in the produced NuGet under `runtimes/*/native/` and that the sample restores from the local `dist/` feed.
- Low-effort improvements: add more convenience aliases and XML-docs to smooth call-sites and developer ergonomics.

Phase A — Investigation & mapping (complete)

- Inventory: scanned `TextView.PlatformInput.cs` and the `external/coretext/src` sources and produced the mapping above.
- Outcome: parity gaps identified in earlier plans are now implemented; Phase A is complete.

Phase B — Host cleanup (1-2 days)

- Remove platform-specific branching inside `TextView.PlatformInput` where the unified `CoreTextEditContext` API is sufficient. Prefer small, well-scoped refactors (constructor/DI or small adapter façade) rather than large rewrites.

Deliverable: PR that reduces #if branching and centralizes host-specific logic in `external/coretext` adapters or small host helpers.

Phase C — Tests & verification (2-4 days)

- Add unit tests for adapter translation and focused integration tests for composition and caret/candidate placement on all supported hosts.

Deliverable: test reports and CI jobs exercising the matrix.

Phase D — CI, packaging, docs, and merge (1-3 days)

- Ensure macOS CI job builds native helper and that packaging places `libUnoEditMacInput.dylib` under `runtimes/*/native/` in the nupkg. Add a smoke integration job that restores the sample from `dist/` and runs basic flows.

Deliverable: CI updates, packaging verification, and final docs updates.

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

