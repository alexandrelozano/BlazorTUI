using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class MonthPickerTests
{
    [Fact]
    public void RendersClosedValueAndPopupMonthGrid()
    {
        var screen = new Screen(50, 12);
        var picker = new MonthPicker(
            "billingMonth",
            new DateOnly(2026, 6, 15),
            MonthPicker.MonthFormat.YYYYMM,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);

        screen.Render();

        Assert.StartsWith("[2026/06", Read(screen, 2, 2, 9));
        Assert.Equal(new DateOnly(2026, 6, 1), picker.Value);

        picker.OpenMonthGrid();
        screen.Render();

        Assert.True(picker.IsMonthGridOpen);
        Assert.Contains("2026", Read(screen, 2, 3, 22));
    }

    [Fact]
    public void KeyboardNavigationSelectsHighlightedMonthAndRaisesEvent()
    {
        var picker = new MonthPicker(
            "billingMonth",
            new DateOnly(2026, 6, 15),
            MonthPicker.MonthFormat.YYYYMM,
            0, 0,
            Color.Yellow,
            Color.Black);
        var changes = new List<(DateOnly? Previous, DateOnly? Value)>();
        picker.ValueChanged += (_, args) => changes.Add((args.PreviousValue, args.Value));
        var container = new Container("root") { Width = 40, Height = 12 };
        container.AddControl(picker);

        Assert.True(picker.KeyDown("Enter", false));
        Assert.True(picker.IsDropDownOpen);

        picker.KeyDown("ArrowRight", false);
        picker.KeyDown("Enter", false);

        Assert.False(picker.IsDropDownOpen);
        Assert.Equal(new DateOnly(2026, 7, 1), picker.Value);
        Assert.Equal(new DateOnly(2026, 6, 1), changes.Single().Previous);
        Assert.Equal(new DateOnly(2026, 7, 1), changes.Single().Value);
    }

    [Fact]
    public void PageKeysMoveDisplayedYearWithoutChangingValue()
    {
        var picker = new MonthPicker(
            "billingMonth",
            new DateOnly(2026, 6, 1),
            MonthPicker.MonthFormat.YYYYMM,
            0, 0,
            Color.Yellow,
            Color.Black);
        var container = new Container("root") { Width = 40, Height = 12 };
        container.AddControl(picker);

        picker.OpenMonthGrid();
        picker.KeyDown("PageDown", false);

        Assert.Equal(2027, picker.DisplayedYear);
        Assert.Equal(new DateOnly(2027, 6, 1), picker.HighlightedMonth);
        Assert.Equal(new DateOnly(2026, 6, 1), picker.Value);
    }

    [Fact]
    public void ClickingRenderedHeaderArrowsMovesDisplayedYear()
    {
        var screen = new Screen(50, 12);
        var picker = new MonthPicker(
            "billingMonth",
            new DateOnly(2026, 6, 1),
            MonthPicker.MonthFormat.YYYYMM,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);
        picker.OpenMonthGrid();
        screen.Render();

        string header = Read(screen, 2, 3, 22);
        int previousArrowColumn = header.IndexOf("‹", StringComparison.Ordinal);
        int nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        Assert.True(previousArrowColumn >= 0);
        Assert.True(nextArrowColumn >= 0);

        screen.TopContainer.Click((short)(2 + previousArrowColumn), 3);

        Assert.Equal(2025, picker.DisplayedYear);

        screen.Render();
        header = Read(screen, 2, 3, 22);
        nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        screen.TopContainer.Click((short)(2 + nextArrowColumn), 3);

        Assert.Equal(2026, picker.DisplayedYear);
    }

    [Fact]
    public void RequiredValidationUsesSelectedMonth()
    {
        var picker = new MonthPicker(
            "billingMonth",
            null,
            MonthPicker.MonthFormat.YYYYMM,
            0, 0,
            Color.Yellow,
            Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Month required"
        };

        Assert.False(picker.Validate());
        Assert.Equal("Month required", picker.ValidationMessage);

        picker.Value = new DateOnly(2026, 6, 15);

        Assert.True(picker.Validate());
        Assert.Empty(picker.ValidationMessage);
        Assert.Equal(new DateOnly(2026, 6, 1), picker.Value);
    }

    [Fact]
    public void StatePersistenceRestoresValueAndMonthGridState()
    {
        var screen = new Screen(50, 12);
        var picker = new MonthPicker(
            "billingMonth",
            new DateOnly(2026, 6, 1),
            MonthPicker.MonthFormat.YYYYMM,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);
        picker.OpenMonthGrid();
        picker.KeyDown("PageDown", false);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(50, 12);
        var restoredPicker = new MonthPicker(
            "billingMonth",
            null,
            MonthPicker.MonthFormat.YYYYMM,
            2, 2,
            Color.Yellow,
            Color.Black);
        restoredScreen.TopContainer.AddControl(restoredPicker);

        restoredScreen.RestoreState(state);

        Assert.Equal(new DateOnly(2026, 6, 1), restoredPicker.Value);
        Assert.Equal(2027, restoredPicker.DisplayedYear);
        Assert.Equal(new DateOnly(2027, 6, 1), restoredPicker.HighlightedMonth);
        Assert.True(restoredPicker.IsMonthGridOpen);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonthPicker("billingMonth", null, MonthPicker.MonthFormat.YYYYMM, 0, 0, Color.White, Color.Black, width: 9));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonthPicker("billingMonth", null, (MonthPicker.MonthFormat)99, 0, 0, Color.White, Color.Black));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
