using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ScreenAndLayoutTests
{
    [Fact]
    public void ConstructorCreatesAddressableCellGrid()
    {
        var screen = new Screen(8, 4);

        Assert.Equal(8, screen.width);
        Assert.Equal(4, screen.height);
        Assert.Equal(4, screen.rows.Count);
        Assert.All(screen.rows, row => Assert.Equal(8, row.Cells.Count));
        Assert.Equal((short)7, screen.rows[3].Cells[7].x);
        Assert.Equal((short)3, screen.rows[3].Cells[7].y);
    }

    [Fact]
    public void NestedControlRendersAtParentRelativeCoordinates()
    {
        var screen = new Screen(12, 6);
        var frame = new Frame("frame", "", 2, 1, 8, 4, Frame.BorderStyle.none, Color.White, Color.DarkBlue);
        frame.AddControl(new Label("label", "HELLO", 1, 1, 5, Color.Yellow, Color.DarkBlue));
        screen.topContainer.AddContainer(frame);

        screen.Render();

        string rendered = string.Concat(screen.rows[2].Cells.Skip(3).Take(5).Select(cell => cell.character));
        Assert.Equal("HELLO", rendered);
        Assert.Equal(Color.Yellow, screen.rows[2].Cells[3].foreColor);
    }

    [Fact]
    public void ChildRenderingIsClippedAtContainerBoundary()
    {
        var screen = new Screen(8, 4);
        var frame = new Frame("frame", "", 0, 0, 4, 3, Frame.BorderStyle.none, Color.White, Color.Black);
        frame.AddControl(new Label("label", "ABCDE", 2, 1, 5, Color.Yellow, Color.Black));
        screen.topContainer.AddContainer(frame);

        screen.Render();

        Assert.Equal("A", screen.rows[1].Cells[2].character);
        Assert.Equal("B", screen.rows[1].Cells[3].character);
        Assert.Equal(" ", screen.rows[1].Cells[4].character);
    }
}
