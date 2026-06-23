using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class PictureBoxTests
{
    [Fact]
    public void RenderStoresImageAsScaledDataUri()
    {
        var screen = new Screen(8, 4);
        var picture = new PictureBox("picture", [1, 2, 3], 1, 1, 2, 2, Color.White, Color.Black);
        screen.topContainer.AddControl(picture);

        screen.Render();

        Cell cell = screen.rows[1].Cells[1];
        Assert.Equal("data:image/png;base64,AQID", cell.backgroundImage);
        Assert.Equal(2, cell.scaleX);
        Assert.Equal(2, cell.scaleY);
    }

    [Fact]
    public void ConstructorRejectsEmptyDataAndInvalidMediaType()
    {
        Assert.Throws<ArgumentException>(() =>
            new PictureBox("empty", [], 0, 0, 1, 1, Color.White, Color.Black));

        Assert.Throws<ArgumentException>(() =>
            new PictureBox("invalid", [1], "text/plain", 0, 0, 1, 1, Color.White, Color.Black));
    }
}
