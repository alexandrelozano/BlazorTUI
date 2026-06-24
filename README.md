# BlazorTUI

BlazorTUI is a Razor Class Library for building retro text user interfaces in Blazor Server applications. It provides a fixed-size character grid with keyboard and mouse interaction, nested layouts, menus, dialogs, colors, and animated controls.

[![NuGet](https://img.shields.io/nuget/v/BlazorTUI.svg)](https://www.nuget.org/packages/BlazorTUI)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/alexandrelozano/BlazorTUI/blob/master/LICENSE.txt)

![BlazorTUI sample application](https://raw.githubusercontent.com/alexandrelozano/BlazorTUI/master/Resources/sampleapp.gif)

## Features

- Character-cell rendering with foreground and background colors.
- Nested frames with relative coordinates and z-order.
- Keyboard focus, `Tab`/`Shift+Tab` navigation, and mouse interaction.
- Menu bars with shortcuts and keyboard navigation.
- Modal dialogs and configurable message boxes.
- Responsive terminal scaling.

## Requirements

- A Blazor Server application.
- The .NET 10 SDK.

## Installation

Install BlazorTUI from NuGet:

```powershell
dotnet add package BlazorTUI
```

## Quick start

Add the BlazorTUI namespace to a Razor page, create a `Screen`, and render it with the component:

```razor
@page "/terminal"
@using global::BlazorTUI.TUI
@using Color = System.Drawing.Color

<BlazorTUI.BlazorTUI screen="@screen" />

@code {
    private readonly Screen screen = CreateScreen();

    private static Screen CreateScreen()
    {
        var screen = new Screen(80, 40);

        var frame = new Frame(
            "mainFrame",
            "CUSTOMER",
            12,
            5,
            56,
            12,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);

        screen.TopContainer.AddContainer(frame);

        frame.AddControl(new Label(
            "nameLabel", "Name:", 2, 2, 12, Color.White, Color.DarkBlue));

        frame.AddControl(new TextBox(
            "nameInput", "", 14, 2, 24, Color.Yellow, Color.Black));

        var saveButton = new Button(
            "saveButton", "Save", 14, 5, 10, Color.White, Color.DarkGreen);

        saveButton.Clicked += (_, _) =>
        {
            var message = new MessageBox(
                "Saved",
                "Result",
                MessageBox.Buttons.OKOnly,
                BorderStyle.Line,
                Color.White,
                Color.DarkGreen,
                screen);

            message.Show();
        };

        frame.AddControl(saveButton);
        screen.SetFocus("nameInput");

        return screen;
    }
}
```

Click inside the terminal before using the keyboard so the screen element receives browser focus. The focused terminal has a visible outline.

## Screen and layout

A `Screen` defines the terminal dimensions. Character cells keep a 1:2 width-to-height ratio at any supported screen size; there is no predefined row limit. A screen twice as wide as it is high—for example, `80 × 40`—forms a square terminal, while other dimensions preserve their natural aspect ratio.

The terminal grows to the available host size without overflowing the viewport. To embed it in a smaller area, give its parent an explicit width and height; the grid will fit inside that area automatically.

Coordinates are zero-based. Controls use coordinates relative to their parent container, which makes it possible to move a complete group by repositioning its `Frame`.

Every control must have a non-empty, unique name. Add elements through:

- `AddContainer` for nested frames and containers.
- `AddControl` for controls inside a container.

These methods initialize parent references, tab order, and z-order. Use `screen.SetFocus("controlName")` to select the initial control.

## Available controls

| Category | Controls |
| --- | --- |
| Layout | `Frame`, `TabControl`, `TabPage` |
| Text and input | `Label`, `TextBox`, `PasswordBox`, `TextArea`, `NumericBox`, `DateBox`, `TimeBox` |
| Selection | `CheckBox`, `RadioButton`, `ComboBox`, `ListBox`, `ColorPicker` |
| Actions and navigation | `Button`, `MenuBar`, `Menu`, `MenuItem` |
| Data and feedback | `GridView`, `ProgressBar`, `Spinner`, `PictureBox` |
| Modal UI | `Dialog`, `MessageBox` |

Control constructors accept `System.Drawing.Color` values for foreground and background colors.

## Public API conventions

The recommended API follows standard .NET naming and event conventions:

- Use properties such as `Screen.Width`, `Screen.Rows`, `Screen.TopContainer`, `Control.Name`, `Control.Width`, and `TextBox.Value`.
- Use `IClipboardControl` when application code needs to select, inspect, cut, or paste text programmatically.
- Use `IUndoableControl` to inspect, clear, undo, or redo a text control's bounded edit history programmatically.
- Subscribe to `Clicked`, `GotFocus`, and `LostFocus` with `+=`. These events work consistently for mouse and keyboard activation.
- Use `MenuBar.AddMenu` and `Menu.AddItem` to build menus. Their `Menus` and `Items` properties provide read-only views.
- Use PascalCase enum members such as `BorderStyle.Line` and `Frame.BorderStyle.Solid`.

Lowercase members from earlier releases remain available in the `0.8.x` line for source and binary compatibility. New code should use the recommended members above.

## Keyboard and mouse interaction

- `Tab`: move to the next focusable control.
- `Shift+Tab`: move to the previous focusable control.
- `Alt+PageDown` or `Alt+PageUp`: move to the next or previous page of the focused `TabControl`. `Ctrl+Tab` is also supported when the browser does not reserve it for browser-tab navigation.
- `Enter` or `Space`: activate buttons and selection controls, and open or confirm a `ComboBox`.
- `F4`: open or close the focused `ComboBox`; `Escape` closes it without changing the selection.
- Arrow keys: navigate text, combo boxes, lists, grids, color pickers, and menus where applicable.
- `Shift` plus the arrow, `Home`, or `End` keys: select text in `TextBox` and `TextArea`.
- `Ctrl+A`, `Ctrl+C`, `Ctrl+X`, and `Ctrl+V`: select all, copy, cut, and paste in text controls. Use `Command` instead of `Ctrl` on macOS.
- `Ctrl+Z` and `Ctrl+Y`: undo and redo text edits. On macOS, use `Command+Z` and `Command+Shift+Z`.
- `Alt`: show menu shortcut keys.
- Mouse click: focus or activate the control under the selected cell.

When a dialog is open, it receives input until it is closed.

The component exposes the terminal as a labelled interactive region and provides a text representation for screen readers. Give each terminal a meaningful label and, when useful, a short description:

```razor
<BlazorTUI.BlazorTUI
    screen="@screen"
    AriaLabel="Order entry terminal"
    AriaDescription="Enter customer and delivery details, then submit the order." />
```

Clipboard and edit-history shortcuts are intercepted only while a compatible `TextBox` or `TextArea` has focus. Other browser and assistive-technology shortcuts that use `Ctrl`, `Command`, or modified `Alt` combinations remain available to the browser. Clipboard access follows browser security and permission rules; use HTTPS outside local development.

Each text control retains its latest 100 text-changing operations. Undo and redo restore the text, cursor, selection, and `TextArea` scroll position. Assigning `Value` or calling `ClearHistory()` starts a new history.

Pasting into a `TextBox` converts line breaks to spaces and respects the control width. `TextArea` preserves line breaks and applies its `MaxTextWidth` and `MaxLines` limits.

## Combo box

`ComboBox` displays one selected value and opens a scrollable list over the controls below it:

```csharp
var priority = new ComboBox(
    "priority",
    new[] { "Low", "Normal", "High", "Urgent" },
    14,
    9,
    22,
    Color.Yellow,
    Color.Black,
    selectedIndex: 1,
    maxDropDownItems: 4);

priority.SelectedIndexChanged += (_, _) =>
    status.Value = $"Priority: {priority.SelectedItem}";

frame.AddControl(priority);
```

Use `SelectedIndex`, `SelectedItem`, `SelectIndex`, or `SelectItem` to control the selection. `Items` is read-only; update it through `AddItem`, `RemoveItem`, and `ClearItems`. Users can change a closed combo box with the arrow, `Home`, and `End` keys, or open it with `Enter`, `Space`, or `F4`. While open, `Enter` confirms the highlighted item and `Escape` cancels it.

## Password input

`PasswordBox` provides the selection, paste, and undo/redo behavior of `TextBox` while rendering a mask instead of the stored value:

```csharp
var password = new PasswordBox(
    "password",
    "",
    14,
    6,
    22,
    Color.Yellow,
    Color.Black,
    '*');

frame.AddControl(password);
```

The default mask is `•`. Use `IsRevealed` or `ToggleReveal()` to show the value explicitly. Copying and cutting are disabled by default, while pasting remains enabled; configure `AllowCopy` and `AllowPaste` when different behavior is required. `Value` always contains the unmasked text and should be handled as sensitive data.

## Tabbed layouts

`TabControl` is a container whose `TabPage` children each own an independent control tree. Add the tab control to a frame or screen before populating its pages so control-name validation covers the complete screen:

```csharp
var tabs = new TabControl(
    "settingsTabs", 3, 4, 40, 14,
    Color.Yellow, Color.DarkBlue);
frame.AddContainer(tabs);

TabPage profile = tabs.AddTab("profileTab", "Profile");
profile.AddControl(new TextBox(
    "userName", "", 2, 2, 18,
    Color.Yellow, Color.Black));

TabPage options = tabs.AddTab("optionsTab", "Options");
options.AddControl(new CheckBox(
    "notifications", "Enable notifications", 2, 2, 28,
    Color.Yellow, Color.DarkBlue));
```

Select pages with `SelectedIndex`, `SelectTab`, `SelectNextTab`, or `SelectPreviousTab`. Users can click headers or press `Alt+PageDown` and `Alt+PageUp`; focus moves to the first focusable control on the new page. `Ctrl+Tab` remains available in browsers that deliver that shortcut to web content.

## Images

`PictureBox` accepts encoded image bytes. The default constructor treats the data as PNG:

```csharp
byte[] imageData = File.ReadAllBytes("logo.png");

var picture = new PictureBox(
    "logo",
    imageData,
    47,
    17,
    10,
    5,
    Color.White,
    Color.Black);

frame.AddControl(picture);
```

For another browser-supported format, provide its MIME type after the byte array:

```csharp
var picture = new PictureBox(
    "photo",
    jpegData,
    "image/jpeg",
    47,
    17,
    10,
    5,
    Color.White,
    Color.Black);
```

BlazorTUI displays the encoded image without resizing or converting the source data. If you are upgrading from an earlier version, replace the previous `System.Drawing.Image` constructor with one of the byte-array constructors above.

## Executable examples

The repository contains focused pages that can be run directly:

| Example | Demonstrates |
| --- | --- |
| [Controls and events](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/ControlsAndEvents.razor) | Text and password input, combo-box selection, checkbox state, callbacks, and focus order |
| [Dialogs and menus](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/DialogsAndMenus.razor) | Menu shortcuts, custom modal dialogs, and message boxes |
| [Images](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/Images.razor) | Loading encoded image bytes into a `PictureBox` |
| [TabControl](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/Tabs.razor) | Tab pages, nested controls, focus changes, mouse selection, and keyboard navigation |
| [Complete showcase](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Index.razor) | All controls, nested frames, z-order, callbacks, and animation |

Run `dotnet run --project SampleApp` from the repository root and open `/examples` to browse them. The example routes are exercised by the automated test suite so API changes cannot silently leave the documentation out of date.

## Changelog

### 0.8.5 — 2026-06-24

- Added `ComboBox` with a bounded scrollable drop-down, mouse selection, keyboard navigation, collection helpers, and selection-change events.
- Added `TabControl` and `TabPage` containers with independent page contents, mouse selection, programmatic selection, and change events.
- Added focus-aware `Ctrl+Tab` and `Ctrl+Shift+Tab` navigation with screen-reader announcements.
- Added `Alt+PageDown` and `Alt+PageUp` navigation for browsers that reserve `Ctrl+Tab` and do not expose it to web pages.
- Preserved tab navigation when the selected page has no focusable controls, including wrap-around from the final page.
- Prevented hidden containers from receiving keyboard input and preserved unique control-name validation across prebuilt tab pages.

### 0.8.4 — 2026-06-24

- Added `PasswordBox` with configurable masking and explicit reveal support.
- Preserved text selection and bounded undo/redo behavior while keeping the unmasked value out of the cell buffer by default.
- Added configurable copy and paste policies; password copying and cutting are disabled by default while pasting remains enabled.
- Removed the unused `ExampleJsInterop` template class, which referenced a JavaScript file that is not part of the package.

### 0.8.3 — 2026-06-24

- Added text selection and clipboard support to `TextBox` and `TextArea` through `Ctrl`/`Command` + `A`, `C`, `X`, and `V`.
- Added the public `IClipboardControl` API for programmatic selection, inspection, cutting, and pasting.
- Added multiline paste normalization, configured text-limit enforcement, visible selection colors, and browser clipboard fallbacks.
- Added bounded undo and redo history for `TextBox` and `TextArea`, including `Ctrl`/`Command` keyboard shortcuts and the public `IUndoableControl` API.

### 0.8.2 — 2026-06-23

- Added a consistent PascalCase public API, standard .NET events, read-only collection views, and precise argument validation while retaining the legacy `0.8.x` members.
- Added focused executable examples for controls, events, dialogs, menus, and images.
- Added accessible terminal semantics, screen-reader text, live menu and dialog announcements, visible focus, and selective keyboard interception that preserves browser shortcuts.

### 0.8.1 — 2026-06-23

- Added revision-based incremental rendering, unchanged-row skipping, cached cell CSS, and synchronization-safe timer updates.
- Added responsive rendering for arbitrary positive screen dimensions without a predefined row limit.
- Corrected cursor visibility and duplicate frame-title rendering regressions.

### 0.8.0 — 2026-06-23

- Upgraded the library, sample, dependencies, and package to .NET 10.
- Removed the `System.Drawing.Common` dependency and changed `PictureBox` to consume encoded image bytes for cross-platform use.
- Eliminated compiler, analyzer, nullable, package, and platform warnings.
- Added automated unit, component, HTTP, and NuGet-consumer tests with Windows and Linux CI validation.
- Reworked the README around installing and using the library from NuGet.

## License

BlazorTUI is available under the [MIT License](https://github.com/alexandrelozano/BlazorTUI/blob/master/LICENSE.txt).
