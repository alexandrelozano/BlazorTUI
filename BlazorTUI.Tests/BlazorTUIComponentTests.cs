using System.Drawing;
using Bunit;
using BlazorTUI.TUI;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorTUI.Tests;

public class BlazorTUIComponentTests : BunitContext
{
    public BlazorTUIComponentTests()
    {
        BunitJSModuleInterop keyboardModule = JSInterop.SetupModule("./_content/BlazorTUI/blazorTui.js");
        keyboardModule.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void CursorRemainsVisibleOnBlankCell()
    {
        var screen = new Screen(8, 4);
        var textBox = new TextBox("text", "", 0, 0, 4, Color.White, Color.Black);
        screen.topContainer.AddControl(textBox);
        screen.SetFocus("text");
        screen.Render();

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        string style = component.FindAll(".tilefs")[0].GetAttribute("style") ?? "";
        Assert.Contains("box-shadow:inset 0 -0.08em currentColor", style);
    }

    [Fact]
    public async Task StaticScreenSkipsPeriodicComponentRenders()
    {
        var screen = new Screen(8, 4);
        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        int initialRenderCount = component.RenderCount;

        await Task.Delay(700);

        Assert.Equal(initialRenderCount, component.RenderCount);
    }

    [Fact]
    public void OnlyChangedRowsAreRenderedAgain()
    {
        var screen = new Screen(8, 4);
        var label = new Label("label", "X", 0, 0, 1, Color.White, Color.Black);
        screen.topContainer.AddControl(label);
        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        IReadOnlyList<IRenderedComponent<global::BlazorTUI.TuiRow>> rows =
            component.FindComponents<global::BlazorTUI.TuiRow>();
        int firstRowRenderCount = rows[0].RenderCount;
        int secondRowRenderCount = rows[1].RenderCount;

        label.foreColor = Color.Yellow;
        component.Render(parameters => parameters.Add(instance => instance.screen, screen));

        Assert.Equal(firstRowRenderCount + 1, rows[0].RenderCount);
        Assert.Equal(secondRowRenderCount, rows[1].RenderCount);
        Assert.Contains("color:#FFFF00", component.Markup);
    }

    [Fact]
    public void ComponentRendersOneTilePerCell()
    {
        var screen = new Screen(4, 2);
        screen.topContainer.AddControl(new Label("label", "OK", 0, 0, 2, Color.White, Color.Black));

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        Assert.Equal(8, component.FindAll(".tilefs").Count);
        Assert.All(component.FindAll(".tilefs"), tile => Assert.True(tile.HasAttribute("b-blazortui")));
        Assert.Contains(">O</div>", component.Markup);
        Assert.Contains(">K</div>", component.Markup);
    }

    [Fact]
    public void ComponentExposesAnAccessibleTerminalSurface()
    {
        var screen = new Screen(4, 2);
        screen.TopContainer.AddControl(new Label("label", "OK", 0, 0, 2, Color.White, Color.Black));

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters => parameters
            .Add(instance => instance.screen, screen)
            .Add(instance => instance.AriaLabel, "Order entry terminal")
            .Add(instance => instance.AriaDescription, "Enter and submit an order."));

        var grid = component.Find(".gridfs");
        Assert.Equal("application", grid.GetAttribute("role"));
        Assert.Equal("Order entry terminal", grid.GetAttribute("aria-label"));
        Assert.Contains("Tab", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("F4", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("PageUp", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("Enter and submit an order.", component.Markup);
        Assert.Contains("OK", component.Find("pre.blazortui-visually-hidden").TextContent);
        Assert.All(component.FindAll(".tilefs"), tile => Assert.Equal("true", tile.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void ComponentExposesSemanticControlSummaries()
    {
        var screen = new Screen(45, 12);
        var grid = new GridView(
            "ordersGrid",
            new[]
            {
                new GridView.GridColumn { Title = "Order", Width = 8 },
                new GridView.GridColumn { Title = "Status", Width = 10 }
            },
            new[]
            {
                new GridView.GridRow { Cells = new[] { "1", "Open" } }
            },
            0, 0, 28, 4,
            Color.White,
            Color.Black)
        {
            ScreenReaderDescription = "Orders awaiting delivery."
        };
        var priority = new ComboBox(
            "priority",
            new[] { "Normal", "High" },
            0, 5, 12,
            Color.White,
            Color.Black)
        {
            ScreenReaderSummary = "Priority selector"
        };
        var tabs = new TabControl("tabs", 20, 5, 20, 6, Color.White, Color.Black);
        tabs.AddTab("details", "Details");
        tabs.AddTab("history", "History");

        screen.TopContainer.AddControl(grid);
        screen.TopContainer.AddControl(priority);
        screen.TopContainer.AddContainer(tabs);

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        string summaries = component.Find("[aria-label=\"Control summaries\"]").TextContent;
        Assert.Contains("GridView ordersGrid: materialized, 1 rows, 2 columns, page 1 of 1", summaries);
        Assert.Contains("Orders awaiting delivery.", summaries);
        Assert.Contains("Priority selector", summaries);
        Assert.Contains("TabControl tabs: selected tab Details, 2 tabs.", summaries);
        Assert.Contains("-controls", component.Find(".gridfs").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void FocusChangesAreAnnouncedAndStored()
    {
        var screen = new Screen(12, 4);
        var first = new TextBox("first", "", 0, 0, 4, Color.White, Color.Black)
        {
            ScreenReaderSummary = "First name field"
        };
        var second = new TextBox("second", "", 0, 1, 4, Color.White, Color.Black)
        {
            ScreenReaderDescription = "Customer surname input."
        };
        screen.TopContainer.AddControl(first);
        screen.TopContainer.AddControl(second);
        screen.SetFocus("first");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        component.Find(".gridfs").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        string focusStatus = component.FindAll("[role=status]")[^1].TextContent;
        Assert.Contains("Focus moved to TextBox second. Customer surname input.", focusStatus);
        Assert.Contains("Focus moved to TextBox second. Customer surname input.", component.Instance.FocusHistoryAnnouncements[^1]);
    }

    [Fact]
    public void ControlAccessibilitySummaryCanBeCustomizedAndIncludesValidation()
    {
        var input = new TextBox("name", "", 0, 0, 8, Color.White, Color.Black)
        {
            ScreenReaderSummary = "Customer name field",
            ScreenReaderDescription = "Required for delivery labels.",
            IsRequired = true,
            RequiredMessage = "Name required"
        };

        input.Validate();

        string summary = input.GetAccessibilitySummary();
        Assert.Contains("Customer name field", summary);
        Assert.Contains("Required for delivery labels.", summary);
        Assert.Contains("Invalid: Name required", summary);
    }

    [Fact]
    public void ComponentSupportsArbitraryScreenDimensions()
    {
        var screen = new Screen(10, 50);

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        var grid = component.Find(".gridfs");
        string style = grid.GetAttribute("style") ?? "";
        Assert.DoesNotContain("sizefs-", grid.ClassName);
        Assert.Contains("--tui-columns:10", style);
        Assert.Contains("--tui-rows:50", style);
        Assert.Contains("--tui-width-from-height:10cqh", style);
        Assert.Contains("--tui-height-from-width:1000cqw", style);
        Assert.Equal(500, component.FindAll(".tilefs").Count);
        Assert.Contains("grid-column:10; grid-row:50", component.FindAll(".tilefs")[^1].GetAttribute("style"));
    }

    [Fact]
    public void KeyDownMovesFocusAndClickInvokesControl()
    {
        var screen = new Screen(8, 4);
        var first = new TextBox("first", "", 0, 0, 4, Color.White, Color.Black);
        var second = new TextBox("second", "", 0, 1, 4, Color.White, Color.Black);
        bool clicked = false;
        var button = new Button("button", "Go", 0, 2, 4, Color.White, Color.DarkGreen)
        {
            OnClick = _ => clicked = true
        };
        screen.topContainer.AddControl(first);
        screen.topContainer.AddControl(second);
        screen.topContainer.AddControl(button);
        screen.SetFocus("first");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        component.Find(".gridfs").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.True(second.Focus);

        component.FindAll(".tilefs")[16].Click();
        Assert.True(clicked);
    }

    [Fact]
    public void ModifiedBrowserShortcutsAreNotForwardedToControls()
    {
        var screen = new Screen(8, 4);
        var textBox = new TextBox("text", "", 0, 0, 4, Color.White, Color.Black);
        screen.TopContainer.AddControl(textBox);
        screen.SetFocus("text");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var grid = component.Find(".gridfs");

        grid.KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });
        grid.KeyDown(new KeyboardEventArgs { Key = "k", MetaKey = true });
        grid.KeyDown(new KeyboardEventArgs { Key = "x", AltKey = true });
        Assert.Equal("", textBox.Value);

        grid.KeyDown(new KeyboardEventArgs { Key = "a" });
        Assert.Equal("a", textBox.Value);
    }

    [Fact]
    public void ComponentIgnoresEmptyKeyAndForwardsSpaceKey()
    {
        var screen = new Screen(12, 4);
        var toggle = new ToggleSwitch("toggle", "Enabled", false, 0, 0, 10, Color.White, Color.Black);
        screen.TopContainer.AddControl(toggle);
        screen.SetFocus("toggle");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var grid = component.Find(".gridfs");

        Exception? exception = Record.Exception(() => grid.KeyDown(new KeyboardEventArgs { Key = "" }));
        Assert.Null(exception);
        Assert.False(toggle.Value);

        grid.KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.True(toggle.Value);
    }

    [Fact]
    public void OpenDialogsAreAnnounced()
    {
        var screen = new Screen(12, 6);
        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var dialog = new Dialog("confirm", "Confirm", 10, 4, BorderStyle.Line, Color.White, Color.Black, screen);

        dialog.Show();
        component.Render(parameters => parameters.Add(instance => instance.screen, screen));

        Assert.Contains("Dialog opened: Confirm", component.Find("[role=status]").TextContent);
    }

    [Fact]
    public async Task ComponentRoutesClipboardOperationsToTheFocusedTextControl()
    {
        var screen = new Screen(12, 4);
        var textBox = new TextBox("name", "Alex", 0, 0, 8, Color.White, Color.Black);
        var button = new Button("save", "Save", 0, 1, 6, Color.White, Color.DarkGreen);
        screen.TopContainer.AddControl(textBox);
        screen.TopContainer.AddControl(button);
        screen.SetFocus("name");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        Assert.Equal("true", component.Find(".gridfs").GetAttribute("data-clipboard-enabled"));
        Assert.Equal("true", component.Find(".gridfs").GetAttribute("data-edit-history-enabled"));
        Assert.Contains("Control+C", component.Find(".gridfs").GetAttribute("aria-keyshortcuts"));
        Assert.Contains("Control+Z", component.Find(".gridfs").GetAttribute("aria-keyshortcuts"));

        await component.InvokeAsync(component.Instance.SelectAllForClipboard);
        Assert.Equal("Alex", component.Instance.CopySelectionForClipboard());

        await component.InvokeAsync(component.Instance.CutSelectionForClipboard);
        Assert.Equal("", textBox.Value);

        await component.InvokeAsync(() => component.Instance.PasteFromClipboard("Jo"));
        Assert.Equal("Jo", textBox.Value);
        Assert.Contains(">J</div>", component.Markup);
        Assert.Contains(">o</div>", component.Markup);

        await component.InvokeAsync(component.Instance.UndoTextEdit);
        Assert.Equal("", textBox.Value);
        await component.InvokeAsync(component.Instance.RedoTextEdit);
        Assert.Equal("Jo", textBox.Value);

        screen.SetFocus("save");
        component.Render(parameters => parameters.Add(instance => instance.screen, screen));
        Assert.Equal("false", component.Find(".gridfs").GetAttribute("data-clipboard-enabled"));
        Assert.Equal("false", component.Find(".gridfs").GetAttribute("data-edit-history-enabled"));
    }

    [Fact]
    public async Task PasswordBoxAppliesClipboardPermissions()
    {
        var screen = new Screen(12, 4);
        var password = new PasswordBox("password", "secret", 0, 0, 10, Color.White, Color.Black);
        screen.TopContainer.AddControl(password);
        screen.SetFocus("password");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var grid = component.Find(".gridfs");

        Assert.Equal("false", grid.GetAttribute("data-clipboard-copy-enabled"));
        Assert.Equal("true", grid.GetAttribute("data-clipboard-paste-enabled"));

        await component.InvokeAsync(component.Instance.SelectAllForClipboard);
        Assert.Null(component.Instance.CopySelectionForClipboard());
        await component.InvokeAsync(component.Instance.CutSelectionForClipboard);
        Assert.Equal("secret", password.Value);

        await component.InvokeAsync(() => component.Instance.PasteFromClipboard("new"));
        Assert.Equal("new", password.Value);

        password.AllowCopy = true;
        component.Render(parameters => parameters.Add(instance => instance.screen, screen));
        Assert.Equal("true", component.Find(".gridfs").GetAttribute("data-clipboard-copy-enabled"));
        await component.InvokeAsync(component.Instance.SelectAllForClipboard);
        Assert.Equal("new", component.Instance.CopySelectionForClipboard());

        password.AllowPaste = false;
        component.Render(parameters => parameters.Add(instance => instance.screen, screen));
        Assert.Equal("false", component.Find(".gridfs").GetAttribute("data-clipboard-paste-enabled"));
        await component.InvokeAsync(() => component.Instance.PasteFromClipboard("blocked"));
        Assert.Equal("new", password.Value);
    }

    [Fact]
    public async Task ComponentRoutesTabNavigationToTheFocusedTabControl()
    {
        var screen = new Screen(30, 12);
        var tabs = new TabControl("tabs", 1, 1, 26, 9, Color.White, Color.Black);
        screen.TopContainer.AddContainer(tabs);
        TabPage first = tabs.AddTab("firstTab", "First");
        TabPage second = tabs.AddTab("secondTab", "Second");
        tabs.AddTab("aboutTab", "About");
        var firstInput = new TextBox("firstInput", "", 1, 1, 8, Color.White, Color.Black);
        var secondInput = new TextBox("secondInput", "", 1, 1, 8, Color.White, Color.Black);
        first.AddControl(firstInput);
        second.AddControl(secondInput);
        screen.SetFocus("firstInput");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        Assert.Equal("true", component.Find(".gridfs").GetAttribute("data-tab-navigation-enabled"));
        Assert.Contains("Control+Tab", component.Find(".gridfs").GetAttribute("aria-keyshortcuts"));
        Assert.Contains("Alt+PageDown", component.Find(".gridfs").GetAttribute("aria-keyshortcuts"));

        await component.InvokeAsync(() => component.Instance.MoveTab(false));
        Assert.Equal(1, tabs.SelectedIndex);
        Assert.True(secondInput.Focus);
        Assert.Contains("Tab selected: Second", component.Find("[role=status]").TextContent);

        await component.InvokeAsync(() => component.Instance.MoveTab(false));
        Assert.Equal(2, tabs.SelectedIndex);
        Assert.Null(tabs.GetCurrentFocusControl());
        Assert.Equal("true", component.Find(".gridfs").GetAttribute("data-tab-navigation-enabled"));
        Assert.Contains("Tab selected: About", component.Find("[role=status]").TextContent);

        await component.InvokeAsync(() => component.Instance.MoveTab(false));
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.True(firstInput.Focus);
    }

    [Fact]
    public void ComponentExposesAndTogglesCommandPalette()
    {
        var screen = new Screen(30, 8);
        var palette = new CommandPalette(
            "commands",
            new[] { new CommandPaletteItem("build", "Build") },
            1, 1, 20,
            Color.White, Color.Black);
        screen.TopContainer.AddControl(palette);

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        var grid = component.Find(".gridfs");
        Assert.Equal("true", grid.GetAttribute("data-command-palette-enabled"));
        Assert.Contains("F2", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("Control+K", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("Meta+K", grid.GetAttribute("aria-keyshortcuts"));
        Assert.Contains("F2 or Control+K or Command+K to open a command palette", component.Markup);
        Assert.Contains("Command+K", component.Markup);

        grid.KeyDown(new KeyboardEventArgs { Key = "F2" });

        Assert.True(palette.IsOpen);
        Assert.Contains("Command palette opened: Command", component.Find("[role=status]").TextContent);
        Assert.Contains("Build", component.Find("pre.blazortui-visually-hidden").TextContent);
    }

    [Fact]
    public void ComponentUsesConfiguredCommandPaletteShortcut()
    {
        var screen = new Screen(30, 8);
        screen.Shortcuts.SetBindings(TuiShortcutAction.ToggleCommandPalette, "F9");
        var palette = new CommandPalette(
            "commands",
            new[] { new CommandPaletteItem("build", "Build") },
            1, 1, 20,
            Color.White, Color.Black);
        screen.TopContainer.AddControl(palette);

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        var grid = component.Find(".gridfs");
        Assert.Contains("F9", grid.GetAttribute("aria-keyshortcuts"));
        Assert.DoesNotContain("Control+K", grid.GetAttribute("aria-keyshortcuts"));

        grid.KeyDown(new KeyboardEventArgs { Key = "F2" });
        Assert.False(palette.IsOpen);

        grid.KeyDown(new KeyboardEventArgs { Key = "F9" });
        Assert.True(palette.IsOpen);
        Assert.Contains("F9 to open a command palette", component.Markup);
    }

    [Fact]
    public async Task ComponentUsesConfiguredModifiedTabShortcut()
    {
        var screen = new Screen(30, 12);
        screen.Shortcuts.SetBindings(TuiShortcutAction.SelectNextTab, "Alt+N");
        screen.Shortcuts.SetBindings(TuiShortcutAction.SelectPreviousTab, "Alt+P");
        var tabs = new TabControl("tabs", 1, 1, 26, 9, Color.White, Color.Black);
        screen.TopContainer.AddContainer(tabs);
        TabPage first = tabs.AddTab("firstTab", "First");
        TabPage second = tabs.AddTab("secondTab", "Second");
        first.AddControl(new TextBox("firstInput", "", 1, 1, 8, Color.White, Color.Black));
        second.AddControl(new TextBox("secondInput", "", 1, 1, 8, Color.White, Color.Black));
        screen.SetFocus("firstInput");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var grid = component.Find(".gridfs");

        grid.KeyDown(new KeyboardEventArgs { Key = "n", AltKey = true });
        Assert.Equal(0, tabs.SelectedIndex);

        await component.InvokeAsync(() => component.Instance.HandleShortcut(nameof(TuiShortcutAction.SelectNextTab)));
        Assert.Equal(1, tabs.SelectedIndex);

        grid.KeyDown(new KeyboardEventArgs { Key = "p", AltKey = true });
        Assert.Equal(1, tabs.SelectedIndex);

        await component.InvokeAsync(() => component.Instance.HandleShortcut(nameof(TuiShortcutAction.SelectPreviousTab)));
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Contains("Alt+N", grid.GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public async Task ComponentUsesConfiguredClipboardShortcut()
    {
        var screen = new Screen(12, 4);
        screen.Shortcuts.SetBindings(TuiShortcutAction.SelectAll, "Control+E");
        var textBox = new TextBox("name", "Alex", 0, 0, 8, Color.White, Color.Black);
        screen.TopContainer.AddControl(textBox);
        screen.SetFocus("name");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));
        var grid = component.Find(".gridfs");

        grid.KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });
        Assert.False(textBox.HasSelection);

        grid.KeyDown(new KeyboardEventArgs { Key = "e", CtrlKey = true });
        Assert.False(textBox.HasSelection);

        await component.InvokeAsync(() => component.Instance.HandleShortcut(nameof(TuiShortcutAction.SelectAll)));
        Assert.True(textBox.HasSelection);
        Assert.Equal("Alex", textBox.SelectedText);
        Assert.Contains("Control+E", grid.GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public async Task ControlsExampleCommandPaletteHasRoomForVisibleText()
    {
        var page = Render<global::SampleApp.Pages.Examples.ControlsAndEvents>();
        IRenderedComponent<global::BlazorTUI.BlazorTUI> terminal =
            page.FindComponent<global::BlazorTUI.BlazorTUI>();

        await terminal.InvokeAsync(terminal.Instance.ToggleCommandPalette);

        string accessibleText = terminal.Find("pre.blazortui-visually-hidden").TextContent;
        Assert.Contains("Type a command", accessibleText);
        Assert.Contains("Focus name - Move focus", accessibleText);
        Assert.Contains("Greet - Show message", accessibleText);
    }
}
