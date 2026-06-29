using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class CalendarTests
{
    [Fact]
    public void RendersSelectedDateAndMonthHeader()
    {
        var screen = new Screen(50, 14);
        var calendar = new Calendar(
            "bookingCalendar",
            new DateOnly(2026, 6, 15),
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(calendar);

        screen.Render();

        Assert.Contains("2026", Read(screen, 2, 2, 22));
        Assert.Contains("15", Read(screen, 2, 4, 22) + Read(screen, 2, 5, 22) + Read(screen, 2, 6, 22) + Read(screen, 2, 7, 22) + Read(screen, 2, 8, 22) + Read(screen, 2, 9, 22));
    }

    [Fact]
    public void KeyboardNavigationSelectsHighlightedDateAndRaisesEvents()
    {
        var calendar = new Calendar(
            "bookingCalendar",
            new DateOnly(2026, 6, 15),
            0, 0,
            Color.Yellow,
            Color.Black);
        var changes = new List<(DateOnly? Previous, DateOnly? Value)>();
        var selected = new List<(DateOnly? Previous, DateOnly? Value)>();
        calendar.ValueChanged += (_, args) => changes.Add((args.PreviousValue, args.Value));
        calendar.DateSelected += (_, args) => selected.Add((args.PreviousValue, args.Value));
        var container = new Container("root") { Width = 40, Height = 12 };
        container.AddControl(calendar);

        calendar.KeyDown("ArrowRight", false);
        calendar.KeyDown("Enter", false);

        Assert.Equal(new DateOnly(2026, 6, 16), calendar.Value);
        Assert.Equal(new DateOnly(2026, 6, 16), calendar.SelectedDate);
        Assert.Equal((new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 16)), changes.Single());
        Assert.Equal((new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 16)), selected.Single());
    }

    [Fact]
    public void MinMaxAndDisabledDatesPreventSelection()
    {
        var calendar = new Calendar(
            "bookingCalendar",
            new DateOnly(2026, 6, 10),
            0, 0,
            Color.Yellow,
            Color.Black,
            minDate: new DateOnly(2026, 6, 10),
            maxDate: new DateOnly(2026, 6, 12));
        calendar.AddDisabledDate(new DateOnly(2026, 6, 11));
        var container = new Container("root") { Width = 40, Height = 12 };
        container.AddControl(calendar);

        calendar.KeyDown("ArrowRight", false);
        calendar.KeyDown("Enter", false);

        Assert.Equal(new DateOnly(2026, 6, 10), calendar.Value);

        calendar.KeyDown("ArrowRight", false);
        calendar.KeyDown("Enter", false);

        Assert.Equal(new DateOnly(2026, 6, 12), calendar.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => calendar.Value = new DateOnly(2026, 6, 13));
    }

    [Fact]
    public void ClickingRenderedHeaderArrowsMovesDisplayedMonth()
    {
        var screen = new Screen(50, 14);
        var calendar = new Calendar(
            "bookingCalendar",
            new DateOnly(2026, 6, 15),
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(calendar);
        screen.Render();

        string header = Read(screen, 2, 2, 22);
        int previousArrowColumn = header.IndexOf("‹", StringComparison.Ordinal);
        int nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        Assert.True(previousArrowColumn >= 0);
        Assert.True(nextArrowColumn >= 0);

        screen.TopContainer.Click((short)(2 + previousArrowColumn), 2);

        Assert.Equal(new DateOnly(2026, 5, 1), calendar.DisplayedMonth);

        screen.Render();
        header = Read(screen, 2, 2, 22);
        nextArrowColumn = header.IndexOf("›", StringComparison.Ordinal);

        screen.TopContainer.Click((short)(2 + nextArrowColumn), 2);

        Assert.Equal(new DateOnly(2026, 6, 1), calendar.DisplayedMonth);
    }

    [Fact]
    public void RequiredValidationUsesSelectedDate()
    {
        var calendar = new Calendar(
            "bookingCalendar",
            null,
            0, 0,
            Color.Yellow,
            Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Date required"
        };

        Assert.False(calendar.Validate());
        Assert.Equal("Date required", calendar.ValidationMessage);

        calendar.Value = new DateOnly(2026, 6, 15);

        Assert.True(calendar.Validate());
        Assert.Empty(calendar.ValidationMessage);
    }

    [Fact]
    public void StatePersistenceRestoresValueAndCalendarState()
    {
        var screen = new Screen(50, 14);
        var calendar = new Calendar(
            "bookingCalendar",
            new DateOnly(2026, 6, 15),
            2, 2,
            Color.Yellow,
            Color.Black);
        screen.TopContainer.AddControl(calendar);
        calendar.KeyDown("PageDown", false);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(50, 14);
        var restoredCalendar = new Calendar(
            "bookingCalendar",
            null,
            2, 2,
            Color.Yellow,
            Color.Black);
        restoredScreen.TopContainer.AddControl(restoredCalendar);

        restoredScreen.RestoreState(state);

        Assert.Equal(new DateOnly(2026, 6, 15), restoredCalendar.Value);
        Assert.Equal(new DateOnly(2026, 7, 1), restoredCalendar.DisplayedMonth);
        Assert.Equal(new DateOnly(2026, 7, 15), restoredCalendar.HighlightedDate);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Calendar("bookingCalendar", null, 0, 0, Color.White, Color.Black, width: 21));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Calendar(
                "bookingCalendar",
                null,
                0, 0,
                Color.White,
                Color.Black,
                minDate: new DateOnly(2026, 7, 1),
                maxDate: new DateOnly(2026, 6, 1)));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
