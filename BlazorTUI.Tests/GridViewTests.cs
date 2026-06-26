using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class GridViewTests
{
    [Fact]
    public void SortByColumnOrdersRowsAndRaisesTypedEvent()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        GridViewSortedEventArgs? sorted = null;
        grid.Sorted += (_, args) => sorted = args;
        screen.TopContainer.AddControl(grid);

        grid.SortByColumn(1);
        screen.Render();

        Assert.Equal(1, grid.SortColumnIndex);
        Assert.Equal(GridSortDirection.Ascending, grid.SortDirection);
        Assert.NotNull(sorted);
        Assert.Equal("Pizza", sorted.Column?.Title);
        Assert.Equal(GridSortDirection.Ascending, sorted.Direction);
        Assert.StartsWith("Order │Pizza ▲│Status│", Read(screen, 1, 1, 22));
        Assert.StartsWith("2     │Calzone│Hold  │", Read(screen, 1, 3, 22));
        Assert.Equal("Calzone", grid.CurrentPageRows[0].Cells[1]);

        grid.SortByColumn(1);
        screen.Render();

        Assert.Equal(GridSortDirection.Descending, grid.SortDirection);
        Assert.StartsWith("3     │Veggie │Ready │", Read(screen, 1, 3, 22));
    }

    [Fact]
    public void HeaderClickSortsColumnAndFocusesGrid()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        screen.TopContainer.AddControl(grid);

        screen.TopContainer.Click(10, 1);

        Assert.True(grid.Focus);
        Assert.Equal(1, grid.SortColumnIndex);
        Assert.Equal(GridSortDirection.Ascending, grid.SortDirection);
    }

    [Fact]
    public void PaginationMovesBetweenPagesAndRaisesTypedEvent()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 2);
        var pages = new List<(int Previous, int Current, int Count, int Size)>();
        grid.PageChanged += (_, args) =>
            pages.Add((args.PreviousPageIndex, args.PageIndex, args.PageCount, args.PageSize));
        screen.TopContainer.AddControl(grid);

        Assert.Equal(3, grid.PageCount);
        Assert.True(grid.NextPage());
        screen.Render();

        Assert.Equal(1, grid.PageIndex);
        Assert.Equal(new[] { (0, 1, 3, 2) }, pages);
        Assert.StartsWith("3     │Veggie │Ready │", Read(screen, 1, 3, 22));
        Assert.StartsWith("4     │Margher│Done  │", Read(screen, 1, 4, 22));

        Assert.True(grid.PreviousPage());
        Assert.Equal(0, grid.PageIndex);
    }

    [Fact]
    public void KeyboardNavigatesRowsAndPages()
    {
        var grid = CreateOrdersGrid(pageSize: 2);
        var selections = new List<(int Row, int Source, string? Status)>();
        grid.SelectionChanged += (_, args) =>
            selections.Add((args.RowIndex, args.SourceRowIndex, args.Row?.Cells[2]));

        grid.KeyDown("ArrowDown", false);
        grid.KeyDown("ArrowDown", false);
        grid.KeyDown("ArrowDown", false);
        grid.KeyDown("PageDown", false);
        grid.KeyDown("End", false);

        Assert.Equal(4, grid.SelectedRowIndex);
        Assert.Equal(4, grid.SelectedSourceRowIndex);
        Assert.Equal(2, grid.PageIndex);
        Assert.Equal("Cooking", grid.SelectedRow?.Cells[2]);
        Assert.Contains((2, 2, "Ready"), selections);
    }

    [Fact]
    public void MouseClickSelectsVisibleRowAndRaisesTypedEvents()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        GridViewSelectionChangedEventArgs? selection = null;
        int legacyEvents = 0;
        grid.SelectedRowChanged += (_, _) => legacyEvents++;
        grid.SelectionChanged += (_, args) => selection = args;
        screen.TopContainer.AddControl(grid);

        screen.TopContainer.Click(2, 4);

        Assert.Equal(1, grid.SelectedRowIndex);
        Assert.Equal(1, grid.SelectedSourceRowIndex);
        Assert.Equal("Calzone", grid.SelectedRow?.Cells[1]);
        Assert.Equal(-1, selection?.PreviousRowIndex);
        Assert.Equal(1, selection?.RowIndex);
        Assert.Equal(1, legacyEvents);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentException>(() =>
            new GridView(
                "orders",
                Array.Empty<GridView.GridColumn>(),
                Array.Empty<GridView.GridRow>(),
                0, 0, 10, 4,
                Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new GridView(
                "orders",
                new[] { new GridView.GridColumn { Title = "Order", Width = 6 } },
                new[] { new GridView.GridRow { Cells = Array.Empty<string>() } },
                0, 0, 10, 4,
                Color.White, Color.Black));

        GridView grid = CreateOrdersGrid(pageSize: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SortByColumn(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SortByColumn(0, (GridSortDirection)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.PageIndex = 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.PageSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SelectedRowIndex = 9);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SelectSourceRow(9));
    }

    private static GridView CreateOrdersGrid(int pageSize)
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
            new GridView.GridRow { Cells = new[] { "2", "Calzone", "Hold" } },
            new GridView.GridRow { Cells = new[] { "3", "Veggie", "Ready" } },
            new GridView.GridRow { Cells = new[] { "4", "Margherita", "Done" } },
            new GridView.GridRow { Cells = new[] { "5", "Hawaiian", "Cooking" } }
        };

        return new GridView(
            "ordersGrid",
            columns,
            rows,
            1, 1, 24, 5,
            Color.Yellow,
            Color.Black,
            pageSize);
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
