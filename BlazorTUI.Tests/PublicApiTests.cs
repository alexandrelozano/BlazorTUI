using System.Drawing;
using System.Reflection;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class PublicApiTests
{
    private static readonly string[] ApprovedLowercaseCompatibilityMembers =
    [
        "BlazorTUI.BlazorTUI.Property:screen",
        "BlazorTUI.TUI.BorderStyle.Field:doubleline",
        "BlazorTUI.TUI.BorderStyle.Field:line",
        "BlazorTUI.TUI.BorderStyle.Field:none",
        "BlazorTUI.TUI.BorderStyle.Field:solid",
        "BlazorTUI.TUI.Cell.Property:backgroundColor",
        "BlazorTUI.TUI.Cell.Property:backgroundImage",
        "BlazorTUI.TUI.Cell.Property:character",
        "BlazorTUI.TUI.Cell.Property:foreColor",
        "BlazorTUI.TUI.Cell.Property:scaleX",
        "BlazorTUI.TUI.Cell.Property:scaleY",
        "BlazorTUI.TUI.Cell.Property:textDecoration",
        "BlazorTUI.TUI.Cell.Property:visible",
        "BlazorTUI.TUI.Cell.Property:x",
        "BlazorTUI.TUI.Cell.Property:y",
        "BlazorTUI.TUI.CheckBox.Property:value",
        "BlazorTUI.TUI.ColorPicker.Field:backgroundColor",
        "BlazorTUI.TUI.ColorPicker.Field:color",
        "BlazorTUI.TUI.ColorPicker.Field:foreColor",
        "BlazorTUI.TUI.ColorPicker.Field:showColorName",
        "BlazorTUI.TUI.ColorPicker.Method:bttCancel_OnClick",
        "BlazorTUI.TUI.ColorPicker.Method:bttDlgColor_OnClick",
        "BlazorTUI.TUI.Container.Property:containers",
        "BlazorTUI.TUI.Container.Property:controls",
        "BlazorTUI.TUI.Container.Property:height",
        "BlazorTUI.TUI.Container.Property:name",
        "BlazorTUI.TUI.Container.Property:parent",
        "BlazorTUI.TUI.Container.Property:width",
        "BlazorTUI.TUI.Control.Property:backgroundColor",
        "BlazorTUI.TUI.Control.Property:container",
        "BlazorTUI.TUI.Control.Property:foreColor",
        "BlazorTUI.TUI.Control.Property:height",
        "BlazorTUI.TUI.Control.Property:name",
        "BlazorTUI.TUI.Control.Property:width",
        "BlazorTUI.TUI.DateBox.Field:value",
        "BlazorTUI.TUI.DateBox.Property:dateFormat",
        "BlazorTUI.TUI.Dialog.Property:backgroundColor",
        "BlazorTUI.TUI.Dialog.Property:borderStyle",
        "BlazorTUI.TUI.Dialog.Property:foreColor",
        "BlazorTUI.TUI.Dialog.Property:title",
        "BlazorTUI.TUI.Frame+BorderStyle.Field:doubleline",
        "BlazorTUI.TUI.Frame+BorderStyle.Field:line",
        "BlazorTUI.TUI.Frame+BorderStyle.Field:none",
        "BlazorTUI.TUI.Frame+BorderStyle.Field:solid",
        "BlazorTUI.TUI.Frame.Property:backgroundColor",
        "BlazorTUI.TUI.Frame.Property:borderStyle",
        "BlazorTUI.TUI.Frame.Property:foreColor",
        "BlazorTUI.TUI.Frame.Property:title",
        "BlazorTUI.TUI.GridView+GridColumn.Field:editorOptions",
        "BlazorTUI.TUI.GridView+GridColumn.Field:isEditable",
        "BlazorTUI.TUI.GridView+GridColumn.Field:title",
        "BlazorTUI.TUI.GridView+GridColumn.Field:width",
        "BlazorTUI.TUI.GridView+GridRow.Field:cells",
        "BlazorTUI.TUI.ListBox.Field:items",
        "BlazorTUI.TUI.ListBox.Field:itemsSelected",
        "BlazorTUI.TUI.Menu.Field:menuItems",
        "BlazorTUI.TUI.Menu.Field:opended",
        "BlazorTUI.TUI.Menu.Field:selectedItem",
        "BlazorTUI.TUI.Menu.Field:shortCutKey",
        "BlazorTUI.TUI.Menu.Field:text",
        "BlazorTUI.TUI.MenuBar.Field:backgroundColor",
        "BlazorTUI.TUI.MenuBar.Field:foreColor",
        "BlazorTUI.TUI.MenuBar.Field:menus",
        "BlazorTUI.TUI.MenuBar.Field:showShortCutkeys",
        "BlazorTUI.TUI.MenuBar.Field:visible",
        "BlazorTUI.TUI.MenuItem.Field:menuItemType",
        "BlazorTUI.TUI.MenuItem.Field:shortCutKey",
        "BlazorTUI.TUI.MenuItem.Field:text",
        "BlazorTUI.TUI.MessageBox.Property:buttons",
        "BlazorTUI.TUI.MessageBox.Property:result",
        "BlazorTUI.TUI.NumericBox.Field:value",
        "BlazorTUI.TUI.ProgressBar.Field:value",
        "BlazorTUI.TUI.Row.Property:y",
        "BlazorTUI.TUI.Screen.Field:dialogs",
        "BlazorTUI.TUI.Screen.Field:menuBar",
        "BlazorTUI.TUI.Screen.Field:rows",
        "BlazorTUI.TUI.Screen.Property:height",
        "BlazorTUI.TUI.Screen.Property:topContainer",
        "BlazorTUI.TUI.Screen.Property:width",
        "BlazorTUI.TUI.TextArea.Field:maxLines",
        "BlazorTUI.TUI.TextArea.Field:maxTextWidth",
        "BlazorTUI.TUI.TextArea.Property:value",
        "BlazorTUI.TUI.TextBox.Property:value",
        "BlazorTUI.TUI.TimeBox.Field:value"
    ];

    [Fact]
    public void LowercaseCompatibilitySurfaceIsFrozenForVersionOne()
    {
        string[] actual = typeof(Screen).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace is "BlazorTUI" or "BlazorTUI.TUI")
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(member => member.MemberType is MemberTypes.Field or MemberTypes.Property or MemberTypes.Event or MemberTypes.Method)
                .Where(member => member.Name != "value__" && !member.Name.StartsWith("op_", StringComparison.Ordinal))
                .Where(member => !member.Name.StartsWith("get_", StringComparison.Ordinal) &&
                    !member.Name.StartsWith("set_", StringComparison.Ordinal) &&
                    !member.Name.StartsWith("add_", StringComparison.Ordinal) &&
                    !member.Name.StartsWith("remove_", StringComparison.Ordinal))
                .Where(member => member.Name.Length > 0 && char.IsLower(member.Name[0]))
                .Select(member => $"{type.FullName}.{member.MemberType}:{member.Name}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedLowercaseCompatibilityMembers, actual);
    }

    [Fact]
    public void RecommendedPropertiesRemainSynchronizedWithLegacyMembers()
    {
        var screen = new Screen(20, 10);
        var button = new Button("save", "Save", 1, 2, 8, Color.White, Color.DarkGreen);
        screen.TopContainer.AddControl(button);

        button.Name = "saveButton";
        button.Width = 10;
        button.ForeColor = Color.Yellow;
        screen.Rows[0].Cells[0].Character = "X";

        Assert.Equal((short)20, screen.Width);
        Assert.Equal((short)10, screen.Height);
        Assert.Same(screen.topContainer, screen.TopContainer);
        Assert.Equal("saveButton", button.name);
        Assert.Equal((short)10, button.width);
        Assert.Equal(Color.Yellow, button.foreColor);
        Assert.Equal("X", screen.rows[0].Cells[0].character);
        Assert.Same(button, Assert.Single(screen.TopContainer.Controls));
    }

    [Fact]
    public void ControlEventsWorkForMouseKeyboardAndFocus()
    {
        var container = new Container("root") { Width = 20, Height = 10 };
        var button = new Button("save", "Save", 1, 1, 8, Color.White, Color.DarkGreen);
        int legacyClicks = 0;
        int clicks = 0;
        int legacyFocus = 0;
        int gotFocus = 0;
        int lostFocus = 0;
        button.OnClick = _ => legacyClicks++;
        button.OnFocus = () => legacyFocus++;
        button.Clicked += (_, _) => clicks++;
        button.GotFocus += (_, _) => gotFocus++;
        button.LostFocus += (_, _) => lostFocus++;
        container.AddControl(button);

        button.Click(0, 0);
        button.KeyDown("Enter", false);
        button.IsFocused = false;

        Assert.Equal(2, clicks);
        Assert.Equal(2, legacyClicks);
        Assert.Equal(1, gotFocus);
        Assert.Equal(1, legacyFocus);
        Assert.Equal(1, lostFocus);
    }

    [Fact]
    public void ModernSelectionConstructorUsesClickedEvent()
    {
        var container = new Container("root") { Width = 20, Height = 10 };
        var checkBox = new CheckBox(
            "details", "Include details", 1, 1, 18, Color.White, Color.Black);
        int clicks = 0;
        checkBox.Clicked += (_, _) => clicks++;
        container.AddControl(checkBox);

        checkBox.KeyDown("Space", false);

        Assert.True(checkBox.Value);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void MenuItemRaisesLegacyAndStandardEvents()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File", 'F');
        var item = new MenuItem("Open", MenuItem.MenuItemType.Item, 'O');
        int legacyClicks = 0;
        int clicks = 0;
        item.OnClick = () => legacyClicks++;
        item.Clicked += (_, _) => clicks++;
        menu.AddItem(item);
        menuBar.AddMenu(menu);
        screen.MenuBar = menuBar;

        screen.KeyDown("Alt", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("Enter", false);

        Assert.Equal(1, legacyClicks);
        Assert.Equal(1, clicks);
        Assert.False(menu.IsOpen);
        Assert.False(menuBar.ShowShortcutKeys);
    }

    [Fact]
    public void PublicEntryPointsRejectInvalidArgumentsPrecisely()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Screen(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Screen(20, 0));
        Assert.Throws<ArgumentException>(() => new Container(" "));
        Assert.Throws<ArgumentException>(() =>
            new Button(" ", "Save", 0, 0, 8, Color.White, Color.Black));

        var container = new Container("root") { Width = 20, Height = 10 };
        Assert.Throws<ArgumentNullException>(() => container.AddControl(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            container.AddControl(new Label("empty", "", 0, 0, 0, Color.White, Color.Black)));
    }
}
