using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ShortcutTests
{
    [Fact]
    public void ShortcutMapParsesAndRebindsGestures()
    {
        var shortcuts = TuiShortcutMap.CreateDefault();

        Assert.True(shortcuts.TryGetAction(TuiKeyGesture.Parse("Control+K"), out TuiShortcutAction action));
        Assert.Equal(TuiShortcutAction.ToggleCommandPalette, action);

        shortcuts.SetBindings(TuiShortcutAction.ToggleCommandPalette, "F9");

        Assert.False(shortcuts.TryGetAction(TuiKeyGesture.Parse("Control+K"), out _));
        Assert.True(shortcuts.TryGetAction(TuiKeyGesture.Parse("F9"), out action));
        Assert.Equal(TuiShortcutAction.ToggleCommandPalette, action);
        Assert.Contains("F9", shortcuts.ToAriaKeyShortcuts());
        Assert.DoesNotContain("Control+K", shortcuts.ToAriaKeyShortcuts());
    }

    [Fact]
    public void ShortcutMapRejectsAmbiguousBindings()
    {
        var shortcuts = TuiShortcutMap.CreateDefault();

        Assert.Throws<InvalidOperationException>(() =>
            shortcuts.AddBinding(TuiShortcutAction.ControlOpen, "F2"));
    }

    [Fact]
    public void ScreenExecutesMenuAndControlShortcutActions()
    {
        var screen = new Screen(12, 4);
        bool menuInvoked = false;
        var menuBar = new MenuBar(Color.White, Color.Blue, screen);
        var fileMenu = new Menu("File");
        var openItem = new MenuItem("Open", MenuItem.MenuItemType.Item);
        openItem.Clicked += (_, _) => menuInvoked = true;
        fileMenu.AddItem(openItem);
        menuBar.AddMenu(fileMenu);
        screen.MenuBar = menuBar;

        screen.ExecuteShortcut(TuiShortcutAction.ToggleMenuShortcuts);
        screen.ExecuteShortcut(TuiShortcutAction.MenuMoveDown);
        screen.ExecuteShortcut(TuiShortcutAction.MenuActivate);

        Assert.True(menuInvoked);
    }
}
