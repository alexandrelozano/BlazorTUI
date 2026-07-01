using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ScreenAndLayoutTests
{
    [Fact]
    public void FrameTitleWritesExactlyOneCharacterPerCell()
    {
        var screen = new Screen(12, 6);
        var frame = new Frame("frame", "FRAME", 1, 1, 9, 4, Frame.BorderStyle.Line, Color.White, Color.Black);
        screen.TopContainer.AddContainer(frame);

        screen.Render();

        string title = string.Concat(screen.Rows[1].Cells.Skip(3).Take(5).Select(cell => cell.Character));
        Assert.Equal("FRAME", title);
        Assert.All(screen.Rows.SelectMany(row => row.Cells), cell => Assert.True(cell.Character.Length <= 1));
    }

    [Fact]
    public void RevisionChangesOnlyWhenRenderedCellsChange()
    {
        var screen = new Screen(8, 4);
        var label = new Label("label", "OK", 0, 0, 2, Color.White, Color.Black);
        screen.TopContainer.AddControl(label);

        screen.Render();
        long renderedRevision = screen.Revision;

        screen.Render();
        Assert.Equal(renderedRevision, screen.Revision);

        label.ForeColor = Color.Yellow;
        screen.Render();
        Assert.True(screen.Revision > renderedRevision);
    }

    [Fact]
    public void ConstructorCreatesAddressableCellGrid()
    {
        var screen = new Screen(8, 4);

        Assert.Equal(8, screen.Width);
        Assert.Equal(4, screen.Height);
        Assert.Equal(4, screen.Rows.Count);
        Assert.All(screen.Rows, row => Assert.Equal(8, row.Cells.Count));
        Assert.Equal((short)7, screen.Rows[3].Cells[7].X);
        Assert.Equal((short)3, screen.Rows[3].Cells[7].Y);
    }

    [Fact]
    public void NestedControlRendersAtParentRelativeCoordinates()
    {
        var screen = new Screen(12, 6);
        var frame = new Frame("frame", "", 2, 1, 8, 4, Frame.BorderStyle.None, Color.White, Color.DarkBlue);
        frame.AddControl(new Label("label", "HELLO", 1, 1, 5, Color.Yellow, Color.DarkBlue));
        screen.TopContainer.AddContainer(frame);

        screen.Render();

        string rendered = string.Concat(screen.Rows[2].Cells.Skip(3).Take(5).Select(cell => cell.Character));
        Assert.Equal("HELLO", rendered);
        Assert.Equal(Color.Yellow, screen.Rows[2].Cells[3].ForeColor);
    }

    [Fact]
    public void ChildRenderingIsClippedAtContainerBoundary()
    {
        var screen = new Screen(8, 4);
        var frame = new Frame("frame", "", 0, 0, 4, 3, Frame.BorderStyle.None, Color.White, Color.Black);
        frame.AddControl(new Label("label", "ABCDE", 2, 1, 5, Color.Yellow, Color.Black));
        screen.TopContainer.AddContainer(frame);

        screen.Render();

        Assert.Equal("A", screen.Rows[1].Cells[2].Character);
        Assert.Equal("B", screen.Rows[1].Cells[3].Character);
        Assert.Equal(" ", screen.Rows[1].Cells[4].Character);
    }
}
