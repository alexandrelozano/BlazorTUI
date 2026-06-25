using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class StatusBarTests
{
    [Fact]
    public void RendersMessageAndRightAlignedItems()
    {
        var screen = new Screen(30, 4);
        var statusBar = new StatusBar(
            "status", "Ready", 0, 2, 30,
            Color.Black, Color.Gray)
        {
            Separator = "  "
        };
        statusBar.AddItem("help", "F1 Help");
        statusBar.AddItem("save", "Ctrl+S Save", foreColor: Color.Yellow);
        screen.TopContainer.AddControl(statusBar);

        screen.Render();

        Assert.Equal("Ready     F1 Help  Ctrl+S Save", Read(screen, 0, 2, 30));
        Assert.Equal(Color.Black, screen.Rows[2].Cells[0].ForeColor);
        Assert.Equal(Color.Gray, screen.Rows[2].Cells[0].BackgroundColor);
        Assert.Equal(Color.Yellow, screen.Rows[2].Cells[19].ForeColor);
        Assert.Equal(Color.Gray, screen.Rows[2].Cells[19].BackgroundColor);
    }

    [Fact]
    public void RendersLeftItemsAndTruncatesToAvailableWidth()
    {
        var screen = new Screen(20, 4);
        var statusBar = new StatusBar(
            "status", "Saving", 0, 1, 20,
            Color.White, Color.DarkBlue)
        {
            Separator = " | "
        };
        statusBar.AddItem("mode", "INS", alignment: StatusBarItemAlignment.Left);
        statusBar.AddItem("state", "Very long state", width: 6);
        screen.TopContainer.AddControl(statusBar);

        screen.Render();

        Assert.Equal("Saving | INS  Ve", Read(screen, 0, 1, 16));
        Assert.Equal("Very l", Read(screen, 14, 1, 6));
    }

    [Fact]
    public void SupportsMessageAndItemMutationEventsAndReadOnlyItems()
    {
        var statusBar = new StatusBar("status", "Ready", 0, 0, 20, Color.White, Color.Black);
        var changes = new List<(string Previous, string Current)>();
        statusBar.MessageChanged += (_, args) => changes.Add((args.PreviousMessage, args.Message));

        StatusBarItem item = statusBar.AddItem("mode", "INS", alignment: StatusBarItemAlignment.Left);
        statusBar.Value = "Saved";
        item.Text = "OVR";
        statusBar.RemoveItem("mode");

        Assert.Equal("Saved", statusBar.Message);
        Assert.Empty(statusBar.Items);
        Assert.Equal(new[] { ("Ready", "Saved") }, changes);
    }

    [Fact]
    public void ValidatesDimensionsItemsAndDuplicates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StatusBar("status", "Ready", 0, 0, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new StatusBar(" ", "Ready", 0, 0, 10, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new StatusBarItem(" ", "Ready"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StatusBarItem("mode", "INS", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StatusBarItem("mode", "INS", alignment: (StatusBarItemAlignment)99));

        var statusBar = new StatusBar("status", "Ready", 0, 0, 10, Color.White, Color.Black);
        statusBar.AddItem("mode", "INS");

        Assert.Throws<InvalidOperationException>(() => statusBar.AddItem("mode", "OVR"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            statusBar.GetItem("mode")!.Alignment = (StatusBarItemAlignment)99);
        Assert.Throws<ArgumentNullException>(() => statusBar.AddItem((StatusBarItem)null!));
        Assert.Throws<ArgumentNullException>(() => statusBar.RemoveItem((StatusBarItem)null!));
        Assert.Throws<ArgumentException>(() => statusBar.GetItem(" "));
    }

    [Fact]
    public void ClipsToParentContainer()
    {
        var screen = new Screen(12, 4);
        var frame = new Frame(
            "frame", "", 2, 1, 8, 2,
            Frame.BorderStyle.none, Color.White, Color.Blue);
        screen.TopContainer.AddContainer(frame);
        var statusBar = new StatusBar(
            "status", "0123456789", 4, 0, 10,
            Color.White, Color.Black);
        frame.AddControl(statusBar);

        screen.Render();

        Assert.Equal("      0123", Read(screen, 0, 1, 10));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
