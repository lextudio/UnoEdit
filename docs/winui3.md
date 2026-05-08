## WinUI 3 support — feasibility and recommended approach

Summary
-------

Short answer: yes — UnoEdit already supports integration with WinUI 3 hosts. The repository uses a pragmatic, hybrid approach: the core library (`src/UnoEdit/UnoEdit.csproj`) is Uno.Sdk-based and conditionally includes WinUI pages when the Windows target framework is enabled.

What I inspected
-----------------

- `src/UnoEdit/UnoEdit.csproj` — uses `Uno.Sdk` and conditionally adds a Windows TargetFramework (`net10.0-windows10.0.19041.0`) when building on Windows.

Practical options
-----------------

1) Recommended — keep the current gated-winui approach (low friction)

- Keep WinUI pages in `src/UnoEdit` and rely on the conditional windows TFM to include them only when building on Windows. This keeps platform-specific XAML close to the shared code and avoids an extra project.
- Pros: fewer projects to maintain; XAML lives next to core logic; easier to keep code and docs in sync.
- Cons: Windows-specific builds and XAML compilation must run on Windows CI.

2) Alternative — extract a dedicated WinUI wrapper project

- If you prefer a clear project boundary, extract WinUI-only pages into `src/UnoEdit.WinUI` and reference the core `src/UnoEdit` project. This isolates WinUI XAML/packaging and can simplify Windows-only CI.

Key code and API checks (audit checklist)
---------------------------------------

- Guard or replace WPF-only namespaces (`System.Windows.*`, `System.Windows.Media.*`) when present.
- Keep native helpers platform-isolated (e.g., macOS `libUnoEditMacInput.dylib` under `external/coretext`) so Windows packages don't pull non-Windows native build steps.
- Verify WinUI XAML/pages are gated by the windows TargetFramework in `UnoEdit.csproj`.

Packaging and NuGet guidance
---------------------------

- The repo's Windows TargetFramework uses a Windows App SDK package for the windows build (the `UnoEdit.csproj` currently references `Microsoft.WindowsAppSDK` for the windows TFM). For NuGet packaging, produce multi-target outputs that include `lib/net10.0-windows10.0.19041.0/` assets for WinUI consumers and keep native runtime assets under `runtimes/*/native/`.

Test matrix and CI
------------------

- Add or enable a Windows CI job that builds the windows TFM and the `src/UnoEdit.WinUI.Sample` project to validate XAML compilation and smoke-test the control surface.
- macOS/Linux CI should build the desktop target (`net10.0-desktop`) and verify platform-specific native assets (e.g., `libUnoEditMacInput.dylib`) where applicable.

Potential pitfalls
------------------

- Resource dictionary and theme differences between Uno and WinUI — validate theme merging in a native WinUI app.
- XAML compile errors are the most likely friction point — keeping WinUI XAML gated to the windows target isolates failures.

Recommended immediate next steps
-------------------------------

1. On Windows, build `src/UnoEdit` (windows target) or `src/UnoEdit.WinUI.Sample` to confirm WinUI pages compile cleanly.
2. Add or enable a Windows CI job that runs `dotnet build` for the windows target and the sample solution.
3. Optionally extract a separate `src/UnoEdit.WinUI` wrapper if you want an explicit project boundary for WinUI-only assets.

Checklist (current)
-------------------

- [x] WinUI pages included in `src/UnoEdit` (gated by windows TFM)
- [x] WinUI sample app present at `src/UnoEdit.WinUI.Sample`
- [ ] CI job to build the Windows/WinUI sample
- [ ] Package NuGet with multi-target outputs (include Windows assets)

If you want, I can scaffold a GitHub Actions job that builds the windows TFM and the WinUI sample. Which would you like next: scaffold the Windows CI job, or extract a separate `UnoEdit.WinUI` wrapper project?

