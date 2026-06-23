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
}
