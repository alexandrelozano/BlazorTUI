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
    public void TextFilterReducesRowsRendersIndicatorAndRaisesTypedEvent()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        GridViewFilterChangedEventArgs? filterChanged = null;
        grid.FilterChanged += (_, args) => filterChanged = args;
        screen.TopContainer.AddControl(grid);

        grid.SetTextFilter(2, "Cooking");
        screen.Render();

        Assert.True(grid.HasActiveFilters);
        Assert.True(grid.IsFilterActive(2));
        Assert.Equal(2, grid.FilteredRowCount);
        Assert.Equal(1, grid.PageCount);
        Assert.Equal("Cooking", grid.GetFilter(2).Description);
        Assert.Equal(GridViewFilterKind.Text, grid.GetFilter(2).Kind);
        Assert.NotNull(filterChanged);
        Assert.Equal("Status", filterChanged.Column?.Title);
        Assert.Equal(GridViewFilterKind.Text, filterChanged.Kind);
        Assert.Equal(2, filterChanged.FilteredRowCount);
        Assert.Contains("◊", Read(screen, 1, 1, 24));
        Assert.Equal(new[] { "Pepperoni", "Hawaiian" }, grid.CurrentPageRows.Select(row => row.Cells[1]));
    }

    [Fact]
    public void ExactPredicateAndRowFiltersCanBeCleared()
    {
        GridView grid = CreateOrdersGrid(pageSize: 3);

        grid.SetExactFilter(1, new[] { "Calzone", "Veggie" });
        Assert.Equal(2, grid.FilteredRowCount);
        Assert.Equal(new[] { "Calzone", "Veggie" }, grid.CurrentPageRows.Select(row => row.Cells[1]));

        grid.SetColumnFilter(1, value => value.Length > 7, "Long pizza names");
        Assert.Equal(3, grid.FilteredRowCount);
        Assert.Equal(GridViewFilterKind.Predicate, grid.GetFilter(1).Kind);
        Assert.Equal("Long pizza names", grid.GetFilter(1).Description);

        grid.SetRowFilter(row => int.Parse(row.Cells[0]) % 2 == 1, "Odd orders");
        Assert.Equal(2, grid.FilteredRowCount);
        Assert.Equal(GridViewFilterKind.RowPredicate, grid.RowFilter?.Kind);

        grid.ClearFilter(1);
        Assert.False(grid.IsFilterActive(1));
        Assert.Equal(3, grid.FilteredRowCount);

        grid.ClearFilters();
        Assert.False(grid.HasActiveFilters);
        Assert.Equal(5, grid.FilteredRowCount);
        Assert.All(grid.Filters, filter => Assert.False(filter.IsActive));
    }

    [Fact]
    public void FilteringOutSelectedRowRaisesSelectionChanged()
    {
        GridView grid = CreateOrdersGrid(pageSize: 3);
        var selections = new List<(int Row, int Source)>();
        grid.SelectionChanged += (_, args) => selections.Add((args.RowIndex, args.SourceRowIndex));
        grid.SelectSourceRow(2);

        grid.SetTextFilter(2, "Cooking");

        Assert.Equal(-1, grid.SelectedRowIndex);
        Assert.Equal(-1, grid.SelectedSourceRowIndex);
        Assert.Null(grid.SelectedRow);
        Assert.Contains((-1, -1), selections);
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
    public void CellEditingCommitsTextAndRaisesTypedEvents()
    {
        GridView grid = CreateEditableGrid(pageSize: 3);
        grid.IsReadOnly = false;
        grid.Columns[1].IsEditable = true;

        GridViewCellEditStartedEventArgs? started = null;
        GridViewCellEditCommittedEventArgs? committed = null;
        grid.CellEditStarted += (_, args) => started = args;
        grid.CellEditCommitted += (_, args) => committed = args;

        Assert.True(grid.BeginEdit(1, 1));
        Assert.True(grid.IsEditing);
        Assert.Equal("Calzone", grid.EditingValue);
        Assert.Equal(1, grid.EditingRowIndex);
        Assert.Equal(1, grid.EditingSourceRowIndex);
        Assert.Equal(1, grid.EditingColumnIndex);

        grid.KeyDown("End", false);
        grid.KeyDown("X", false);

        Assert.Equal("CalzoneX", grid.EditingValue);
        Assert.True(grid.CommitEdit());

        Assert.False(grid.IsEditing);
        Assert.Equal("CalzoneX", grid.Rows[1].Cells[1]);
        Assert.NotNull(started);
        Assert.Equal("Pizza", started.Column.Title);
        Assert.Equal("Calzone", started.Value);
        Assert.NotNull(committed);
        Assert.Equal("Calzone", committed.PreviousValue);
        Assert.Equal("CalzoneX", committed.Value);
        Assert.Equal("Pizza", committed.Column.Title);
    }

    [Fact]
    public void CellEditingCanBeCanceledWithoutChangingTheRow()
    {
        GridView grid = CreateEditableGrid(pageSize: 3);
        grid.IsReadOnly = false;
        grid.Columns[1].IsEditable = true;
        GridViewCellEditCanceledEventArgs? canceled = null;
        grid.CellEditCanceled += (_, args) => canceled = args;

        Assert.True(grid.BeginEdit(0, 1));
        grid.KeyDown("End", false);
        grid.KeyDown("X", false);

        Assert.True(grid.CancelEdit());

        Assert.False(grid.IsEditing);
        Assert.Equal("Pepperoni", grid.Rows[0].Cells[1]);
        Assert.NotNull(canceled);
        Assert.Equal("Pepperoni", canceled.OriginalValue);
        Assert.Equal("Pizza", canceled.Column.Title);
    }

    [Fact]
    public void CellEditingSupportsComboCheckNumericAndDateEditors()
    {
        GridView grid = CreateTypedEditorGrid();

        Assert.True(grid.BeginEdit(0, 0));
        grid.KeyDown("ArrowDown", false);
        Assert.True(grid.CommitEdit());
        Assert.Equal("Ready", grid.Rows[0].Cells[0]);

        Assert.True(grid.BeginEdit(0, 1));
        grid.KeyDown("Space", false);
        Assert.True(grid.CommitEdit());
        Assert.Equal("Yes", grid.Rows[0].Cells[1]);

        Assert.True(grid.BeginEdit(0, 2));
        grid.KeyDown("Home", false);
        grid.KeyDown("Delete", false);
        grid.KeyDown("Delete", false);
        grid.KeyDown(".", false);

        Assert.False(grid.CommitEdit());
        Assert.True(grid.IsEditing);
        Assert.Equal("Value must be numeric.", grid.EditValidationMessage);
        Assert.True(grid.CancelEdit());

        string[] invalidDateCells = grid.Rows[0].Cells.ToArray();
        invalidDateCells[3] = "not-date";
        grid.Rows[0].Cells = invalidDateCells;

        Assert.True(grid.BeginEdit(0, 3));
        Assert.False(grid.CommitEdit());
        Assert.Equal("Value must be a valid date.", grid.EditValidationMessage);
    }

    [Fact]
    public void CellEditingHonorsColumnValidationRules()
    {
        GridView grid = CreateEditableGrid(pageSize: 3);
        grid.IsReadOnly = false;
        grid.Columns[1].IsEditable = true;
        grid.Columns[1].ValidationRules.Add(
            value => value is string text && text.Length >= 3,
            "Pizza must have at least 3 characters.");
        string[] invalidPizzaCells = grid.Rows[1].Cells.ToArray();
        invalidPizzaCells[1] = "AB";
        grid.Rows[1].Cells = invalidPizzaCells;

        Assert.True(grid.BeginEditSourceRow(1, 1));
        Assert.False(grid.CommitEdit());
        Assert.Equal("Pizza must have at least 3 characters.", grid.EditValidationMessage);

        grid.KeyDown("End", false);
        grid.KeyDown("C", false);

        Assert.True(grid.CommitEdit());
        Assert.Equal("ABC", grid.Rows[1].Cells[1]);
    }

    [Fact]
    public void CellEditingIsReadOnlyByDefault()
    {
        GridView grid = CreateEditableGrid(pageSize: 3);
        grid.Columns[1].IsEditable = true;

        Assert.False(grid.BeginEdit(0, 1));

        grid.SelectCell(0, 1);
        grid.KeyDown("Enter", false);

        Assert.False(grid.IsEditing);
        Assert.Equal("Pepperoni", grid.Rows[0].Cells[1]);

        grid.IsReadOnly = false;
        Assert.True(grid.BeginEdit(0, 1));
        grid.IsReadOnly = true;
        Assert.False(grid.IsEditing);
    }

    [Fact]
    public void ColumnLayoutCanHideResizeReorderAndExportVisibleRows()
    {
        var screen = new Screen(40, 8);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        screen.TopContainer.AddControl(grid);

        grid.HideColumn(0);
        grid.SetColumnWidth(1, 10);
        grid.MoveColumn(2, 0);
        screen.Render();

        Assert.Equal(new[] { 2, 1 }, grid.VisibleColumnIndexes);
        Assert.StartsWith("Status│Pizza", Read(screen, 1, 1, 18));
        Assert.DoesNotContain("Order", grid.ExportCsv());
        Assert.Equal(
            "Status,Pizza" + Environment.NewLine +
            "Cooking,Pepperoni" + Environment.NewLine +
            "Hold,Calzone" + Environment.NewLine +
            "Ready,Veggie" + Environment.NewLine +
            "Done,Margherita" + Environment.NewLine +
            "Cooking,Hawaiian",
            grid.ExportCsv());
        Assert.Contains("Order", grid.ExportCsv(visibleColumnsOnly: false));
    }

    [Fact]
    public void FilterRowEditsTextFilterFromKeyboard()
    {
        GridView grid = CreateOrdersGrid(pageSize: 3);
        grid.ShowFilterRow = true;

        Assert.True(grid.BeginFilterEdit(1));
        grid.KeyDown("C", false);
        grid.KeyDown("a", false);
        grid.KeyDown("Enter", false);

        Assert.False(grid.IsFilterEditing);
        Assert.True(grid.IsFilterActive(1));
        Assert.Equal("Ca", grid.GetFilter(1).Description);
        Assert.Equal(new[] { "Calzone" }, grid.CurrentPageRows.Select(row => row.Cells[1]));

        Assert.True(grid.BeginFilterEdit(1));
        grid.KeyDown("Backspace", false);
        grid.KeyDown("Backspace", false);
        grid.KeyDown("Enter", false);

        Assert.False(grid.HasActiveFilters);
    }

    [Fact]
    public void GroupingAndAggregateFootersRenderComputedRows()
    {
        var screen = new Screen(60, 12);
        GridView grid = CreateOrdersGrid(pageSize: 3);
        grid.GroupByColumn(2);
        grid.AddCountFooter("Rows", 2);
        screen.TopContainer.AddControl(grid);

        screen.Render();

        Assert.Equal(2, grid.GroupColumnIndex);
        Assert.Contains("◆", Read(screen, 1, 1, 24));
        Assert.Contains("Status: Cooking", Read(screen, 1, 3, 30));
        Assert.Contains("Rows", Read(screen, 1, 5, 24) + Read(screen, 1, 6, 24) + Read(screen, 1, 7, 24));
    }

    [Fact]
    public async Task LoadRowsAsyncReplacesMaterializedRowsAndPreservesGridOperations()
    {
        GridView grid = CreateOrdersGrid(pageSize: 3);

        await grid.LoadRowsAsync(_ => Task.FromResult<IEnumerable<GridView.GridRow>>(new[]
        {
            new GridView.GridRow { Cells = new[] { "10", "Funghi", "Ready" } },
            new GridView.GridRow { Cells = new[] { "11", "Diavola", "Cooking" } }
        }));

        Assert.False(grid.IsLoading);
        Assert.Equal(2, grid.RowCount);
        grid.SortByColumn(1);

        Assert.Equal(new[] { "Diavola", "Funghi" }, grid.CurrentPageRows.Select(row => row.Cells[1]));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetTextFilter(9, "x"));
        Assert.Throws<ArgumentNullException>(() => grid.SetTextFilter(0, null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetExactFilter(0, (IEnumerable<string>)null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetColumnFilter(0, null!, "Custom"));
        Assert.Throws<ArgumentException>(() => grid.SetColumnFilter(0, _ => true, " "));
        Assert.Throws<ArgumentNullException>(() => grid.SetRowFilter(null!, "Custom"));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SelectedColumnIndex = 9);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SelectCell(0, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SelectCell(9, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.BeginEdit(9, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.BeginEdit(0, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.BeginEditSourceRow(9, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Columns[0].EditorKind = (GridViewCellEditorKind)99);
        Assert.Throws<ArgumentNullException>(() => grid.Columns[0].EditorOptions = null!);
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

    private static GridView CreateEditableGrid(int pageSize)
    {
        var columns = new[]
        {
            new GridView.GridColumn { Title = "Order", Width = 7 },
            new GridView.GridColumn { Title = "Pizza", Width = 12 },
            new GridView.GridColumn { Title = "Status", Width = 9 }
        };
        var rows = new[]
        {
            new GridView.GridRow { Cells = new[] { "1", "Pepperoni", "Cooking" } },
            new GridView.GridRow { Cells = new[] { "2", "Calzone", "Hold" } },
            new GridView.GridRow { Cells = new[] { "3", "Veggie", "Ready" } }
        };

        return new GridView(
            "editableOrdersGrid",
            columns,
            rows,
            1, 1, 30, 5,
            Color.Yellow,
            Color.Black,
            pageSize);
    }

    private static GridView CreateTypedEditorGrid()
    {
        var columns = new[]
        {
            new GridView.GridColumn
            {
                Title = "Status",
                Width = 9,
                IsEditable = true,
                EditorKind = GridViewCellEditorKind.ComboBox,
                EditorOptions = new[] { "New", "Ready", "Done" }
            },
            new GridView.GridColumn
            {
                Title = "Paid",
                Width = 7,
                IsEditable = true,
                EditorKind = GridViewCellEditorKind.CheckBox,
                EditorOptions = new[] { "No", "Yes" }
            },
            new GridView.GridColumn
            {
                Title = "Qty",
                Width = 6,
                IsEditable = true,
                EditorKind = GridViewCellEditorKind.NumericBox
            },
            new GridView.GridColumn
            {
                Title = "Date",
                Width = 12,
                IsEditable = true,
                EditorKind = GridViewCellEditorKind.DateBox
            }
        };
        var rows = new[]
        {
            new GridView.GridRow { Cells = new[] { "New", "No", "10", "2026-06-28" } }
        };

        return new GridView(
            "typedEditorsGrid",
            columns,
            rows,
            1, 1, 34, 4,
            Color.Yellow,
            Color.Black,
            pageSize: 1)
        {
            IsReadOnly = false
        };
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
