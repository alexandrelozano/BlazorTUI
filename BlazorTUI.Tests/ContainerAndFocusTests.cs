using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ContainerAndFocusTests
{
    [Fact]
    public void AddControlInitializesOwnershipTabOrderAndZOrder()
    {
        var container = new Container("root");
        var first = TextBox("first", 0);
        var second = TextBox("second", 1);

        container.AddControl(first);
        container.AddControl(second);

        Assert.Same(container, first.container);
        Assert.Equal((short)1, first.TabIndex);
        Assert.Equal((short)2, second.TabIndex);
        Assert.Equal((short)1, first.ZOrder);
        Assert.Equal((short)2, second.ZOrder);
    }

    [Fact]
    public void AddControlRejectsBlankAndDuplicateNames()
    {
        var container = new Container("root");
        container.AddControl(TextBox("unique", 0));

        Assert.Throws<InvalidOperationException>(() => container.AddControl(TextBox("unique", 1)));
        Assert.Throws<ArgumentException>(() => TextBox(" ", 2));
    }

    [Fact]
    public void TabAndShiftTabMoveFocusWithinNestedContainer()
    {
        var screen = new Screen(20, 10);
        var frame = new Frame("frame", "", 0, 0, 20, 10, Frame.BorderStyle.none, Color.White, Color.Black);
        var first = TextBox("first", 1);
        var second = TextBox("second", 2);
        frame.AddControl(first);
        frame.AddControl(second);
        screen.topContainer.AddContainer(frame);
        screen.SetFocus("first");

        screen.KeyDown("Tab", false);
        Assert.False(first.Focus);
        Assert.True(second.Focus);

        screen.KeyDown("Tab", true);
        Assert.True(first.Focus);
        Assert.False(second.Focus);
    }

    [Fact]
    public void BringToFrontAndBottomChangeVisibleControlOrder()
    {
        var screen = new Screen(8, 4);
        var frame = new Frame("frame", "", 0, 0, 8, 4, Frame.BorderStyle.none, Color.White, Color.Black);
        var first = new Label("first", "A", 1, 1, 1, Color.White, Color.Black);
        var second = new Label("second", "B", 1, 1, 1, Color.White, Color.Black);
        frame.AddControl(first);
        frame.AddControl(second);
        screen.topContainer.AddContainer(frame);

        screen.Render();
        Assert.Equal("B", screen.rows[1].Cells[1].character);

        frame.BringToFront(first);
        screen.Render();
        Assert.Equal("A", screen.rows[1].Cells[1].character);

        frame.BringToBottom(first);
        screen.Render();
        Assert.Equal("B", screen.rows[1].Cells[1].character);
    }

    [Fact]
    public void BringToFrontAndBottomChangeVisibleContainerOrder()
    {
        var screen = new Screen(8, 4);
        var first = new Frame("firstFrame", "", 0, 0, 8, 4, Frame.BorderStyle.none, Color.White, Color.Black);
        var second = new Frame("secondFrame", "", 0, 0, 8, 4, Frame.BorderStyle.none, Color.White, Color.Black);
        first.AddControl(new Label("firstLabel", "A", 1, 1, 1, Color.White, Color.Black));
        second.AddControl(new Label("secondLabel", "B", 1, 1, 1, Color.White, Color.Black));
        screen.topContainer.AddContainer(first);
        screen.topContainer.AddContainer(second);

        screen.Render();
        Assert.Equal("B", screen.rows[1].Cells[1].character);

        screen.topContainer.BringToFront(first);
        screen.Render();
        Assert.Equal("A", screen.rows[1].Cells[1].character);

        screen.topContainer.BringToBottom(first);
        screen.Render();
        Assert.Equal("B", screen.rows[1].Cells[1].character);
    }

    private static TextBox TextBox(string name, short y) =>
        new(name, "", 1, y, 5, Color.White, Color.Black);
}
