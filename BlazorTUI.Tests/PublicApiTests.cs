using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class PublicApiTests
{
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
