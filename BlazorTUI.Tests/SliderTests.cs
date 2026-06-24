using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class SliderTests
{
    [Fact]
    public void HorizontalSliderRendersTrackKnobAndFocus()
    {
        var screen = new Screen(15, 4);
        var slider = new Slider(
            "volume", 0, 100, 50, 10, 1, 1, 11,
            Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(slider);
        screen.SetFocus("volume");

        screen.Render();

        Assert.Equal("━━━━━●─────", Read(screen, 1, 1, 11));
        Assert.Equal(Color.Black, screen.Rows[1].Cells[6].ForeColor);
        Assert.Equal(Color.Yellow, screen.Rows[1].Cells[6].BackgroundColor);
        Assert.Equal(50d, slider.Percentage);
    }

    [Fact]
    public void VerticalSliderRendersFromMaximumToMinimum()
    {
        var screen = new Screen(5, 7);
        var slider = new Slider(
            "temperature", 0, 100, 50, 10, 2, 1, 5,
            SliderOrientation.Vertical, Color.White, Color.Black);
        screen.TopContainer.AddControl(slider);

        screen.Render();

        Assert.Equal(new[] { "│", "│", "●", "┃", "┃" },
            screen.Rows.Skip(1).Take(5).Select(row => row.Cells[2].Character));
        Assert.Equal((short)1, slider.Width);
        Assert.Equal((short)5, slider.Height);
        Assert.Equal(SliderOrientation.Vertical, slider.Orientation);
    }

    [Fact]
    public void KeyboardUsesSmallAndLargeChangesAndClampsAtBounds()
    {
        var slider = new Slider(
            "volume", 0, 100, 50, 10, 0, 0, 11,
            SliderOrientation.Horizontal, Color.White, Color.Black,
            largeChange: 30);
        var changes = new List<(int Previous, int Current)>();
        int activations = 0;
        slider.ValueChanged += (_, args) => changes.Add((args.PreviousValue, args.Value));
        slider.Clicked += (_, _) => activations++;

        slider.KeyDown("ArrowLeft", false);
        slider.KeyDown("ArrowRight", false);
        slider.KeyDown("PageUp", false);
        slider.KeyDown("End", false);
        slider.KeyDown("ArrowRight", false);
        slider.KeyDown("Home", false);

        Assert.Equal(0, slider.Value);
        Assert.Equal(
            new[] { (50, 40), (40, 50), (50, 80), (80, 100), (100, 0) },
            changes);
        Assert.Equal(5, activations);
    }

    [Fact]
    public void MouseClickMapsAndSnapsPositionForBothOrientations()
    {
        var container = new Container("root") { Width = 20, Height = 10 };
        var horizontal = new Slider(
            "horizontal", 0, 100, 0, 10, 1, 1, 11,
            Color.White, Color.Black);
        var vertical = new Slider(
            "vertical", -50, 50, 0, 5, 15, 1, 5,
            SliderOrientation.Vertical, Color.White, Color.Black);
        container.AddControl(horizontal);
        container.AddControl(vertical);

        horizontal.Click(7, 0);
        Assert.Equal(70, horizontal.Value);
        horizontal.Click(10, 0);
        Assert.Equal(100, horizontal.Value);

        vertical.Click(0, 0);
        Assert.Equal(50, vertical.Value);
        vertical.Click(0, 4);
        Assert.Equal(-50, vertical.Value);
    }

    [Fact]
    public void RangeChangesPreserveAValidValueAndRaiseEvents()
    {
        var slider = new Slider(
            "volume", 0, 100, 50, 5, 0, 0, 11,
            Color.White, Color.Black);
        int changes = 0;
        slider.ValueChanged += (_, _) => changes++;

        slider.Minimum = 60;
        slider.Maximum = 80;
        slider.Value = 75;
        slider.Maximum = 70;

        Assert.Equal(60, slider.Minimum);
        Assert.Equal(70, slider.Maximum);
        Assert.Equal(70, slider.Value);
        Assert.Equal(3, changes);
        Assert.Throws<ArgumentOutOfRangeException>(() => slider.Minimum = 70);
        Assert.Throws<ArgumentOutOfRangeException>(() => slider.Maximum = 60);
        Assert.Throws<ArgumentOutOfRangeException>(() => slider.Value = 71);
    }

    [Fact]
    public void ValidatesRangeChangesLengthAndOrientation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Slider("slider", 10, 10, 10, 1, 0, 0, 10, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Slider("slider", 0, 10, 11, 1, 0, 0, 10, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Slider("slider", 0, 10, 5, 0, 0, 0, 10, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Slider("slider", 0, 10, 5, 1, 0, 0, 2, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Slider(
                "slider", 0, 10, 5, 1, 0, 0, 10,
                (SliderOrientation)99, Color.White, Color.Black));

        var slider = new Slider(
            "slider", 0, 10, 5, 1, 0, 0, 10,
            Color.White, Color.Black);
        Assert.Throws<ArgumentOutOfRangeException>(() => slider.Step = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => slider.LargeChange = 0);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
