using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class CommandPaletteTests
{
    [Fact]
    public void OpensFiltersRendersAndExecutesHighlightedCommand()
    {
        var screen = new Screen(40, 8);
        string executed = "";
        var newFileCommand = new CommandPaletteItem("newFile", "New file", "Create");
        newFileCommand.Executed += (_, _) => executed = newFileCommand.Name;
        var openFileCommand = new CommandPaletteItem("openFile", "Open file", "Load");
        openFileCommand.Executed += (_, _) => executed = openFileCommand.Name;
        var saveFileCommand = new CommandPaletteItem("saveFile", "Save file", "Write");
        saveFileCommand.Executed += (_, _) => executed = saveFileCommand.Name;
        var palette = new CommandPalette(
            "commands",
            new[]
            {
                newFileCommand,
                openFileCommand,
                saveFileCommand
            },
            1, 1, 28,
            Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(palette);
        screen.SetFocus("commands");

        palette.KeyDown("Enter", false);
        palette.KeyDown("o", false);
        screen.Render();

        Assert.True(palette.IsOpen);
        Assert.True(palette.IsPaletteOpen);
        Assert.Equal("o", palette.SearchText);
        Assert.Equal(new[] { "Open file" }, palette.FilteredCommands.Select(command => command.Title));
        Assert.StartsWith(">o", Read(screen, 1, 1, 6));
        Assert.StartsWith("Open file - Load", Read(screen, 1, 2, 20));

        palette.KeyDown("Enter", false);

        Assert.Equal("openFile", executed);
        Assert.False(palette.IsOpen);
        Assert.Equal("", palette.SearchText);
    }

    [Fact]
    public void MouseClickExecutesVisibleCommandAndRaisesEvents()
    {
        var screen = new Screen(40, 8);
        var palette = new CommandPalette(
            "commands",
            new[]
            {
                new CommandPaletteItem("build", "Build"),
                new CommandPaletteItem("test", "Test")
            },
            1, 1, 20,
            Color.White, Color.Black);
        var executed = new List<string>();
        palette.Commands[1].Executed += (_, _) => executed.Add("item");
        palette.CommandExecuted += (_, args) => executed.Add(args.CommandName);
        screen.TopContainer.AddControl(palette);

        screen.TopContainer.Click(1, 1);
        screen.TopContainer.Click(2, 3);

        Assert.Equal(new[] { "item", "test" }, executed);
        Assert.False(palette.IsOpen);
        Assert.True(palette.Focus);
    }

    [Fact]
    public void KeyboardSupportsSearchEditingAndNoResults()
    {
        var palette = new CommandPalette(
            "commands",
            new[] { new CommandPaletteItem("build", "Build") },
            0, 0, 20,
            Color.White, Color.Black)
        {
            ResetSearchOnClose = false
        };

        palette.Open();
        palette.KeyDown("x", false);
        palette.KeyDown("Delete", false);
        palette.KeyDown("b", false);
        palette.KeyDown("u", false);
        palette.KeyDown("Backspace", false);
        palette.KeyDown("Escape", false);

        Assert.False(palette.IsOpen);
        Assert.Equal("b", palette.SearchText);
        Assert.Single(palette.FilteredCommands);
    }

    [Fact]
    public void SupportsCommandMutationAndProgrammaticExecution()
    {
        var palette = new CommandPalette(
            "commands",
            Array.Empty<CommandPaletteItem>(),
            0, 0, 20,
            Color.White, Color.Black);
        string executed = "";

        CommandPaletteItem buildCommand = palette.AddCommand("build", "Build");
        buildCommand.Executed += (_, _) => executed = buildCommand.Name;
        palette.AddCommand("test", "Test");
        Assert.True(palette.ExecuteCommand("build"));
        Assert.True(palette.RemoveCommand("test"));
        palette.ClearCommands();

        Assert.Equal("build", executed);
        Assert.Empty(palette.Commands);
        Assert.Empty(palette.FilteredCommands);
    }

    [Fact]
    public void ValidatesArgumentsAndDuplicateCommands()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CommandPalette("commands", Array.Empty<CommandPaletteItem>(), 0, 0, 11, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CommandPalette("commands", Array.Empty<CommandPaletteItem>(), 0, 0, 12, Color.White, Color.Black, 0));
        Assert.Throws<ArgumentException>(() =>
            new CommandPaletteItem("bad", "One\nTwo"));
        Assert.Throws<ArgumentNullException>(() =>
            new CommandPalette(
                "commands",
                new CommandPaletteItem[] { null! },
                0, 0, 12,
                Color.White, Color.Black));
        Assert.Throws<InvalidOperationException>(() =>
            new CommandPalette(
                "commands",
                new[]
                {
                    new CommandPaletteItem("duplicate", "One"),
                    new CommandPaletteItem("duplicate", "Two")
                },
                0, 0, 12,
                Color.White, Color.Black));

        var palette = new CommandPalette(
            "commands",
            new[] { new CommandPaletteItem("build", "Build") },
            0, 0, 12,
            Color.White, Color.Black);

        Assert.Throws<InvalidOperationException>(() => palette.AddCommand("build", "Build again"));
        Assert.Throws<ArgumentNullException>(() => palette.AddCommand((CommandPaletteItem)null!));
        Assert.Throws<ArgumentNullException>(() => palette.RemoveCommand((CommandPaletteItem)null!));
        Assert.Throws<ArgumentException>(() => palette.GetCommand(" "));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
