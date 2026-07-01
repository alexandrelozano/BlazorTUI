using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class TransientUiTests
{
    [Fact]
    public void ContextMenuOpensFromContextClickAndInvokesItem()
    {
        var screen = new Screen(30, 10);
        var button = new Button("actions", "Actions", 2, 2, 9, Color.White, Color.DarkGreen);
        screen.TopContainer.AddControl(button);

        bool invoked = false;
        var refreshItem = new ContextMenuItem("refresh", "Refresh");
        refreshItem.Clicked += (_, _) => invoked = true;
        var menu = new ContextMenu(
            "actionsMenu",
            new[]
            {
                refreshItem,
                new ContextMenuItem("separator", "", ContextMenuItemType.Separator),
                new ContextMenuItem("disabled", "Disabled") { Enabled = false }
            },
            0, 0, 10,
            Color.Yellow, Color.Black,
            new[] { "actions" });
        screen.TopContainer.AddControl(menu);

        bool opened = screen.TopContainer.ContextClick(3, 2);
        screen.Render();

        Assert.True(opened);
        Assert.True(menu.IsOpen);
        Assert.Contains("Refresh", Read(screen, 3, 2, 10));

        screen.TopContainer.Click(3, 2);

        Assert.True(invoked);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void ContextMenuOpensFromKeyboardAndSupportsSelection()
    {
        var screen = new Screen(30, 10);
        var button = new Button("actions", "Actions", 2, 2, 9, Color.White, Color.DarkGreen);
        screen.TopContainer.AddControl(button);

        string selected = "";
        var openItem = new ContextMenuItem("open", "Open");
        openItem.Clicked += (_, _) => selected = "open";
        var archiveItem = new ContextMenuItem("archive", "Archive");
        archiveItem.Clicked += (_, _) => selected = "archive";
        var menu = new ContextMenu(
            "actionsMenu",
            new[]
            {
                openItem,
                archiveItem
            },
            0, 0, 10,
            Color.Yellow, Color.Black,
            new[] { "actions" });
        screen.TopContainer.AddControl(menu);
        screen.SetFocus("actions");

        screen.KeyDown("F10", true);
        screen.KeyDown("ArrowDown", false);
        screen.KeyDown("Enter", false);

        Assert.Equal("archive", selected);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void TooltipRendersWhenTargetHasFocus()
    {
        var screen = new Screen(40, 8);
        var button = new Button("save", "Save", 2, 2, 8, Color.White, Color.DarkGreen);
        var tooltip = new Tooltip(
            "saveTip", "Tooltip text", "save",
            12, 2, 14,
            Color.Black, Color.Cyan);
        screen.TopContainer.AddControl(button);
        screen.TopContainer.AddControl(tooltip);

        screen.SetFocus("save");
        screen.Render();

        Assert.Contains("Tooltip text", Read(screen, 12, 2, 14));
    }

    [Fact]
    public void PopoverClosesWhenClickingOutside()
    {
        var screen = new Screen(40, 12);
        var popover = new Popover(
            "details", "INFO", "Short text",
            5, 3, 16, 5,
            Color.Yellow, Color.DarkGreen);
        screen.TopContainer.AddControl(popover);

        popover.Show();
        screen.Render();

        Assert.True(popover.IsOpen);
        Assert.Contains("INFO", Read(screen, 5, 3, 16));

        screen.TopContainer.Click(0, 0);

        Assert.False(popover.IsOpen);
    }

    [Fact]
    public void ToastRendersVisibleItemsAndCanDismissThem()
    {
        var screen = new Screen(40, 8);
        var toast = new Toast(
            "toast", 2, 2, 20, 2,
            Color.Black, Color.Cyan);
        screen.TopContainer.AddControl(toast);

        toast.AddToast("saved", "Saved");
        toast.AddToast("queued", "Queued");
        screen.Render();

        Assert.Contains("Saved", Read(screen, 2, 2, 20));
        Assert.Contains("Queued", Read(screen, 2, 3, 20));

        Assert.True(toast.Dismiss("saved"));
        Assert.Null(toast.GetToast("saved"));
    }

    [Fact]
    public void ModalPanelClosesWithEscapeAndRaisesClosedEvent()
    {
        var screen = new Screen(40, 16);
        var modal = new ModalPanel(
            "modal", "MODAL", 20, 8,
            BorderStyle.Line, Color.Yellow, Color.DarkMagenta, screen);
        string reason = "";
        modal.Closed += (_, args) => reason = args.Reason;

        modal.Show();
        modal.Show();
        Assert.Single(screen.Dialogs);

        screen.KeyDown("Escape", false);

        Assert.False(modal.Visible);
        Assert.Empty(screen.Dialogs);
        Assert.Equal("Escape", reason);
    }

    [Fact]
    public void StatePersistenceRestoresTransientControlState()
    {
        var screen = CreateTransientStateScreen();
        ContextMenu menu = (ContextMenu)screen.TopContainer.GetControl("menu")!;
        Tooltip tooltip = (Tooltip)screen.TopContainer.GetControl("tooltip")!;
        Popover popover = (Popover)screen.TopContainer.GetControl("popover")!;
        Toast toast = (Toast)screen.TopContainer.GetControl("toast")!;

        menu.OpenAt(7, 4);
        menu.SelectedIndex = 1;
        tooltip.Show();
        popover.Show();
        toast.AddToast("saved", "Saved");

        TuiScreenState state = screen.ExportState();

        var restoredScreen = CreateTransientStateScreen();
        restoredScreen.RestoreState(state);

        ContextMenu restoredMenu = (ContextMenu)restoredScreen.TopContainer.GetControl("menu")!;
        Tooltip restoredTooltip = (Tooltip)restoredScreen.TopContainer.GetControl("tooltip")!;
        Popover restoredPopover = (Popover)restoredScreen.TopContainer.GetControl("popover")!;
        Toast restoredToast = (Toast)restoredScreen.TopContainer.GetControl("toast")!;

        Assert.True(restoredMenu.IsOpen);
        Assert.Equal(1, restoredMenu.SelectedIndex);
        Assert.Equal(7, restoredMenu.X);
        Assert.Equal(4, restoredMenu.Y);
        Assert.True(restoredTooltip.IsOpen);
        Assert.True(restoredPopover.IsOpen);
        Assert.Equal("Saved", restoredToast.Items.Single().Text);
    }

    [Fact]
    public void ValidatesArgumentsAndDuplicateNames()
    {
        Assert.Throws<ArgumentException>(() => new ContextMenuItem("", "Invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ContextMenu("menu", Array.Empty<ContextMenuItem>(), 0, 0, 3, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Tooltip("tip", "Tip", "target", 0, 0, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Popover("popover", "Title", "Text", 0, 0, 3, 3, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Toast("toast", 0, 0, 1, 0, Color.White, Color.Black));

        var toast = new Toast("toast", 0, 0, 10, 1, Color.White, Color.Black);
        toast.AddToast("saved", "Saved");
        Assert.Throws<InvalidOperationException>(() => toast.AddToast("saved", "Saved again"));
    }

    private static Screen CreateTransientStateScreen()
    {
        var screen = new Screen(40, 12);
        screen.TopContainer.AddControl(new Button("target", "Target", 2, 2, 8, Color.White, Color.DarkGreen));
        screen.TopContainer.AddControl(new ContextMenu(
            "menu",
            new[]
            {
                new ContextMenuItem("open", "Open"),
                new ContextMenuItem("archive", "Archive")
            },
            0, 0, 10,
            Color.Yellow, Color.Black,
            new[] { "target" }));
        screen.TopContainer.AddControl(new Tooltip(
            "tooltip", "Tooltip text", "target",
            12, 2, 14,
            Color.Black, Color.Cyan));
        screen.TopContainer.AddControl(new Popover(
            "popover", "INFO", "Short text",
            4, 5, 16, 5,
            Color.Yellow, Color.DarkGreen));
        screen.TopContainer.AddControl(new Toast(
            "toast", 22, 5, 12, 2,
            Color.Black, Color.Cyan));

        return screen;
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
