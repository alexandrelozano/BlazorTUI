using System.Drawing;
using System.Reflection;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class PublicApiTests
{
    [Fact]
    public void PublicApiDoesNotExposeLegacyLowercaseMembers()
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

        Assert.Empty(actual);
    }

    [Fact]
    public void RecommendedPropertiesExposeState()
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
        Assert.Equal("saveButton", button.Name);
        Assert.Equal((short)10, button.Width);
        Assert.Equal(Color.Yellow, button.ForeColor);
        Assert.Equal("X", screen.Rows[0].Cells[0].Character);
        Assert.Same(button, Assert.Single(screen.TopContainer.Controls));
    }

    [Fact]
    public void ControlEventsWorkForMouseKeyboardAndFocus()
    {
        var container = new Container("root") { Width = 20, Height = 10 };
        var button = new Button("save", "Save", 1, 1, 8, Color.White, Color.DarkGreen);
        int clicks = 0;
        int gotFocus = 0;
        int lostFocus = 0;
        button.Clicked += (_, _) => clicks++;
        button.GotFocus += (_, _) => gotFocus++;
        button.LostFocus += (_, _) => lostFocus++;
        container.AddControl(button);

        button.Click(0, 0);
        button.KeyDown("Enter", false);
        button.IsFocused = false;

        Assert.Equal(2, clicks);
        Assert.Equal(1, gotFocus);
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
    public void MenuItemRaisesStandardEvent()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File", 'F');
        var item = new MenuItem("Open", MenuItem.MenuItemType.Item, 'O');
        int clicks = 0;
        item.Clicked += (_, _) => clicks++;
        menu.AddItem(item);
        menuBar.AddMenu(menu);
        screen.MenuBar = menuBar;

        screen.KeyDown("Alt", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("Enter", false);

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
