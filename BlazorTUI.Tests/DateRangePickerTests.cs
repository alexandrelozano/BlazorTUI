using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class DateRangePickerTests
{
    [Fact]
    public void RendersClosedValueAndPopupCalendar()
    {
        var screen = new Screen(60, 16);
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);

        screen.Render();

        Assert.StartsWith("[2026/06/10..2026/06/14", Read(screen, 2, 2, 24));

        picker.OpenCalendar();
        screen.Render();

        Assert.True(picker.IsCalendarOpen);
        Assert.Contains("2026", Read(screen, 2, 3, 22));
        Assert.Contains("10", Read(screen, 2, 5, 22) + Read(screen, 2, 6, 22) + Read(screen, 2, 7, 22) + Read(screen, 2, 8, 22) + Read(screen, 2, 9, 22) + Read(screen, 2, 10, 22));
    }

    [Fact]
    public void KeyboardSelectionUsesTwoStepsAndRaisesTypedEvents()
    {
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black);
        var changes = new List<(DateOnly? PreviousStart, DateOnly? PreviousEnd, DateOnly? Start, DateOnly? End)>();
        picker.ValueChanged += (_, args) => changes.Add((args.PreviousStartValue, args.PreviousEndValue, args.StartValue, args.EndValue));
        var container = new Container("root") { Width = 50, Height = 12 };
        container.AddControl(picker);

        Assert.True(picker.KeyDown("Enter", false));
        Assert.True(picker.IsDropDownOpen);
        Assert.Equal(DateRangePickerSelectionTarget.Start, picker.SelectionTarget);

        picker.KeyDown("ArrowRight", false);
        picker.KeyDown("Enter", false);

        Assert.True(picker.IsDropDownOpen);
        Assert.Equal(new DateOnly(2026, 6, 11), picker.StartValue);
        Assert.Null(picker.EndValue);
        Assert.Equal(DateRangePickerSelectionTarget.End, picker.SelectionTarget);

        picker.KeyDown("ArrowRight", false);
        picker.KeyDown("Enter", false);

        Assert.False(picker.IsDropDownOpen);
        Assert.Equal(new DateOnly(2026, 6, 11), picker.StartValue);
        Assert.Equal(new DateOnly(2026, 6, 12), picker.EndValue);
        Assert.Equal(2, changes.Count);
        Assert.Equal((new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 14), new DateOnly(2026, 6, 11), null), changes[0]);
        Assert.Equal((new DateOnly(2026, 6, 11), null, new DateOnly(2026, 6, 11), new DateOnly(2026, 6, 12)), changes[1]);
    }

    [Fact]
    public void EndSelectionBeforeStartNormalizesRange()
    {
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            null,
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black);
        var container = new Container("root") { Width = 50, Height = 12 };
        container.AddControl(picker);

        picker.OpenCalendar();
        picker.KeyDown("ArrowLeft", false);
        picker.KeyDown("ArrowLeft", false);
        picker.KeyDown("Enter", false);

        Assert.Equal(new DateOnly(2026, 6, 8), picker.StartValue);
        Assert.Equal(new DateOnly(2026, 6, 10), picker.EndValue);
    }

    [Fact]
    public void PageKeysMoveDisplayedMonthWithoutChangingRange()
    {
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black);
        var container = new Container("root") { Width = 50, Height = 12 };
        container.AddControl(picker);

        picker.OpenCalendar();
        picker.KeyDown("PageDown", false);

        Assert.Equal(new DateOnly(2026, 7, 1), picker.DisplayedMonth);
        Assert.Equal(new DateOnly(2026, 7, 10), picker.HighlightedDate);
        Assert.Equal(new DateOnly(2026, 6, 10), picker.StartValue);
        Assert.Equal(new DateOnly(2026, 6, 14), picker.EndValue);
    }

    [Fact]
    public void ClickingRenderedHeaderArrowsMovesDisplayedMonth()
    {
        var screen = new Screen(60, 16);
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 14),
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);
        picker.OpenCalendar();
        screen.Render();

        string header = Read(screen, 2, 3, 22);
        int previousArrowColumn = header.IndexOf("‹", StringComparison.Ordinal);
        int nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        Assert.True(previousArrowColumn >= 0);
        Assert.True(nextArrowColumn >= 0);

        screen.TopContainer.Click((short)(2 + previousArrowColumn), 3);

        Assert.Equal(new DateOnly(2026, 5, 1), picker.DisplayedMonth);

        screen.Render();
        header = Read(screen, 2, 3, 22);
        nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        screen.TopContainer.Click((short)(2 + nextArrowColumn), 3);

        Assert.Equal(new DateOnly(2026, 6, 1), picker.DisplayedMonth);
    }

    [Fact]
    public void RequiredValidationUsesCompleteRange()
    {
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            null,
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Range required"
        };

        picker.ValidationRules.Add(
            value => value is DateRangePickerValue range && range.EndValue.DayNumber - range.StartValue.DayNumber <= 7,
            "Use one week or less");

        Assert.False(picker.Validate());
        Assert.Equal("Range required", picker.ValidationMessage);

        picker.EndValue = new DateOnly(2026, 6, 20);

        Assert.False(picker.Validate());
        Assert.Equal("Use one week or less", picker.ValidationMessage);

        picker.EndValue = new DateOnly(2026, 6, 14);

        Assert.True(picker.Validate());
        Assert.Empty(picker.ValidationMessage);
    }

    [Fact]
    public void StatePersistenceRestoresValueAndCalendarState()
    {
        var screen = new Screen(60, 16);
        var picker = new DateRangePicker(
            "travelRange",
            new DateOnly(2026, 6, 10),
            null,
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);
        picker.OpenCalendar();
        picker.KeyDown("PageDown", false);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(60, 16);
        var restoredPicker = new DateRangePicker(
            "travelRange",
            null,
            null,
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        restoredScreen.TopContainer.AddControl(restoredPicker);

        restoredScreen.RestoreState(state);

        Assert.Equal(new DateOnly(2026, 6, 10), restoredPicker.StartValue);
        Assert.Null(restoredPicker.EndValue);
        Assert.Equal(new DateOnly(2026, 7, 1), restoredPicker.DisplayedMonth);
        Assert.Equal(new DateOnly(2026, 7, 10), restoredPicker.HighlightedDate);
        Assert.Equal(DateRangePickerSelectionTarget.End, restoredPicker.SelectionTarget);
        Assert.True(restoredPicker.IsCalendarOpen);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DateRangePicker("travelRange", null, null, DateBox.DateFormat.YYYYMMDD, 0, 0, Color.White, Color.Black, width: 24));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DateRangePicker("travelRange", null, null, (DateBox.DateFormat)99, 0, 0, Color.White, Color.Black));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
