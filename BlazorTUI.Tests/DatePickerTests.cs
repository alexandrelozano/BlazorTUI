using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class DatePickerTests
{
    [Fact]
    public void RendersClosedValueAndPopupCalendar()
    {
        var screen = new Screen(50, 16);
        var picker = new DatePicker(
            "deliveryDate",
            new DateOnly(2026, 6, 29),
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);

        screen.Render();

        Assert.StartsWith("[2026/06/29", Read(screen, 2, 2, 12));

        picker.OpenCalendar();
        screen.Render();

        Assert.True(picker.IsCalendarOpen);
        Assert.Contains("2026", Read(screen, 2, 3, 22));
        Assert.Contains("29", Read(screen, 2, 5, 22) + Read(screen, 2, 6, 22) + Read(screen, 2, 7, 22) + Read(screen, 2, 8, 22) + Read(screen, 2, 9, 22) + Read(screen, 2, 10, 22));
    }

    [Fact]
    public void KeyboardNavigationSelectsHighlightedDateAndRaisesEvent()
    {
        var picker = new DatePicker(
            "deliveryDate",
            new DateOnly(2026, 6, 29),
            DateBox.DateFormat.YYYYMMDD,
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
        Assert.Equal(new DateOnly(2026, 6, 30), picker.Value);
        Assert.Equal(new DateOnly(2026, 6, 29), changes.Single().Previous);
        Assert.Equal(new DateOnly(2026, 6, 30), changes.Single().Value);
    }

    [Fact]
    public void PageKeysMoveDisplayedMonthWithoutChangingValue()
    {
        var picker = new DatePicker(
            "deliveryDate",
            new DateOnly(2026, 6, 29),
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black);
        var container = new Container("root") { Width = 40, Height = 12 };
        container.AddControl(picker);

        picker.OpenCalendar();
        picker.KeyDown("PageDown", false);

        Assert.Equal(new DateOnly(2026, 7, 1), picker.DisplayedMonth);
        Assert.Equal(new DateOnly(2026, 7, 29), picker.HighlightedDate);
        Assert.Equal(new DateOnly(2026, 6, 29), picker.Value);
    }

    [Fact]
    public void ClickingRenderedHeaderArrowsMovesDisplayedMonth()
    {
        var screen = new Screen(50, 16);
        var picker = new DatePicker(
            "deliveryDate",
            new DateOnly(2026, 6, 29),
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
    public void RequiredValidationUsesSelectedDate()
    {
        var picker = new DatePicker(
            "deliveryDate",
            null,
            DateBox.DateFormat.YYYYMMDD,
            0, 0,
            Color.Yellow,
            Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Date required"
        };

        Assert.False(picker.Validate());
        Assert.Equal("Date required", picker.ValidationMessage);

        picker.Value = new DateOnly(2026, 6, 29);

        Assert.True(picker.Validate());
        Assert.Empty(picker.ValidationMessage);
    }

    [Fact]
    public void StatePersistenceRestoresValueAndCalendarState()
    {
        var screen = new Screen(50, 16);
        var picker = new DatePicker(
            "deliveryDate",
            new DateOnly(2026, 6, 29),
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(picker);
        picker.OpenCalendar();
        picker.KeyDown("PageDown", false);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(50, 16);
        var restoredPicker = new DatePicker(
            "deliveryDate",
            null,
            DateBox.DateFormat.YYYYMMDD,
            2, 2,
            Color.Yellow,
            Color.Black);
        restoredScreen.TopContainer.AddControl(restoredPicker);

        restoredScreen.RestoreState(state);

        Assert.Equal(new DateOnly(2026, 6, 29), restoredPicker.Value);
        Assert.Equal(new DateOnly(2026, 7, 1), restoredPicker.DisplayedMonth);
        Assert.True(restoredPicker.IsCalendarOpen);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DatePicker("deliveryDate", null, DateBox.DateFormat.YYYYMMDD, 0, 0, Color.White, Color.Black, width: 12));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DatePicker("deliveryDate", null, (DateBox.DateFormat)99, 0, 0, Color.White, Color.Black));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
