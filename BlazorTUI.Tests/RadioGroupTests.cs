using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class RadioGroupTests
{
    [Fact]
    public void VerticalRadioGroupRendersSelectedOptionAndFocus()
    {
        var screen = new Screen(24, 8);
        var group = new RadioGroup(
            "deliveryType",
            new[] { "Pickup", "Delivery", "Courier" },
            1, 1, 16,
            Color.Yellow, Color.Black,
            selectedIndex: 1);
        screen.TopContainer.AddControl(group);
        screen.SetFocus("deliveryType");

        screen.Render();

        Assert.StartsWith("( ) Pickup", Read(screen, 1, 1, 16));
        Assert.StartsWith("(●) Delivery", Read(screen, 1, 2, 16));
        Assert.StartsWith("( ) Courier", Read(screen, 1, 3, 16));
        Assert.Equal(Color.Black, screen.Rows[2].Cells[1].ForeColor);
        Assert.Equal(Color.Yellow, screen.Rows[2].Cells[1].BackgroundColor);
        Assert.Equal((short)3, group.Height);
        Assert.Equal("Delivery", group.SelectedItem);
    }

    [Fact]
    public void HorizontalRadioGroupRendersAndHandlesMouseSelection()
    {
        var screen = new Screen(36, 5);
        var group = new RadioGroup(
            "gender",
            new[]
            {
                new RadioGroupOption("male", "Male", "M"),
                new RadioGroupOption("female", "Female", "F")
            },
            1, 1, 30,
            Color.White, Color.Black,
            selectedIndex: 0,
            RadioGroupOrientation.Horizontal);
        int changes = 0;
        group.SelectedIndexChanged += (_, _) => changes++;
        screen.TopContainer.AddControl(group);

        screen.TopContainer.Click(12, 1);
        screen.Render();

        Assert.StartsWith("( ) Male (●) Female", Read(screen, 1, 1, 22));
        Assert.Equal(1, group.SelectedIndex);
        Assert.Equal("F", group.SelectedValue);
        Assert.Equal(1, changes);
        Assert.True(group.Focus);
        Assert.Equal((short)1, group.Height);
    }

    [Fact]
    public void KeyboardNavigationSelectsOptionsAndRaisesTypedEvents()
    {
        var group = new RadioGroup(
            "priority",
            new[] { "Low", "Normal", "High" },
            0, 0, 14,
            Color.White, Color.Black);
        var changes = new List<(int Previous, int Current, string? PreviousText, string? CurrentText)>();
        group.SelectionChanged += (_, args) =>
            changes.Add((
                args.PreviousSelectedIndex,
                args.SelectedIndex,
                args.PreviousSelectedOption?.Text,
                args.SelectedOption?.Text));

        group.KeyDown("ArrowDown", false);
        group.KeyDown("End", false);
        group.KeyDown("ArrowDown", false);
        group.KeyDown("Home", false);
        group.KeyDown("ArrowUp", false);

        Assert.Equal(0, group.SelectedIndex);
        var expectedChanges = new List<(int Previous, int Current, string? PreviousText, string? CurrentText)>
            {
                (0, 1, "Low", "Normal"),
                (1, 2, "Normal", "High"),
                (2, 0, "High", "Low")
            };
        Assert.Equal(expectedChanges, changes);
    }

    [Fact]
    public void SupportsOptionMutationAndSelectionHelpers()
    {
        var group = new RadioGroup(
            "priority",
            Array.Empty<string>(),
            0, 0, 14,
            Color.White, Color.Black);
        int changes = 0;
        group.SelectedIndexChanged += (_, _) => changes++;

        group.AddOption("low", "Low", "L");
        group.AddOption("normal", "Normal", "N");
        group.AddOption("high", "High", "H");
        group.SelectValue("H");
        Assert.True(group.RemoveOption("normal"));
        group.SelectOption("low");
        group.ClearOptions();

        Assert.Empty(group.Options);
        Assert.Equal(-1, group.SelectedIndex);
        Assert.Null(group.SelectedOption);
        Assert.Equal(5, changes);
    }

    [Fact]
    public void ValidatesArgumentsAndDuplicateOptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RadioGroup("group", new[] { "One" }, 0, 0, 3, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RadioGroup("group", new[] { "One" }, 0, 0, 10, Color.White, Color.Black, selectedIndex: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RadioGroup("group", new[] { "One" }, 0, 0, 10, Color.White, Color.Black, orientation: (RadioGroupOrientation)99));
        Assert.Throws<ArgumentException>(() =>
            new RadioGroupOption("bad", "One\nTwo"));
        Assert.Throws<InvalidOperationException>(() =>
            new RadioGroup(
                "group",
                new[]
                {
                    new RadioGroupOption("duplicate", "One"),
                    new RadioGroupOption("duplicate", "Two")
                },
                0, 0, 10,
                Color.White, Color.Black));

        var group = new RadioGroup("group", new[] { "One" }, 0, 0, 10, Color.White, Color.Black);

        Assert.Throws<ArgumentOutOfRangeException>(() => group.SelectedIndex = 5);
        Assert.Throws<ArgumentException>(() => group.SelectOption("missing"));
        Assert.Throws<ArgumentException>(() => group.SelectValue("missing"));
        Assert.Throws<InvalidOperationException>(() => group.AddOption("option0", "Duplicate"));
        Assert.Throws<ArgumentNullException>(() => group.AddOption(null!));
        Assert.Throws<ArgumentNullException>(() => group.RemoveOption((RadioGroupOption)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => group.Orientation = (RadioGroupOrientation)99);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
