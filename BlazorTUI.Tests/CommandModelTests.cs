using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class CommandModelTests
{
    [Fact]
    public void CommandRegistryExecutesCommandsAndRejectsDuplicates()
    {
        int executed = 0;
        var registry = new TuiCommandRegistry();
        var save = registry.AddCommand("save", "Save", "Save changes", _ => executed++);
        save.SetShortcuts("Control+S", "Control+S");

        Assert.Single(save.Shortcuts);
        Assert.Equal("Control+S", save.ShortcutText);
        Assert.True(registry.Execute("save"));
        Assert.Equal(1, executed);

        Assert.Throws<InvalidOperationException>(() =>
            registry.AddCommand(new TuiCommand("save", "Duplicate")));

        var duplicateShortcut = new TuiCommand("store", "Store");
        duplicateShortcut.SetShortcuts("Control+S");
        Assert.Throws<InvalidOperationException>(() => registry.AddCommand(duplicateShortcut));

        save.Enabled = false;
        Assert.False(registry.Execute("save"));
        save.Enabled = true;
        save.Visible = false;
        Assert.False(registry.Execute("save"));
    }

    [Fact]
    public void BoundCommandControlsShareLabelDescriptionShortcutsAndExecution()
    {
        int executed = 0;
        var command = new TuiCommand("save", "Save", "Save current record", _ => executed++);
        command.SetShortcuts("Control+S");
        var registry = new TuiCommandRegistry();
        registry.AddCommand(command);

        var screen = new Screen(80, 16);
        var button = new Button("saveButton", command, 1, 1, 10, Color.White, Color.DarkGreen);
        var palette = new CommandPalette("commands", registry, 1, 3, 24, Color.White, Color.Black);
        var contextMenu = new ContextMenu(
            "contextMenu",
            new[] { command },
            1,
            5,
            18,
            Color.White,
            Color.Black,
            new[] { "saveButton" });
        var status = new StatusBar("status", "Ready", 0, 14, 80, Color.Black, Color.Cyan);
        StatusBarItem statusItem = status.AddCommand(command);
        var tooltip = new Tooltip(
            "saveTooltip",
            command,
            "saveButton",
            14,
            1,
            24,
            Color.Black,
            Color.Cyan);

        var menuBar = new MenuBar(Color.White, Color.DarkBlue, screen);
        var fileMenu = new Menu("File", 'F');
        fileMenu.AddCommand(command, 'S');
        menuBar.AddMenu(fileMenu);
        screen.MenuBar = menuBar;

        screen.TopContainer.AddControl(button);
        screen.TopContainer.AddControl(palette);
        screen.TopContainer.AddControl(contextMenu);
        screen.TopContainer.AddControl(status);
        screen.TopContainer.AddControl(tooltip);

        Assert.True(button.Click(0, 0));
        Assert.True(palette.ExecuteCommand("save"));
        contextMenu.OpenAt(1, 5);
        Assert.True(contextMenu.Click(0, 0));
        menuBar.KeyDown("F", false);
        menuBar.KeyDown("S", false);

        Assert.Equal(4, executed);

        command.Label = "Store";
        command.Description = "Store current record";

        Assert.Equal("Store", button.Text);
        Assert.Equal("Store", palette.GetCommand("save")!.Title);
        Assert.Equal("Control+S Store", statusItem.Text);
        Assert.Equal("Store current record", tooltip.Text);

        tooltip.Show();
        screen.Render();

        Assert.Contains("Control+S Store", Read(screen, 0, 14, 80));
        Assert.Contains("Store current record", Read(screen, 14, 1, 24));
    }

    [Fact]
    public void BoundCommandControlsRespectEnabledAndVisibleState()
    {
        int executed = 0;
        var command = new TuiCommand("archive", "Archive", "Archive current row", _ => executed++);
        var registry = new TuiCommandRegistry();
        registry.AddCommand(command);

        var screen = new Screen(50, 10);
        var button = new Button("archiveButton", command, 1, 1, 12, Color.White, Color.DarkGreen);
        var palette = new CommandPalette("commands", registry, 1, 3, 24, Color.White, Color.Black);
        var contextMenu = new ContextMenu("contextMenu", new[] { command }, 1, 5, 18, Color.White, Color.Black);
        var status = new StatusBar("status", "Ready", 0, 8, 50, Color.Black, Color.Cyan);
        status.AddCommand(command);

        screen.TopContainer.AddControl(button);
        screen.TopContainer.AddControl(palette);
        screen.TopContainer.AddControl(contextMenu);
        screen.TopContainer.AddControl(status);

        command.Enabled = false;

        Assert.False(button.Click(0, 0));
        Assert.False(palette.ExecuteCommand("archive"));
        contextMenu.OpenAt(1, 5);
        Assert.True(contextMenu.Click(0, 0));
        Assert.Equal(0, executed);

        command.Enabled = true;
        command.Visible = false;
        palette.SearchText = "";
        contextMenu.Close();
        contextMenu.OpenAt(1, 5);
        screen.Render();

        Assert.False(button.Click(0, 0));
        Assert.False(palette.ExecuteCommand("archive"));
        Assert.False(contextMenu.IsOpen);
        Assert.DoesNotContain("Archive", Read(screen, 0, 8, 50));
        Assert.Equal(0, executed);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
