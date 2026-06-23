# Repository guide for coding agents

## Purpose

BlazorTUI is a .NET 10 Razor Class Library that renders a text user interface in a Blazor Server application. The repository also contains `SampleApp`, a Blazor Server application that exercises the public controls.

Keep changes focused on this model: controls draw into a two-dimensional `Screen` cell buffer, and the `BlazorTUI.razor` component projects that buffer into HTML.

## Repository layout

- `BlazorTUI/`: the packageable Razor Class Library.
- `BlazorTUI/TUI/`: screen, containers, controls, menus, dialogs, and cell rendering.
- `BlazorTUI/Utils/`: internal helpers.
- `BlazorTUI/BlazorTUI.razor`: browser event handling and cell-to-DOM rendering.
- `BlazorTUI/BlazorTUI.razor.scss`: source for the component's generated CSS.
- `SampleApp/`: the executable showcase; its main example is `Pages/Index.razor`.
- `Resources/`: README and NuGet artwork.

## Architecture and invariants

- `Screen` owns the cell rows, root container, optional menu bar, and active dialog stack.
- `Container` and `Frame` establish nested coordinate systems. Add children with `AddContainer` and controls with `AddControl`; these methods set parent/container references and z-order.
- Every control name must be non-empty and unique across the screen's container tree. `AddControl` enforces this rule.
- Coordinates and dimensions use `short` and are zero-based. Child coordinates are relative to their parent.
- A screen is intended to be twice as wide as it is high (for example, `80 × 40`) because terminal cells are rendered at a 1:2 width-to-height ratio.
- The generated stylesheet defines screen heights from 1 through 40. If this range changes, update the SCSS loop and regenerate both CSS outputs.
- Interactive controls opt into focus with `TabStop`. Preserve `Tab`, `Shift+Tab`, arrow-key, `Enter`, and `Space` behavior when modifying input handling.
- Browser key names are passed directly to `KeyDown`; use values such as `ArrowLeft`, `ArrowRight`, `Enter`, and `Backspace`.
- Controls render by mutating `Cell` instances. Rendering must respect visibility, parent offsets, bounds, colors, and z-order.
- Dialog input is modal: when the dialog stack is non-empty, keyboard and click events go to the top dialog.
- `PictureBox` receives encoded image bytes and stores a base64 `data:` URI in the cell buffer. Keep image decoding and transcoding out of the library so it remains cross-platform.

## Build and validation

Run commands from the repository root:

```powershell
dotnet restore .\BlazorTUI.sln
dotnet build .\BlazorTUI.sln --configuration Release
dotnet run --project .\SampleApp\SampleApp.csproj
```

There is currently no automated test project. For library changes, the minimum validation is a successful solution build plus a manual check in `SampleApp` for affected rendering, focus, keyboard, and mouse behavior. Add regression tests if a test project is introduced.

The solution must build with zero warnings. Treat any compiler, analyzer, nullable-reference, package, or platform warning as a regression; fix its cause instead of suppressing it globally.

Building `BlazorTUI.csproj` in Release also produces the NuGet package because `GeneratePackageOnBuild` is enabled. Verify package metadata and packed assets after changing the project file or README:

```powershell
dotnet build .\BlazorTUI\BlazorTUI.csproj --configuration Release
```

## Coding conventions

- Follow the existing C# and Razor layout: four-space indentation, braces on new lines, nullable reference types enabled, and implicit usings enabled.
- Preserve existing public member names even where they do not follow current .NET naming conventions; renaming them is a breaking change.
- Use explicit, unique control names in examples and production code.
- Keep control construction and event wiring readable. Assign `OnClick`, `OnFocus`, and `OnLostFocus` after construction where the API expects it.
- Use `System.Drawing.Color` consistently with the existing public constructors. It comes from the cross-platform `System.Drawing.Primitives` runtime assembly; do not add `System.Drawing.Common`.
- Do not edit only `BlazorTUI.razor.css` or only its minified counterpart. Change `BlazorTUI.razor.scss`, then regenerate `BlazorTUI.razor.css` and `BlazorTUI.razor.min.css` according to `compilerconfig.json`.
- Keep the library free of dependencies on `SampleApp`; the sample may depend on the library only through its project reference.

## Documentation expectations

- Keep `README.md` examples compilable against the current public API.
- Update the controls list, compatibility notes, and sample when adding or removing public functionality.
- Document breaking changes and platform restrictions explicitly.
- Keep the README usable both on GitHub and when embedded in the NuGet package; use absolute URLs for repository-only images.
