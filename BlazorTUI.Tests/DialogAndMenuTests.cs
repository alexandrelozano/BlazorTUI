using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class DialogAndMenuTests
{
    [Fact]
    public void DialogIsCenteredRenderedAndRemovedFromModalStack()
    {
        var screen = new Screen(20, 10);
        var dialog = new Dialog("dialog", "TITLE", 6, 4, BorderStyle.line, Color.Yellow, Color.DarkBlue, screen);

        dialog.Show();
        screen.Render();

        Assert.Same(dialog, Assert.Single(screen.dialogs));
        Assert.True(dialog.Visible);
        Assert.Equal((short)7, dialog.X);
        Assert.Equal((short)3, dialog.Y);
        Assert.Equal("┌", screen.rows[3].Cells[7].character);

        dialog.Close();
        Assert.Empty(screen.dialogs);
        Assert.False(dialog.Visible);
    }

    [Fact]
    public void MessageBoxButtonClosesDialogAndSetsResult()
    {
        var screen = new Screen(40, 20);
        var messageBox = new MessageBox(
            "Saved",
            "Result",
            MessageBox.Buttons.OKOnly,
            BorderStyle.line,
            Color.White,
            Color.DarkGreen,
            screen);

        messageBox.Show();
        Dialog dialog = Assert.Single(screen.dialogs);
        Button button = Assert.Single(dialog.controls.OfType<Button>());
        button.Click(0, 0);

        Assert.Empty(screen.dialogs);
        Assert.Equal(MessageBox.Result.OK, messageBox.result);
    }

    [Fact]
    public void MenuCanBeOpenedAndActivatedFromKeyboard()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File", 'F');
        bool invoked = false;
        menu.menuItems.Add(new MenuItem("Open", MenuItem.MenuItemType.Item, 'O') { OnClick = () => invoked = true });
        menuBar.menus.Add(menu);
        screen.menuBar = menuBar;

        screen.KeyDown("Alt", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("Enter", false);

        Assert.True(invoked);
        Assert.False(menu.opended);
        Assert.False(menuBar.showShortCutkeys);
    }

    [Fact]
    public void ClickingOutsideAnOpenMenuClosesItAndReturnsUnhandled()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File", 'F');
        menu.AddItem(new MenuItem("Open", MenuItem.MenuItemType.Item, 'O'));
        menu.AddItem(new MenuItem("Save", MenuItem.MenuItemType.Item, 'S'));
        menuBar.AddMenu(menu);

        Assert.True(menuBar.Click(0, 0));
        bool handled = menuBar.Click(10, 5);

        Assert.False(handled);
        Assert.False(menu.IsOpen);
        Assert.False(menuBar.ShowShortcutKeys);
    }

    [Fact]
    public void ClickingImmediatelyBelowMenuItemsDoesNotAccessPastTheCollection()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File");
        menu.AddItem(new MenuItem("Open", MenuItem.MenuItemType.Item));
        menu.AddItem(new MenuItem("Save", MenuItem.MenuItemType.Item));
        menuBar.AddMenu(menu);
        menuBar.Click(0, 0);

        bool handled = menuBar.Click(0, 3);

        Assert.False(handled);
        Assert.False(menu.IsOpen);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ClickingAMenuItemInvokesTheCorrectEntry(short row, int expectedIndex)
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File");
        int invokedIndex = -1;
        var open = new MenuItem("Open", MenuItem.MenuItemType.Item);
        open.Clicked += (_, _) => invokedIndex = 0;
        var save = new MenuItem("Save", MenuItem.MenuItemType.Item);
        save.Clicked += (_, _) => invokedIndex = 1;
        menu.AddItem(open);
        menu.AddItem(save);
        menuBar.AddMenu(menu);
        menuBar.Click(0, 0);

        bool handled = menuBar.Click(0, row);

        Assert.True(handled);
        Assert.Equal(expectedIndex, invokedIndex);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void OutsideMenuClickCanContinueToTheUnderlyingControl()
    {
        var screen = new Screen(20, 10);
        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var menu = new Menu("File");
        menu.AddItem(new MenuItem("Open", MenuItem.MenuItemType.Item));
        menuBar.AddMenu(menu);
        bool buttonClicked = false;
        var button = new Button("underlying", "Open", 10, 5, 8, Color.White, Color.Black);
        button.Clicked += (_, _) => buttonClicked = true;
        screen.TopContainer.AddControl(button);
        menuBar.Click(0, 0);

        bool handled = menuBar.Click(10, 5);
        if (!handled)
            screen.TopContainer.Click(10, 5);

        Assert.True(buttonClicked);
        Assert.False(menu.IsOpen);
    }
}
