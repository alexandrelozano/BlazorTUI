using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class LayoutPanelTests
{
    [Fact]
    public void StackPanelArrangesChildrenInOrientationWithSpacing()
    {
        var screen = new Screen(30, 12);
        var stack = new StackPanel(
            "stack", 1, 1, 16, 8,
            LayoutOrientation.Vertical, Color.White, Color.Black)
        {
            Padding = 1,
            Spacing = 1,
            Alignment = LayoutAlignment.Stretch
        };
        var first = new Label("first", "First", 0, 0, 5, Color.Yellow, Color.Black);
        var second = new Label("second", "Second", 0, 0, 6, Color.Yellow, Color.Black);
        stack.AddControl(first);
        stack.AddControl(second);
        screen.TopContainer.AddContainer(stack);

        screen.Render();

        Assert.Equal(1, first.X);
        Assert.Equal(1, first.Y);
        Assert.Equal(14, first.Width);
        Assert.Equal(1, second.X);
        Assert.Equal(3, second.Y);
        Assert.StartsWith("First", Read(screen, 2, 2, 5));
        Assert.StartsWith("Second", Read(screen, 2, 4, 6));
    }

    [Fact]
    public void GridPanelResolvesFixedAutoAndStarCells()
    {
        var screen = new Screen(40, 12);
        var grid = new GridPanel(
            "grid",
            new[]
            {
                GridPanelLength.Fixed(6),
                GridPanelLength.Auto,
                GridPanelLength.Star()
            },
            new[]
            {
                GridPanelLength.Auto,
                GridPanelLength.Star()
            },
            1, 1, 30, 8,
            Color.White, Color.Black);
        var left = new Label("left", "Left", 0, 0, 4, Color.Yellow, Color.Black);
        var auto = new Label("auto", "AutoWide", 0, 0, 8, Color.Yellow, Color.Black);
        var fill = new Label("fill", "Fill", 0, 0, 4, Color.Yellow, Color.Black);
        grid.AddControl(left, 0, 0);
        grid.AddControl(auto, 0, 1);
        grid.AddControl(fill, 1, 2);
        screen.TopContainer.AddContainer(grid);

        screen.Render();

        Assert.Equal(0, left.X);
        Assert.Equal(6, auto.X);
        Assert.Equal(14, fill.X);
        Assert.True(fill.Width >= 16);
        Assert.StartsWith("AutoWide", Read(screen, 7, 1, 8));
    }

    [Fact]
    public void DockPanelDocksChildrenAroundFillArea()
    {
        var screen = new Screen(40, 12);
        var dock = new DockPanel("dock", 1, 1, 24, 8, Color.White, Color.Black);
        var top = new Label("top", "Top", 0, 0, 3, Color.Yellow, Color.Black);
        var left = new Label("left", "Left", 0, 0, 4, Color.Yellow, Color.Black);
        var fill = new Label("fill", "Fill", 0, 0, 4, Color.Yellow, Color.Black);
        dock.AddControl(top, DockPanelDock.Top);
        dock.AddControl(left, DockPanelDock.Left);
        dock.AddControl(fill, DockPanelDock.Fill);
        screen.TopContainer.AddContainer(dock);

        screen.Render();

        Assert.Equal(0, top.X);
        Assert.Equal(0, top.Y);
        Assert.Equal(24, top.Width);
        Assert.Equal(0, left.X);
        Assert.Equal(1, left.Y);
        Assert.Equal(4, left.Width);
        Assert.Equal(7, left.Height);
        Assert.Equal(4, fill.X);
        Assert.Equal(1, fill.Y);
        Assert.Equal(20, fill.Width);
        Assert.StartsWith("Fill", Read(screen, 5, 2, 4));
    }

    [Fact]
    public void WrapPanelMovesChildrenToNextLineWhenWidthIsExhausted()
    {
        var screen = new Screen(30, 12);
        var wrap = new WrapPanel(
            "wrap", 1, 1, 12, 8,
            LayoutOrientation.Horizontal, Color.White, Color.Black)
        {
            Padding = 1,
            ItemSpacing = 1,
            LineSpacing = 1
        };
        var first = new Label("first", "Alpha", 0, 0, 5, Color.Yellow, Color.Black);
        var second = new Label("second", "Beta", 0, 0, 4, Color.Yellow, Color.Black);
        var third = new Label("third", "Gamma", 0, 0, 5, Color.Yellow, Color.Black);
        wrap.AddControl(first);
        wrap.AddControl(second);
        wrap.AddControl(third);
        screen.TopContainer.AddContainer(wrap);

        screen.Render();

        Assert.Equal(1, first.X);
        Assert.Equal(1, first.Y);
        Assert.Equal(7, second.X);
        Assert.Equal(1, second.Y);
        Assert.Equal(1, third.X);
        Assert.Equal(3, third.Y);
        Assert.StartsWith("Gamma", Read(screen, 2, 4, 5));
    }

    [Fact]
    public void ScrollViewerClipsContentAndRendersScrolledOffset()
    {
        var screen = new Screen(40, 12);
        var viewer = new ScrollViewer("viewer", 1, 1, 18, 4, Color.White, Color.Black);
        viewer.AddControl(new Label("row0", "Row 0 visible", 0, 0, 24, Color.Yellow, Color.Black));
        viewer.AddControl(new Label("row5", "Row 5 visible", 0, 5, 24, Color.Yellow, Color.Black));
        viewer.AddControl(new Label("row10", "Row 10 visible", 0, 10, 24, Color.Yellow, Color.Black));
        screen.TopContainer.AddContainer(viewer);

        screen.Render();

        Assert.StartsWith("Row 0", Read(screen, 1, 1, 5));
        Assert.Equal("↑", screen.Rows[1].Cells[18].Character);
        Assert.Equal("↓", screen.Rows[3].Cells[18].Character);
        Assert.Equal("←", screen.Rows[4].Cells[1].Character);
        Assert.Equal("→", screen.Rows[4].Cells[18].Character);
        Assert.DoesNotContain("Row 5", ReadScreen(screen));

        viewer.ScrollTo(0, 5);
        screen.Render();

        Assert.Equal(5, viewer.VerticalOffset);
        Assert.StartsWith("Row 5", Read(screen, 1, 1, 5));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));

    private static string ReadScreen(Screen screen)
        => string.Join(Environment.NewLine, screen.Rows.Select(row =>
            string.Concat(row.Cells.Select(cell => cell.Character))));
}
