using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class SplitPanelTests
{
    [Fact]
    public void VerticalSplitPanelRendersSeparatorAndChildPanels()
    {
        var screen = new Screen(20, 6);
        var splitPanel = new SplitPanel(
            "layout", 1, 1, 14, 4,
            SplitPanelOrientation.Vertical, 6,
            Color.Yellow, Color.DarkBlue);
        screen.TopContainer.AddContainer(splitPanel);
        splitPanel.FirstPanel.AddControl(new Label(
            "leftLabel", "Left", 0, 0, 4, Color.White, Color.Black));
        splitPanel.SecondPanel.AddControl(new Label(
            "rightLabel", "Right", 0, 0, 5, Color.Cyan, Color.Black));

        screen.Render();

        Assert.Equal("Left  │Right  ", Read(screen, 1, 1, 14));
        Assert.Equal((short)6, splitPanel.FirstPanel.Width);
        Assert.Equal((short)7, splitPanel.SecondPanel.X);
        Assert.Equal((short)7, splitPanel.SecondPanel.Width);
        Assert.Equal(Color.Yellow, screen.Rows[1].Cells[7].ForeColor);
        Assert.Equal(Color.DarkBlue, screen.Rows[1].Cells[7].BackgroundColor);
    }

    [Fact]
    public void HorizontalSplitPanelRendersSeparatorAndChildPanels()
    {
        var screen = new Screen(12, 8);
        var splitPanel = new SplitPanel(
            "layout", 1, 1, 10, 6,
            SplitPanelOrientation.Horizontal, 2,
            Color.White, Color.DarkGreen);
        screen.TopContainer.AddContainer(splitPanel);
        splitPanel.FirstPanel.AddControl(new Label(
            "topLabel", "Top", 0, 0, 3, Color.Yellow, Color.Black));
        splitPanel.SecondPanel.AddControl(new Label(
            "bottomLabel", "Bottom", 0, 0, 6, Color.Cyan, Color.Black));

        screen.Render();

        Assert.StartsWith("Top", Read(screen, 1, 1, 10));
        Assert.Equal("──────────", Read(screen, 1, 3, 10));
        Assert.StartsWith("Bottom", Read(screen, 1, 4, 10));
        Assert.Equal((short)2, splitPanel.FirstPanel.Height);
        Assert.Equal((short)3, splitPanel.SecondPanel.Y);
        Assert.Equal((short)3, splitPanel.SecondPanel.Height);
    }

    [Fact]
    public void MovingSplitterUpdatesPaneSizesAndRaisesEvents()
    {
        var splitPanel = new SplitPanel(
            "layout", 0, 0, 20, 5,
            SplitPanelOrientation.Vertical, 5,
            Color.White, Color.Black);
        var events = new List<(short Previous, short Current, short First, short Second)>();
        splitPanel.SplitterMoved += (_, args) =>
            events.Add((args.PreviousSplitterPosition, args.SplitterPosition, args.FirstPaneSize, args.SecondPaneSize));

        splitPanel.MoveSplitter(3);
        splitPanel.MoveSplitter(100);

        Assert.Equal((short)18, splitPanel.SplitterPosition);
        Assert.Equal((short)18, splitPanel.FirstPaneSize);
        Assert.Equal((short)1, splitPanel.SecondPaneSize);
        Assert.Equal(
            new[] { ((short)5, (short)8, (short)8, (short)11), ((short)8, (short)18, (short)18, (short)1) },
            events);
    }

    [Fact]
    public void TabNavigationMovesBetweenPaneControls()
    {
        var screen = new Screen(20, 5);
        var splitPanel = new SplitPanel(
            "layout", 0, 0, 20, 5,
            SplitPanelOrientation.Vertical, 10,
            Color.White, Color.Black);
        screen.TopContainer.AddContainer(splitPanel);
        var leftInput = new TextBox("leftInput", "", 0, 0, 8, Color.White, Color.Black);
        var rightInput = new TextBox("rightInput", "", 0, 0, 8, Color.White, Color.Black);
        splitPanel.FirstPanel.AddControl(leftInput);
        splitPanel.SecondPanel.AddControl(rightInput);
        screen.SetFocus("leftInput");

        screen.KeyDown("Tab", false);
        Assert.True(rightInput.Focus);
        Assert.False(leftInput.Focus);

        screen.KeyDown("Tab", true);
        Assert.True(leftInput.Focus);
        Assert.False(rightInput.Focus);
    }

    [Fact]
    public void NestedPanelContentIsClippedToAncestorPaneBounds()
    {
        var screen = new Screen(30, 10);
        var frame = new Frame(
            "frame", "", 2, 1, 20, 8,
            Frame.BorderStyle.Line, Color.Yellow, Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);
        var splitPanel = new SplitPanel(
            "layout", 1, 1, 18, 5,
            SplitPanelOrientation.Vertical, 14,
            Color.Yellow, Color.DarkBlue);
        frame.AddContainer(splitPanel);
        var widePanel = new Container("widePanel")
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5
        };
        splitPanel.SecondPanel.AddContainer(widePanel);
        widePanel.AddControl(new Label(
            "wideLabel", "ABCDEFGHIJ", 0, 0, 10,
            Color.White, Color.Black));

        screen.Render();

        Assert.Equal("ABC", Read(screen, 18, 2, 3));
        Assert.Equal("│", screen.Rows[2].Cells[21].Character);
        Assert.Equal("        ", Read(screen, 22, 2, 8));
    }

    [Fact]
    public void ValidatesDimensionsOrientationSplitterAndMinimums()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SplitPanel("layout", 0, 0, 2, 5, SplitPanelOrientation.Vertical, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SplitPanel("layout", 0, 0, 5, 2, SplitPanelOrientation.Horizontal, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SplitPanel("layout", 0, 0, 10, 5, (SplitPanelOrientation)99, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SplitPanel("layout", 0, 0, 10, 5, SplitPanelOrientation.Vertical, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SplitPanel("layout", 0, 0, 10, 5, SplitPanelOrientation.Vertical, 9, Color.White, Color.Black));

        var splitPanel = new SplitPanel(
            "layout", 0, 0, 10, 5,
            SplitPanelOrientation.Vertical, 4,
            Color.White, Color.Black);

        Assert.Throws<ArgumentOutOfRangeException>(() => splitPanel.MinimumFirstPaneSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => splitPanel.MinimumSecondPaneSize = 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => splitPanel.Orientation = (SplitPanelOrientation)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => splitPanel.SplitterPosition = 9);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
