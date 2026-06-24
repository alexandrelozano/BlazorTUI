using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ComboBoxTests
{
    [Fact]
    public void RendersSelectedItemAndDropDown()
    {
        var screen = new Screen(20, 8);
        var comboBox = new ComboBox(
            "priority", new[] { "Low", "Normal", "High" },
            1, 1, 10, Color.Yellow, Color.Black, selectedIndex: 1);
        screen.TopContainer.AddControl(comboBox);

        screen.Render();

        Assert.Equal("[Normal ▼]", Read(screen, 1, 1, 10));

        comboBox.OpenDropDown();
        screen.Render();

        Assert.True(comboBox.IsDropDownOpen);
        Assert.StartsWith("Low", Read(screen, 1, 2, 10));
        Assert.StartsWith("Normal", Read(screen, 1, 3, 10));
        Assert.Equal(Color.Black, screen.Rows[3].Cells[1].ForeColor);
        Assert.Equal(Color.Yellow, screen.Rows[3].Cells[1].BackgroundColor);
    }

    [Fact]
    public void KeyboardNavigationCommitsOrCancelsHighlightedItem()
    {
        var container = new Container("root") { Width = 20, Height = 8 };
        var comboBox = new ComboBox(
            "priority", new[] { "Low", "Normal", "High" },
            1, 1, 10, Color.White, Color.Black, selectedIndex: 1);
        container.AddControl(comboBox);
        int changes = 0;
        comboBox.SelectedIndexChanged += (_, _) => changes++;

        comboBox.KeyDown("Enter", false);
        comboBox.KeyDown("ArrowDown", false);

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.True(comboBox.IsDropDownOpen);

        comboBox.KeyDown("Escape", false);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(comboBox.IsDropDownOpen);

        comboBox.KeyDown("F4", false);
        comboBox.KeyDown("End", false);
        comboBox.KeyDown("Enter", false);

        Assert.Equal(2, comboBox.SelectedIndex);
        Assert.Equal("High", comboBox.SelectedItem);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(1, changes);

        comboBox.KeyDown("Home", false);
        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void PopupClickSelectsItemWithoutActivatingCoveredControl()
    {
        var screen = new Screen(20, 8);
        var comboBox = new ComboBox(
            "priority", new[] { "Low", "Normal", "High" },
            1, 1, 10, Color.White, Color.Black);
        int coveredButtonClicks = 0;
        var coveredButton = new Button(
            "coveredButton", "Covered", 1, 3, 10, Color.White, Color.DarkBlue);
        coveredButton.Clicked += (_, _) => coveredButtonClicks++;
        screen.TopContainer.AddControl(comboBox);
        screen.TopContainer.AddControl(coveredButton);

        screen.TopContainer.Click(2, 1);
        screen.TopContainer.Click(2, 3);

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Normal", comboBox.SelectedItem);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, coveredButtonClicks);
    }

    [Fact]
    public void ItemsCanBeChangedAndSelectionRemainsValid()
    {
        var comboBox = new ComboBox(
            "priority", Array.Empty<string>(),
            0, 0, 10, Color.White, Color.Black);
        int changes = 0;
        comboBox.SelectedIndexChanged += (_, _) => changes++;

        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Null(comboBox.SelectedItem);

        comboBox.AddItem("Low");
        comboBox.AddItem("High");
        comboBox.SelectItem("High");
        Assert.True(comboBox.RemoveItem("High"));

        Assert.Equal(0, comboBox.SelectedIndex);
        Assert.Equal("Low", comboBox.SelectedItem);
        Assert.Equal(3, changes);

        comboBox.ClearItems();
        Assert.Equal(-1, comboBox.SelectedIndex);
        Assert.Empty(comboBox.Items);
        Assert.Equal(4, changes);
    }

    [Fact]
    public void ValidatesDimensionsItemsAndSelection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ComboBox("priority", new[] { "Low" }, 0, 0, 3, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ComboBox("priority", new[] { "Low" }, 0, 0, 10, Color.White, Color.Black, 1));
        Assert.Throws<ArgumentNullException>(() =>
            new ComboBox("priority", new string[] { null! }, 0, 0, 10, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new ComboBox("priority", new[] { "Low\nHigh" }, 0, 0, 10, Color.White, Color.Black));

        var comboBox = new ComboBox(
            "priority", new[] { "Low" }, 0, 0, 10, Color.White, Color.Black);
        Assert.Throws<ArgumentOutOfRangeException>(() => comboBox.SelectedIndex = 1);
        Assert.Throws<ArgumentException>(() => comboBox.SelectItem("Missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => comboBox.MaxDropDownItems = 0);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
