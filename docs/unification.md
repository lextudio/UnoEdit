# Unify LeXtudio.UI.Text.Core with Windows.UI.Text.Core

## Background

The codebase moved from branching host-specific IME logic inside the editor to
a platform-adapter pattern implemented in the `LeXtudio.UI.Text.Core` library.
The editor's host-side entrypoint is still
[UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs](UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs),
but event shapes, platform bridging, and native helpers now live under
`UnoEdit/external/coretext/src` and provide a WinUI-parity surface for hosts
to consume.

This document records the current implementation, maps WinUI-like concepts to
concrete source files, documents packaging/CI expectations, and lists
recommended next steps for contributors.

## Goal

Provide a single, well-defined text-input surface so that IME/composition,
caret/candidate positioning, selection, and platform commands behave
consistently across Windows, macOS and Linux. Hosts should call the same
public APIs/events and let `LeXtudio.UI.Text.Core` select and run the
appropriate platform adapter.

## Current implementation (summary)

- Core factory: `CoreTextServicesManager.GetForCurrentView().CreateEditContext()`
  returns a `CoreTextEditContext` backed by a platform adapter chosen at
  runtime. See [external/coretext/src/CoreTextServicesManager.cs](external/coretext/src/CoreTextServicesManager.cs).
- Central host surface: `CoreTextEditContext` exposes the public events and
  methods consumers expect (text requests/updates, selection/layout,
  composition lifecycle, command/deferral semantics). See
  [external/coretext/src/CoreTextEditContext.cs](external/coretext/src/CoreTextEditContext.cs).
- Platform adapters implement an internal `IPlatformTextInputAdapter` and live
  in `external/coretext/src`:
  - `Win32TextInputAdapter` (IMM32) — [external/coretext/src/Win32TextInputAdapter.cs](external/coretext/src/Win32TextInputAdapter.cs)
  - `MacOSTextInputAdapter` (AppKit bridge + native dylib) —
    [external/coretext/src/MacOSTextInputAdapter.cs](external/coretext/src/MacOSTextInputAdapter.cs) and
    [external/coretext/src/Native/MacOS/UnoEditMacInput.mm](external/coretext/src/Native/MacOS/UnoEditMacInput.mm)
  - `LinuxIbusTextInputAdapter` + `LinuxIBusConnection` (IBus/D-Bus) —
    [external/coretext/src/LinuxIBusTextInputAdapter.cs](external/coretext/src/LinuxIBusTextInputAdapter.cs) and
    [external/coretext/src/LinuxIBusConnection.cs](external/coretext/src/LinuxIBusConnection.cs)
  - `NullTextInputAdapter` for unsupported platforms —
    [external/coretext/src/NullTextInputAdapter.cs](external/coretext/src/NullTextInputAdapter.cs)
- Host compatibility helpers: `CoreTextCompatExtensions` in
  [UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs](UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs)
  provides helpers the editor uses (for example `SyncState` and
  `ApplyLayoutBoundsCompat`).

## Public surface and semantics

`CoreTextEditContext` (consumer-facing):

- Events the editor subscribes to:
  - `TextRequested` / `TextUpdating`
  - `SelectionRequested` / `SelectionUpdating`
  - `LayoutRequested`
  - `CompositionStarted` / `CompositionCompleted`
  - `FocusRemoved`
  - `CommandReceived`
- Methods hosts call:
  - `AttachToCurrentWindow(Window?)`
  - `NotifyCaretRectChanged(x, y, w, h)` (+ `RasterizationScale` overload)
  - `NotifyFocusEnter()` / `NotifyFocusLeave()`
  - `NotifyLayoutChanged()`
  - `NotifySelectionChanged(CoreTextRange)`
  - `ProcessKeyEvent(int virtualKey, bool shift, bool control, char? unicodeKey)`
  - `Dispose()`

Adapters raise events via internal `Raise*` helpers on `CoreTextEditContext`.
Adapters translate native IME callbacks (AppKit, IMM32, IBus) into a high-
level sequence of `TextRequested` → `TextUpdating` → composition events and
command notifications to match WinUI semantics where practical.

## How the editor (host) uses the library today

- The editor acquires a `CoreTextEditContext` via
  `CoreTextServicesManager.GetForCurrentView().CreateEditContext()` and stores it
  in the platform-input host. See
  [UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs](UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs#L1-L200).
- The host sets `InputScope`, subscribes to context events, calls
  `AttachToCurrentWindow(window)` and `NotifyFocusEnter()`, and uses
  `SyncState` to push layout, selection and caret state in a single call.
  `SyncState` is implemented in
  [UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs](UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs#L1-L120).
- When the platform requests text, the host fills `Request.Text`. On updates
  the host applies edits, updates selection, and calls the bridge helpers
  (`NotifyCaretRectChanged` / `NotifySelectionChanged`) to keep IME UI in sync.
- Hosts may call `ProcessKeyEvent` to forward keys to the adapter where the
  platform supports explicit key forwarding.

## Mapping table (WinUI → source locations)

| Windows concept | Implemented type / location | Notes |
|---|---:|---|
| Core factory / create context | `CoreTextServicesManager.CreateEditContext()` — [external/coretext/src/CoreTextServicesManager.cs](external/coretext/src/CoreTextServicesManager.cs) | Returns a `CoreTextEditContext` backed by an OS-specific adapter. |
| Central context & events | `CoreTextEditContext` — [external/coretext/src/CoreTextEditContext.cs](external/coretext/src/CoreTextEditContext.cs) | Public events/methods for hosts. |
| Attach / window handle resolution | `AttachToCurrentWindow` + `IPlatformTextInputAdapter` — [external/coretext/src/CoreTextEditContext.cs](external/coretext/src/CoreTextEditContext.cs) and [external/coretext/src/IPlatformTextInputAdapter.cs](external/coretext/src/IPlatformTextInputAdapter.cs) | Adapters resolve platform handles (NSWindow, HWND, X11, etc.). |
| Caret / layout notifications | `NotifyCaretRectChanged` / `NotifyLayoutChanged()` — [external/coretext/src/CoreTextEditContext.cs](external/coretext/src/CoreTextEditContext.cs) | macOS adapter re-applies the last caret on layout changes. |
| Selection types | `CoreTextRange` — [external/coretext/src/CoreTextRange.cs](external/coretext/src/CoreTextRange.cs) | Mirrors host start/end semantics. |
| Native macOS bridge | `MacOSTextInputAdapter` + `libUnoEditMacInput.dylib` — [external/coretext/src/MacOSTextInputAdapter.cs](external/coretext/src/MacOSTextInputAdapter.cs) and [external/coretext/src/Native/MacOS/UnoEditMacInput.mm](external/coretext/src/Native/MacOS/UnoEditMacInput.mm) | Adapter registers native callbacks that map to `CoreTextEditContext` events. |
| Win32 IME handling | `Win32TextInputAdapter` — [external/coretext/src/Win32TextInputAdapter.cs](external/coretext/src/Win32TextInputAdapter.cs) | Subclasses WndProc and maps `WM_IME_*` to context events. |
| Linux IBus integration | `LinuxIbusTextInputAdapter` + `LinuxIBusConnection` — [external/coretext/src/LinuxIBusTextInputAdapter.cs](external/coretext/src/LinuxIBusTextInputAdapter.cs) and [external/coretext/src/LinuxIBusConnection.cs](external/coretext/src/LinuxIBusConnection.cs) | Uses DBus/IBus to forward signals into the context. |

## Packaging & CI

- The macOS CI job builds the native macOS bridge and verifies the produced
  dylib. See [/.github/workflows/ci-macos.yml](.github/workflows/ci-macos.yml) —
  it runs the `BuildTextCoreMacInputBridge` target and checks for
  `libUnoEditMacInput.dylib` in the build output.
- Packaging scripts (`dist.all.sh`, `sign-and-pack.ps1`) and
  `external/coretext/PACKAGING.md` demonstrate how to include native assets in
  a nupkg. The pack output must place the native bridge under
  `runtimes/<rid>/native/` so consumers can deploy it with the final app.
- The `LeXtudio.UI.Text.Core` nupkg must be packed on macOS and signed on Windows.

## Sample & tests

- A sample project `LeXtudio.UI.Text.Core.Sample` exists under
  `external/coretext/sample` and can be used as a smoke harness for adapters.
- Recommended tests:
  - Adapter unit tests that simulate native callbacks and assert emitted
    `CoreTextEditContext` events.
  - Host-side tests for `CoreTextCompatExtensions.SyncState` behavior.
  - Integration/smoke tests that run the sample on CI macOS/Linux runners
    to verify candidate placement and composition lifecycle.

## Known gaps / recommendations

- Add adapter unit tests to validate native callback → `CoreTextEditContext`
  event translation.
- Add CI matrix entries to run the sample smoke tests on macOS and Linux
  runners where feasible.
- Trim host-side `#if` branching in `TextView.PlatformInput` by relying on
  `CoreTextCompatExtensions.SyncState` and the `CoreTextEditContext` public
  surface where parity suffices.

## Where to look in the code (quick links)

- Editor entrypoint and event handlers: [UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs](UnoEdit/src/UnoEdit/Rendering/TextView.PlatformInput.cs)
- Core service & adapter selection: [external/coretext/src/CoreTextServicesManager.cs](external/coretext/src/CoreTextServicesManager.cs)
- Core context and public events/methods: [external/coretext/src/CoreTextEditContext.cs](external/coretext/src/CoreTextEditContext.cs)
- macOS adapter and native bridge: [external/coretext/src/MacOSTextInputAdapter.cs](external/coretext/src/MacOSTextInputAdapter.cs) and [external/coretext/src/Native/MacOS/UnoEditMacInput.mm](external/coretext/src/Native/MacOS/UnoEditMacInput.mm)
- Win32 adapter: [external/coretext/src/Win32TextInputAdapter.cs](external/coretext/src/Win32TextInputAdapter.cs)
- Linux IBus helpers: [external/coretext/src/LinuxIBusConnection.cs](external/coretext/src/LinuxIBusConnection.cs)
- Compatibility extension helpers: [UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs](UnoEdit/src/UnoEdit/Compatibility/CoreTextCompatExtensions.cs)
