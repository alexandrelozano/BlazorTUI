using System.Drawing;
using Bunit;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ThemeTests : BunitContext
{
    public ThemeTests()
    {
        BunitJSModuleInterop keyboardModule = JSInterop.SetupModule("./_content/BlazorTUI/blazorTui.js");
        keyboardModule.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void PredefinedThemesExposeReusableStatePalettes()
    {
        TuiTheme theme = TuiTheme.Dark;

        Assert.Equal("Dark", theme.Name);
        Assert.Equal(theme.Input.ForeColor, theme.Resolve(TuiThemeRole.Input).ForeColor);
        Assert.Equal(theme.Focus.BackgroundColor, theme.Resolve(TuiThemeRole.Input, TuiThemeState.Focus).BackgroundColor);
        Assert.Equal(theme.Selected.BackgroundColor, theme.Resolve(TuiThemeRole.Selection, TuiThemeState.Selected).BackgroundColor);
        Assert.Equal(theme.Disabled.ForeColor, theme.Resolve(TuiThemeRole.Action, TuiThemeState.Disabled).ForeColor);
        Assert.Equal(theme.Error.BackgroundColor, theme.Resolve(TuiThemeRole.Input, TuiThemeState.Error).BackgroundColor);

        TuiTheme clone = theme.Clone();
        clone.Input.ForeColor = Color.Magenta;

        Assert.NotEqual(theme.Input.ForeColor, clone.Input.ForeColor);
        Assert.Throws<ArgumentOutOfRangeException>(() => theme.Resolve((TuiThemeRole)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => theme.Resolve(TuiThemeRole.Input, (TuiThemeState)99));
    }

    [Fact]
    public void ApplyThemeUpdatesContainersControlsAndMenuBar()
    {
        var screen = new Screen(40, 12);
        var menuBar = new MenuBar(Color.White, Color.Blue, screen);
        menuBar.AddMenu(new Menu("File", 'F'));
        screen.MenuBar = menuBar;

        var frame = new Frame(
            "frame", "THEME", 1, 1, 30, 8,
            Frame.BorderStyle.Line, Color.Yellow, Color.Blue);
        screen.TopContainer.AddContainer(frame);

        var label = new Label(
            "label", "Name:", 2, 2, 8,
            Color.Yellow, Color.Blue);
        frame.AddControl(label);

        var input = new TextBox(
            "input", "Alex", 10, 2, 10,
            Color.Yellow, Color.Black)
        {
            ThemeState = TuiThemeState.Error
        };
        frame.AddControl(input);

        var button = new Button(
            "save", "Save", 10, 4, 8,
            Color.White, Color.DarkGreen);
        frame.AddControl(button);

        var status = new StatusBar(
            "status", "Ready", 2, 6, 24,
            Color.Black, Color.Cyan);
        status.AddItem("help", "F1");
        frame.AddControl(status);

        screen.ApplyTheme(TuiTheme.Dark);
        screen.Render();

        Assert.Equal("Dark", screen.Theme.Name);
        Assert.Equal(TuiTheme.Dark.Accent.ForeColor, screen.MenuBar?.ForeColor);
        Assert.Equal(TuiTheme.Dark.Border.ForeColor, frame.ForeColor);
        Assert.Equal(TuiTheme.Dark.Surface.BackgroundColor, frame.BackgroundColor);
        Assert.Equal(TuiTheme.Dark.Surface.ForeColor, label.ForeColor);
        Assert.Equal(TuiTheme.Dark.Error.ForeColor, input.ForeColor);
        Assert.Equal(TuiTheme.Dark.Error.BackgroundColor, input.BackgroundColor);
        Assert.Equal(TuiTheme.Dark.Action.ForeColor, button.ForeColor);
        Assert.Equal(TuiTheme.Dark.Status.BackgroundColor, status.BackgroundColor);
        Assert.Equal(TuiTheme.Dark.Error.BackgroundColor, screen.Rows[3].Cells[11].BackgroundColor);

        screen.ApplyTheme(TuiTheme.Light);
        screen.Render();

        Assert.Equal("Light", screen.Theme.Name);
        Assert.Equal(TuiTheme.Light.Action.ForeColor, button.ForeColor);
        Assert.Equal(TuiTheme.Light.Error.BackgroundColor, screen.Rows[3].Cells[11].BackgroundColor);
    }

    [Fact]
    public void CustomRolesOverrideDefaultControlMapping()
    {
        var screen = new Screen(24, 6);
        var label = new Label(
            "label", "Accent", 1, 1, 8,
            Color.Yellow, Color.Blue)
        {
            ThemeRole = TuiThemeRole.Accent,
            ThemeState = TuiThemeState.Selected
        };
        screen.TopContainer.AddControl(label);

        screen.ApplyTheme(TuiTheme.HighContrast);

        Assert.Equal(TuiTheme.HighContrast.Selected.ForeColor, label.ForeColor);
        Assert.Equal(TuiTheme.HighContrast.Selected.BackgroundColor, label.BackgroundColor);
    }

    [Fact]
    public void ThemesExampleListsPredefinedThemes()
    {
        var component = Render<global::SampleApp.Pages.Examples.Themes>();
        string accessibleText = component.Find("pre.blazortui-visually-hidden").TextContent;

        Assert.Contains("THEMES", accessibleText);
        Assert.Contains("Classic", accessibleText);
        Assert.Contains("Dark", accessibleText);
        Assert.Contains("Light", accessibleText);
        Assert.Contains("High contrast", accessibleText);
        Assert.Contains("Theme: Dark", accessibleText);
    }
}
