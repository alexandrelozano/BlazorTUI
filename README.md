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
            Frame.BorderStyle.line,
            Color.Yellow,
            Color.DarkBlue);

        screen.topContainer.AddContainer(frame);

        frame.AddControl(new Label(
            "nameLabel", "Name:", 2, 2, 12, Color.White, Color.DarkBlue));

        frame.AddControl(new TextBox(
            "nameInput", "", 14, 2, 24, Color.Yellow, Color.Black));

        var saveButton = new Button(
            "saveButton", "Save", 14, 5, 10, Color.White, Color.DarkGreen);

        saveButton.OnClick = _ =>
        {
            var message = new MessageBox(
                "Saved",
                "Result",
                MessageBox.Buttons.OKOnly,
                BorderStyle.line,
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

Click inside the terminal before using the keyboard so the screen element receives browser focus.

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
| Layout | `Frame` |
| Text and input | `Label`, `TextBox`, `TextArea`, `NumericBox`, `DateBox`, `TimeBox` |
| Selection | `CheckBox`, `RadioButton`, `ListBox`, `ColorPicker` |
| Actions and navigation | `Button`, `MenuBar`, `Menu`, `MenuItem` |
| Data and feedback | `GridView`, `ProgressBar`, `Spinner`, `PictureBox` |
| Modal UI | `Dialog`, `MessageBox` |

Control constructors accept `System.Drawing.Color` values for foreground and background colors. Interactive controls expose callbacks such as `OnClick`, `OnFocus`, and `OnLostFocus`.

## Keyboard and mouse interaction

- `Tab`: move to the next focusable control.
- `Shift+Tab`: move to the previous focusable control.
- `Enter` or `Space`: activate buttons and selection controls.
- Arrow keys: navigate text, lists, grids, color pickers, and menus where applicable.
- `Alt`: show menu shortcut keys.
- Mouse click: focus or activate the control under the selected cell.

When a dialog is open, it receives input until it is closed.

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
| [Controls and events](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/ControlsAndEvents.razor) | Text input, checkbox state, focus callbacks, click callbacks, and focus order |
| [Dialogs and menus](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/DialogsAndMenus.razor) | Menu shortcuts, custom modal dialogs, and message boxes |
| [Images](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Examples/Images.razor) | Loading encoded image bytes into a `PictureBox` |
| [Complete showcase](https://github.com/alexandrelozano/BlazorTUI/blob/master/SampleApp/Pages/Index.razor) | All controls, nested frames, z-order, callbacks, and animation |

Run `dotnet run --project SampleApp` from the repository root and open `/examples` to browse them. The example routes are exercised by the automated test suite so API changes cannot silently leave the documentation out of date.

## License

BlazorTUI is available under the [MIT License](https://github.com/alexandrelozano/BlazorTUI/blob/master/LICENSE.txt).
