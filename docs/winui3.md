# WinUI 3 support — feasibility and recommended approach

Summary
-------

Short answer: yes — UnoEdit can be made consumable by WinUI 3 apps. The safest, lowest-risk path is to expose a WinUI-specific wrapper (a small WinUI class library) that references the cross-platform core code. Multi-targeting the existing Uno.Sdk project is possible but more complex because of XAML compilation/Uno SDK build steps.

What I inspected
-----------------

- The library in this repo is authored with `Uno.Sdk` and contains many `*.uno.cs` partials and XAML `Page` items; it already targets the Uno/WinUI API surface in many places (e.g., `Microsoft.UI.Xaml`-style types and Uno compatibility shims).
- The current library compiles as an Uno-targeted component and includes platform-specific IME bridges and rendering forks that are already WinUI-/Uno-aware.

Two practical options
---------------------

1) Recommended — Add a small WinUI 3 wrapper project (safe, incremental)

- Create a new WinUI class library project named `UnoEdit.WinUI` (or similar) that targets a WinUI TFM, e.g. `net7.0-windows10.0.19041.0` (or the TFM matching your projects).
- Reference the shared core `UnoEdit` project with a `ProjectReference` and keep WinUI-only XAML and resource glue inside this wrapper. This avoids changing the existing Uno SDK project layout and keeps the platform surface thin.
- In the wrapper's csproj, add a reference to the Windows App SDK / WinUI package used by your apps, but mark it as a development/implementation detail so the consuming app decides the Windows App SDK version. Example:

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net7.0-windows10.0.19041.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<!-- Use PrivateAssets so the App SDK version does not flow to package consumers -->
		<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.4.0" PrivateAssets="all" />
		<ProjectReference Include="..\UnoEdit\UnoEdit.csproj" />
	</ItemGroup>

	<ItemGroup>
		<!-- WinUI XAML pages and code-behind (platform glue) live here -->
		<Page Include="TextEditor.Xaml" />
		<Compile Include="TextEditor.Xaml.cs" />
	</ItemGroup>
</Project>
```

Advantages:

- Keeps the main, cross-platform codebase unchanged.
- Limits WinUI-specific XAML/markup to a single wrapper project so XAML compilation for WinUI is predictable.
- Simplifies NuGet packaging: publish a package that contains both netstandard (or Uno SDK build) and net<windows> assets, or publish platform-specific packages.

Key code and API checks (audit checklist)
---------------------------------------

- Search for WPF-only namespaces (`System.Windows.*`, `System.Windows.Media.*`) and replace or guard them — this repo already contains many compatibility shims.
- Verify any P/Invoke or Win32-only calls (IME bridges, native macOS bridging) are isolated behind platform folders; WinUI apps run on Windows and must not pull non-Windows native build steps into the package.
- Confirm XAML markup and resource dictionaries are compatible with WinUI 3 (the repo already references WinUI/Uno types in places, which is a good sign).

Packaging and NuGet guidance
---------------------------

- Prefer producing a NuGet package: `lib/net7.0-windows10.0.19041.0/UnoEdit.WinUI.dll` (WinUI wrapper).
- Force a minimal Windows App SDK version on consumers.

Test matrix and CI
------------------

- Add CI jobs that build the WinUI wrapper on a Windows runner to validate XAML compilation.
- Add a small WinUI 3 sample app (in `src/UnoEdit.WinUI.Sample`) that references the wrapper and exercises basic scenarios (open file, edit, syntax highlighting). Keep sample minimal so build is quick.

Potential pitfalls
------------------

- Differences in resource dictionary merging and theme lookup between Uno and WinUI 3; test theme resources on a native WinUI app.
- If any control relies on Uno-specific renderers (Skia runtime internals), confirm that equivalent rendering is available or provide a WinUI-specific implementation.
- XAML compile errors are the most likely friction — keep WinUI XAML in the WinUI wrapper so compile-time failures are isolated.

Recommended immediate next steps
-------------------------------

1. Create a new project `src/UnoEdit.WinUI` with TFM `net7.0-windows...` and add files from `src/UnoEdit/UnoEdit.csproj`.
2. Try building on Windows.
3. Iterate on any API or resource compatibility issues discovered during the build.
4. Once stable, add a small WinUI sample UnoEdit.WinUI.Sample and a CI job to build it on Windows.

Checklist (quick)
-----------------

- [x] Create `UnoEdit.WinUI` project
- [x] Add files from `UnoEdit` gradually to this new WinUI project as links (~120 files linked)
- [x] Build on Windows and fix XAML compile errors (0 errors, 7 warnings)
- [x] Add WinUI sample app `src/UnoEdit.WinUI.Sample` — unpackaged, self-contained, win-x64
- [ ] Add CI job (Windows runner, `dotnet build src/UnoEdit.WinUI.Sample`)
- [ ] Package NuGet with multi-target outputs

If you want, I can scaffold the `src/UnoEdit.WinUI` project and a tiny WinUI sample app next (CSProj + minimal XAML + CI job), or I can instead prepare a multi-target csproj approach. Which path do you prefer?

Notes / references
------------------

- This repo already leans toward WinUI/Uno compatibility (see `Compatibility` shims and `*.uno.cs` partials). That makes the wrapper approach particularly practical and low-risk.

