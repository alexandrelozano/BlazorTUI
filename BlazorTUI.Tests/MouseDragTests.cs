using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class MouseDragTests
{
    [Fact]
    public void SplitPanelDragMovesSplitter()
    {
        var splitPanel = new SplitPanel(
            "layout",
            0,
            0,
            20,
            5,
            SplitPanelOrientation.Vertical,
            5,
            Color.White,
            Color.Black);

        Assert.True(splitPanel.BeginDrag(5, 2));
        Assert.True(splitPanel.Drag(5, 2, 9, 2));
        Assert.Equal((short)9, splitPanel.SplitterPosition);

        Assert.True(splitPanel.EndDrag(5, 2, 200, 2));
        Assert.Equal((short)18, splitPanel.SplitterPosition);
    }

    [Fact]
    public void GridViewDragResizesAndReordersColumns()
    {
        GridView grid = CreateOrdersGrid();
        var root = new Container("root") { Width = 40, Height = 10 };
        root.AddControl(grid);

        Assert.True(grid.BeginDrag(6, 0));
        Assert.True(grid.Drag(6, 0, 9, 0));
        Assert.Equal((short)10, grid.Columns[0].Width);

        Assert.True(grid.BeginDrag(1, 0));
        Assert.False(grid.Drag(1, 0, 17, 0));
        Assert.True(grid.EndDrag(1, 0, 17, 0));
        Assert.Equal(new[] { 1, 2, 0 }, grid.VisibleColumnIndexes);
    }

    [Fact]
    public void ListBoxDragSelectsContiguousRange()
    {
        var list = new ListBox(
            "items",
            new List<string> { "One", "Two", "Three", "Four" },
            multipleSelection: true,
            0,
            0,
            12,
            4,
            Color.White,
            Color.Black);

        Assert.True(list.BeginDrag(1, 0));
        Assert.True(list.Drag(1, 0, 1, 2));
        Assert.Equal(new[] { "One", "Two", "Three" }, list.SelectedItems);

        Assert.False(list.EndDrag(1, 0, 1, 2));
    }

    [Fact]
    public void ScrollViewerDragMovesScrollbarThumb()
    {
        var viewer = new ScrollViewer("viewer", 0, 0, 18, 4, Color.White, Color.Black);
        viewer.AddControl(new Label("top", "Top", 0, 0, 3, Color.Yellow, Color.Black));
        viewer.AddControl(new Label("bottom", "Bottom", 0, 10, 6, Color.Yellow, Color.Black));

        Assert.True(viewer.BeginDrag(17, 1));
        Assert.True(viewer.Drag(17, 1, 17, 2));
        Assert.Equal(viewer.MaximumVerticalOffset, viewer.VerticalOffset);
    }

    [Fact]
    public void CalendarDragSelectsDate()
    {
        var calendar = new Calendar(
            "calendar",
            new DateOnly(2026, 6, 10),
            0,
            0,
            Color.Yellow,
            Color.Black,
            cultureOptions: TuiCultureOptions.Invariant);
        var root = new Container("root") { Width = 30, Height = 10 };
        root.AddControl(calendar);
        (short startX, short startY) = GetCalendarPoint(calendar.DisplayedMonth, new DateOnly(2026, 6, 10));
        (short endX, short endY) = GetCalendarPoint(calendar.DisplayedMonth, new DateOnly(2026, 6, 14));

        Assert.True(calendar.BeginDrag(startX, startY));
        Assert.True(calendar.Drag(startX, startY, endX, endY));

        Assert.Equal(new DateOnly(2026, 6, 14), calendar.Value);
    }

    [Fact]
    public void DatePickerDragSelectsDateAndClosesCalendar()
    {
        var picker = new DatePicker(
            "date",
            new DateOnly(2026, 6, 10),
            DateBox.DateFormat.YYYYMMDD,
            0,
            0,
            Color.Yellow,
            Color.Black,
            cultureOptions: TuiCultureOptions.Invariant);
        var root = new Container("root") { Width = 40, Height = 12 };
        root.AddControl(picker);
        picker.OpenCalendar();
        (short startX, short startY) = GetPopupCalendarPoint(picker.DisplayedMonth, new DateOnly(2026, 6, 10));
        (short endX, short endY) = GetPopupCalendarPoint(picker.DisplayedMonth, new DateOnly(2026, 6, 14));

        Assert.True(picker.BeginDrag(startX, startY));
        Assert.True(picker.Drag(startX, startY, endX, endY));
        Assert.True(picker.EndDrag(startX, startY, endX, endY));

        Assert.Equal(new DateOnly(2026, 6, 14), picker.Value);
        Assert.False(picker.IsCalendarOpen);
    }

    [Fact]
    public void DateRangePickerDragSelectsRangeAndClosesCalendar()
    {
        var picker = new DateRangePicker(
            "range",
            new DateOnly(2026, 6, 10),
            null,
            DateBox.DateFormat.YYYYMMDD,
            0,
            0,
            Color.Yellow,
            Color.Black,
            cultureOptions: TuiCultureOptions.Invariant);
        var root = new Container("root") { Width = 40, Height = 12 };
        root.AddControl(picker);
        picker.OpenCalendar();
        (short startX, short startY) = GetPopupCalendarPoint(picker.DisplayedMonth, new DateOnly(2026, 6, 10));
        (short endX, short endY) = GetPopupCalendarPoint(picker.DisplayedMonth, new DateOnly(2026, 6, 14));

        Assert.True(picker.BeginDrag(startX, startY));
        Assert.True(picker.Drag(startX, startY, endX, endY));
        Assert.True(picker.EndDrag(startX, startY, endX, endY));

        Assert.Equal(new DateOnly(2026, 6, 10), picker.StartValue);
        Assert.Equal(new DateOnly(2026, 6, 14), picker.EndValue);
        Assert.False(picker.IsCalendarOpen);
    }

    private static GridView CreateOrdersGrid()
    {
        var columns = new[]
        {
            new GridView.GridColumn { Title = "Order", Width = 7 },
            new GridView.GridColumn { Title = "Pizza", Width = 8 },
            new GridView.GridColumn { Title = "Status", Width = 7 }
        };
        var rows = new[]
        {
            new GridView.GridRow { Cells = new[] { "1", "Pepperoni", "Cooking" } },
            new GridView.GridRow { Cells = new[] { "2", "Calzone", "Hold" } }
        };

        return new GridView(
            "ordersGrid",
            columns,
            rows,
            0,
            0,
            24,
            5,
            Color.Yellow,
            Color.Black,
            pageSize: 2);
    }

    private static (short X, short Y) GetCalendarPoint(DateOnly displayedMonth, DateOnly date)
    {
        (int weekRow, int dayColumn) = GetCalendarCell(displayedMonth, date);
        return ((short)(dayColumn * 3), (short)(weekRow + 2));
    }

    private static (short X, short Y) GetPopupCalendarPoint(DateOnly displayedMonth, DateOnly date)
    {
        (int weekRow, int dayColumn) = GetCalendarCell(displayedMonth, date);
        return ((short)(dayColumn * 3), (short)(weekRow + 3));
    }

    private static (int WeekRow, int DayColumn) GetCalendarCell(DateOnly displayedMonth, DateOnly date)
    {
        int firstDayOfWeek = (int)System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek;
        int monthStartDayOfWeek = (int)displayedMonth.DayOfWeek;
        int offset = (monthStartDayOfWeek - firstDayOfWeek + 7) % 7;
        int dayOffset = date.DayNumber - displayedMonth.DayNumber + offset;
        return (dayOffset / 7, dayOffset % 7);
    }
}
