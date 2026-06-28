using System.Drawing;
using System.Text;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class GridView : Control
    {
        public class GridRow
        {
            public string[] cells = Array.Empty<string>();

            public IReadOnlyList<string> Cells
            {
                get => cells;
                set => cells = value?.ToArray() ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public class GridColumn
        {
            public string title = "";
            public string Title { get => title; set => title = value ?? ""; }
            public short width;
            public short Width { get => width; set => width = value; }
        }

        private readonly GridColumn[] columns;
        private readonly GridRow[] gridrows;
        private readonly ActiveColumnFilter?[] columnFilters;
        private readonly List<int> viewRowIndexes = new();
        private ActiveRowFilter? rowFilter;
        private int pageIndex;
        private int pageSize;
        private int selectedViewRowIndex = -1;
        private int sortColumnIndex = -1;
        private GridSortDirection sortDirection = GridSortDirection.None;

        public IReadOnlyList<GridColumn> Columns => columns;

        public IReadOnlyList<GridRow> Rows => gridrows;

        public IReadOnlyList<GridViewFilterState> Filters
            => Enumerable.Range(0, columns.Length)
                .Select(GetFilter)
                .ToList();

        public GridViewFilterState? RowFilter
            => rowFilter is null
                ? null
                : new GridViewFilterState(
                    -1,
                    null,
                    GridViewFilterKind.RowPredicate,
                    rowFilter.Description,
                    isActive: true);

        public bool HasActiveFilters
            => rowFilter is not null || columnFilters.Any(filter => filter is not null);

        public int FilteredRowCount => viewRowIndexes.Count;

        public IReadOnlyList<GridRow> CurrentPageRows
            => GetCurrentPageViewIndexes()
                .Select(viewIndex => gridrows[viewRowIndexes[viewIndex]])
                .ToList();

        public int PageIndex
        {
            get => pageIndex;
            set => GoToPage(value);
        }

        public int PageSize
        {
            get => pageSize;
            set => SetPageSize(value);
        }

        public int PageCount
            => viewRowIndexes.Count == 0
                ? 1
                : (int)Math.Ceiling(viewRowIndexes.Count / (double)pageSize);

        public int SortColumnIndex => sortColumnIndex;

        public GridSortDirection SortDirection => sortDirection;

        public int SelectedRowIndex
        {
            get => selectedViewRowIndex;
            set => SelectRow(value);
        }

        public int SelectedSourceRowIndex
            => selectedViewRowIndex >= 0 && selectedViewRowIndex < viewRowIndexes.Count
                ? viewRowIndexes[selectedViewRowIndex]
                : -1;

        public GridRow? SelectedRow
            => SelectedSourceRowIndex >= 0 ? gridrows[SelectedSourceRowIndex] : null;

        public event EventHandler? SelectedRowChanged;

        public event EventHandler<GridViewSelectionChangedEventArgs>? SelectionChanged;

        public event EventHandler<GridViewSortedEventArgs>? Sorted;

        public event EventHandler<GridViewPageChangedEventArgs>? PageChanged;

        public event EventHandler<GridViewFilterChangedEventArgs>? FilterChanged;

        public GridView(
            string name,
            GridColumn[] columns,
            GridRow[] gridrows,
            short X,
            short Y,
            short width,
            short height,
            Color forecolor,
            Color backgroundcolor,
            int pageSize = 0)
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(gridrows);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)2);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)2);
            ValidateColumns(columns);
            ValidateRows(columns, gridrows);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = forecolor;
            BackgroundColor = backgroundcolor;
            this.columns = columns;
            this.gridrows = gridrows;
            columnFilters = new ActiveColumnFilter?[columns.Length];
            TabStop = true;
            this.pageSize = pageSize == 0
                ? DefaultPageSize
                : ValidatePageSize(pageSize);

            RebuildViewRowIndexes();
        }

        public void SetTextFilter(
            int columnIndex,
            string text,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            ValidateColumnIndex(columnIndex);
            ArgumentNullException.ThrowIfNull(text);
            ValidateStringComparison(comparison);

            if (text.Length == 0)
            {
                ClearFilter(columnIndex);
                return;
            }

            SetColumnFilterCore(
                columnIndex,
                GridViewFilterKind.Text,
                text,
                value => value.Contains(text, comparison));
        }

        public void SetExactFilter(int columnIndex, string value, StringComparer? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(value);
            SetExactFilter(columnIndex, new[] { value }, comparer);
        }

        public void SetExactFilter(int columnIndex, IEnumerable<string> values, StringComparer? comparer = null)
        {
            ValidateColumnIndex(columnIndex);
            ArgumentNullException.ThrowIfNull(values);

            string[] filterValues = values
                .Select(value => value ?? throw new ArgumentException("Filter values cannot contain null.", nameof(values)))
                .ToArray();
            if (filterValues.Length == 0)
            {
                ClearFilter(columnIndex);
                return;
            }

            var allowedValues = new HashSet<string>(filterValues, comparer ?? StringComparer.OrdinalIgnoreCase);
            SetColumnFilterCore(
                columnIndex,
                GridViewFilterKind.Exact,
                string.Join(", ", filterValues),
                allowedValues.Contains);
        }

        public void SetColumnFilter(int columnIndex, Func<string, bool> predicate, string description)
        {
            ValidateColumnIndex(columnIndex);
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            SetColumnFilterCore(columnIndex, GridViewFilterKind.Predicate, description, predicate);
        }

        public void SetRowFilter(Func<GridRow, bool> predicate, string description)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            rowFilter = new ActiveRowFilter(description, predicate);
            ApplyFilterChange(new GridViewFilterState(
                -1,
                null,
                GridViewFilterKind.RowPredicate,
                description,
                isActive: true));
        }

        public void ClearFilter(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            if (columnFilters[columnIndex] is null)
                return;

            columnFilters[columnIndex] = null;
            ApplyFilterChange(GetFilter(columnIndex));
        }

        public void ClearRowFilter()
        {
            if (rowFilter is null)
                return;

            rowFilter = null;
            ApplyFilterChange(new GridViewFilterState(
                -1,
                null,
                GridViewFilterKind.RowPredicate,
                "",
                isActive: false));
        }

        public void ClearFilters()
        {
            if (!HasActiveFilters)
                return;

            Array.Clear(columnFilters);
            rowFilter = null;
            ApplyFilterChange(new GridViewFilterState(
                -1,
                null,
                GridViewFilterKind.None,
                "",
                isActive: false));
        }

        public bool IsFilterActive(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            return columnFilters[columnIndex] is not null;
        }

        public GridViewFilterState GetFilter(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            ActiveColumnFilter? filter = columnFilters[columnIndex];
            return filter is null
                ? new GridViewFilterState(
                    columnIndex,
                    columns[columnIndex],
                    GridViewFilterKind.None,
                    "",
                    isActive: false)
                : new GridViewFilterState(
                    columnIndex,
                    columns[columnIndex],
                    filter.Kind,
                    filter.Description,
                    isActive: true);
        }

        public void SortByColumn(int columnIndex)
        {
            ValidateColumnIndex(columnIndex);
            GridSortDirection nextDirection =
                sortColumnIndex == columnIndex && sortDirection == GridSortDirection.Ascending
                    ? GridSortDirection.Descending
                    : GridSortDirection.Ascending;

            SortByColumn(columnIndex, nextDirection);
        }

        public void SortByColumn(int columnIndex, GridSortDirection direction)
        {
            ValidateColumnIndex(columnIndex);
            ValidateSortDirection(direction);

            int previousSourceRowIndex = SelectedSourceRowIndex;
            sortColumnIndex = direction == GridSortDirection.None ? -1 : columnIndex;
            sortDirection = direction;
            RebuildViewRowIndexes();
            RestoreSelection(previousSourceRowIndex);
            ClampPageIndexAfterDataChange();
            Sorted?.Invoke(
                this,
                new GridViewSortedEventArgs(
                    sortColumnIndex,
                    sortColumnIndex >= 0 ? columns[sortColumnIndex] : null,
                    sortDirection));
        }

        public void ClearSort()
        {
            if (sortDirection == GridSortDirection.None)
                return;

            int previousSourceRowIndex = SelectedSourceRowIndex;
            sortColumnIndex = -1;
            sortDirection = GridSortDirection.None;
            RebuildViewRowIndexes();
            RestoreSelection(previousSourceRowIndex);
            ClampPageIndexAfterDataChange();
            Sorted?.Invoke(this, new GridViewSortedEventArgs(-1, null, GridSortDirection.None));
        }

        public bool GoToPage(int index)
        {
            if (index < 0 || index >= PageCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == pageIndex)
                return false;

            SetPageIndex(index);
            return true;
        }

        public bool NextPage()
        {
            if (pageIndex >= PageCount - 1)
                return false;

            SetPageIndex(pageIndex + 1);
            return true;
        }

        public bool PreviousPage()
        {
            if (pageIndex <= 0)
                return false;

            SetPageIndex(pageIndex - 1);
            return true;
        }

        public void SelectRow(int rowIndex)
        {
            if (rowIndex < -1 || rowIndex >= viewRowIndexes.Count)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));

            SetSelectedRowIndex(rowIndex, adjustPage: true);
        }

        public void SelectSourceRow(int sourceRowIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= gridrows.Length)
                throw new ArgumentOutOfRangeException(nameof(sourceRowIndex));

            int rowIndex = viewRowIndexes.IndexOf(sourceRowIndex);
            if (rowIndex < 0)
                throw new ArgumentException("The source row is not visible in this GridView.", nameof(sourceRowIndex));

            SetSelectedRowIndex(rowIndex, adjustPage: true);
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (!Visible)
                return false;

            switch (key)
            {
                case "ArrowUp":
                    MoveSelection(-1);
                    return viewRowIndexes.Count > 0;
                case "ArrowDown":
                    MoveSelection(1);
                    return viewRowIndexes.Count > 0;
                case "PageUp":
                    return PreviousPage();
                case "PageDown":
                    return NextPage();
                case "Home":
                    SelectFirstRow();
                    return viewRowIndexes.Count > 0;
                case "End":
                    SelectLastRow();
                    return viewRowIndexes.Count > 0;
                case "Enter":
                case "Space":
                case " ":
                    if (SelectedRow is null)
                        return false;

                    NotifyClicked();
                    return true;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y < 0 || Y >= Height)
                return false;

            container.TopContainer().SetFocus(Name);

            if (X == Width - 1)
            {
                bool pageChanged = Y == 0
                    ? PreviousPage()
                    : Y == Height - 1 && NextPage();
                if (pageChanged)
                    NotifyClicked();

                return pageChanged;
            }

            if (Y == 0)
            {
                int columnIndex = GetColumnIndexAt(X);
                if (columnIndex < 0)
                    return false;

                SortByColumn(columnIndex);
                NotifyClicked();
                return true;
            }

            if (Y < 2)
                return false;

            int pageRowIndex = Y - 2;
            int rowIndex = PageStartRowIndex + pageRowIndex;
            if (pageRowIndex < 0 || pageRowIndex >= VisibleRowCapacity || rowIndex >= viewRowIndexes.Count)
                return false;

            SetSelectedRowIndex(rowIndex, adjustPage: false);
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            for (int localY = 0; localY < Height; localY++)
            {
                for (int localX = 0; localX < Width; localX++)
                {
                    if (!TryGetCell(rows, localX, localY, out Cell cell))
                        continue;

                    string character = GetRenderedCharacter(localX, localY);
                    bool selected = IsSelectedDataCell(localY);
                    PrepareCell(
                        cell,
                        selected ? BackgroundColor : ForeColor,
                        selected ? ForeColor : BackgroundColor);
                    cell.Character = character;
                }
            }
        }

        private void SetPageSize(int value)
        {
            int newPageSize = ValidatePageSize(value);
            if (newPageSize == pageSize)
                return;

            pageSize = newPageSize;
            ClampPageIndexAfterDataChange(forcePageChanged: true);
        }

        private void SetPageIndex(int index)
        {
            int previousPageIndex = pageIndex;
            pageIndex = index;
            EnsureSelectionIsOnCurrentPage();
            PageChanged?.Invoke(
                this,
                new GridViewPageChangedEventArgs(previousPageIndex, pageIndex, PageCount, PageSize));
        }

        private void ClampPageIndexAfterDataChange(bool forcePageChanged = false)
        {
            int previousPageIndex = pageIndex;
            pageIndex = Math.Clamp(pageIndex, 0, PageCount - 1);
            if (selectedViewRowIndex >= 0)
                pageIndex = Math.Clamp(selectedViewRowIndex / PageSize, 0, PageCount - 1);

            if (forcePageChanged || previousPageIndex != pageIndex)
            {
                PageChanged?.Invoke(
                    this,
                    new GridViewPageChangedEventArgs(previousPageIndex, pageIndex, PageCount, PageSize));
            }
        }

        private void MoveSelection(int direction)
        {
            if (viewRowIndexes.Count == 0)
                return;

            if (selectedViewRowIndex < 0)
            {
                SetSelectedRowIndex(direction < 0 ? PageEndRowIndex : PageStartRowIndex, adjustPage: false);
                return;
            }

            int nextRowIndex = Math.Clamp(selectedViewRowIndex + direction, 0, viewRowIndexes.Count - 1);
            SetSelectedRowIndex(nextRowIndex, adjustPage: true);
        }

        private void SelectFirstRow()
        {
            if (viewRowIndexes.Count > 0)
                SetSelectedRowIndex(0, adjustPage: true);
        }

        private void SelectLastRow()
        {
            if (viewRowIndexes.Count > 0)
                SetSelectedRowIndex(viewRowIndexes.Count - 1, adjustPage: true);
        }

        private void SetSelectedRowIndex(int rowIndex, bool adjustPage)
        {
            if (selectedViewRowIndex == rowIndex)
                return;

            int previousSelectedRowIndex = selectedViewRowIndex;
            int previousSourceRowIndex = SelectedSourceRowIndex;
            GridRow? previousSelectedRow = SelectedRow;

            selectedViewRowIndex = rowIndex;

            if (adjustPage && selectedViewRowIndex >= 0)
            {
                int targetPageIndex = selectedViewRowIndex / PageSize;
                if (targetPageIndex != pageIndex)
                    SetPageIndex(targetPageIndex);
            }

            SelectedRowChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(
                this,
                new GridViewSelectionChangedEventArgs(
                    previousSelectedRowIndex,
                    selectedViewRowIndex,
                    previousSourceRowIndex,
                    SelectedSourceRowIndex,
                    previousSelectedRow,
                    SelectedRow));
        }

        private void EnsureSelectionIsOnCurrentPage()
        {
            if (selectedViewRowIndex < 0 || viewRowIndexes.Count == 0)
                return;

            if (selectedViewRowIndex < PageStartRowIndex || selectedViewRowIndex > PageEndRowIndex)
                SetSelectedRowIndex(PageStartRowIndex, adjustPage: false);
        }

        private void RestoreSelection(int sourceRowIndex)
        {
            selectedViewRowIndex = sourceRowIndex < 0 ? -1 : viewRowIndexes.IndexOf(sourceRowIndex);
        }

        private void RebuildViewRowIndexes()
        {
            IEnumerable<int> rowIndexes = Enumerable.Range(0, gridrows.Length)
                .Where(RowMatchesFilters);
            if (sortDirection != GridSortDirection.None && sortColumnIndex >= 0)
            {
                rowIndexes = sortDirection == GridSortDirection.Ascending
                    ? rowIndexes
                        .OrderBy(index => GetCellValue(gridrows[index], sortColumnIndex), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(index => index)
                    : rowIndexes
                        .OrderByDescending(index => GetCellValue(gridrows[index], sortColumnIndex), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(index => index);
            }

            viewRowIndexes.Clear();
            viewRowIndexes.AddRange(rowIndexes);
        }

        private void SetColumnFilterCore(
            int columnIndex,
            GridViewFilterKind kind,
            string description,
            Func<string, bool> predicate)
        {
            columnFilters[columnIndex] = new ActiveColumnFilter(kind, description, predicate);
            ApplyFilterChange(GetFilter(columnIndex));
        }

        private void ApplyFilterChange(GridViewFilterState filter)
        {
            int previousSelectedRowIndex = selectedViewRowIndex;
            int previousSourceRowIndex = SelectedSourceRowIndex;
            GridRow? previousSelectedRow = SelectedRow;

            RebuildViewRowIndexes();
            RestoreSelection(previousSourceRowIndex);
            ClampPageIndexAfterDataChange();
            RaiseSelectionChangedIfNeeded(previousSelectedRowIndex, previousSourceRowIndex, previousSelectedRow);
            FilterChanged?.Invoke(this, new GridViewFilterChangedEventArgs(filter, FilteredRowCount));
        }

        private void RaiseSelectionChangedIfNeeded(
            int previousSelectedRowIndex,
            int previousSourceRowIndex,
            GridRow? previousSelectedRow)
        {
            if (previousSelectedRowIndex == selectedViewRowIndex &&
                previousSourceRowIndex == SelectedSourceRowIndex)
            {
                return;
            }

            SelectedRowChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(
                this,
                new GridViewSelectionChangedEventArgs(
                    previousSelectedRowIndex,
                    selectedViewRowIndex,
                    previousSourceRowIndex,
                    SelectedSourceRowIndex,
                    previousSelectedRow,
                    SelectedRow));
        }

        private bool RowMatchesFilters(int rowIndex)
        {
            GridRow row = gridrows[rowIndex];
            if (rowFilter is not null && !rowFilter.Predicate(row))
                return false;

            for (int columnIndex = 0; columnIndex < columnFilters.Length; columnIndex++)
            {
                ActiveColumnFilter? filter = columnFilters[columnIndex];
                if (filter is not null && !filter.Predicate(GetCellValue(row, columnIndex)))
                    return false;
            }

            return true;
        }

        private string GetRenderedCharacter(int localX, int localY)
        {
            if (localX == Width - 1)
                return GetPageNavigationCharacter(localY);

            if (localY == 0)
                return GetCharacterAt(BuildHeaderRow(), localX);

            if (localY == 1)
                return "─";

            int pageRowIndex = localY - 2;
            int rowIndex = PageStartRowIndex + pageRowIndex;
            if (pageRowIndex < 0 || pageRowIndex >= VisibleRowCapacity || rowIndex >= viewRowIndexes.Count)
                return " ";

            return GetCharacterAt(BuildDataRow(gridrows[viewRowIndexes[rowIndex]]), localX);
        }

        private string GetPageNavigationCharacter(int localY)
        {
            if (localY == 0)
                return pageIndex > 0 ? "↑" : " ";

            if (localY == Height - 1)
                return pageIndex < PageCount - 1 ? "↓" : " ";

            return PageCount > 1 ? "│" : " ";
        }

        private bool IsSelectedDataCell(int localY)
        {
            if (localY < 2 || selectedViewRowIndex < 0)
                return false;

            int rowIndex = PageStartRowIndex + localY - 2;
            return rowIndex == selectedViewRowIndex;
        }

        private string BuildHeaderRow()
        {
            var builder = new StringBuilder();
            for (int index = 0; index < columns.Length; index++)
            {
                GridColumn column = columns[index];
                string title = column.Title;
                string filterIndicator = columnFilters[index] is not null ? "◊" : "";
                string sortIndicator = index == sortColumnIndex
                    ? sortDirection == GridSortDirection.Ascending ? "▲" : "▼"
                    : "";
                string indicator = filterIndicator + sortIndicator;
                builder.Append(FormatCell(title, column.Width, indicator));
            }

            return builder.ToString();
        }

        private string BuildDataRow(GridRow row)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < columns.Length; index++)
                builder.Append(FormatCell(GetCellValue(row, index), columns[index].Width));

            return builder.ToString();
        }

        private static string FormatCell(string text, short width, string indicator = "")
        {
            if (width <= 0)
                return "";

            if (width == 1)
                return "│";

            int contentWidth = width - 1;
            int textWidth = Math.Max(0, contentWidth - TuiText.VisualWidth(indicator));
            string content = TuiText.PadRightToVisualWidth(text, textWidth);
            return $"{content}{indicator}│";
        }

        private static string GetCharacterAt(string text, int index)
            => TuiText.CellAt(text, index);

        private int GetColumnIndexAt(short localX)
        {
            int cursor = 0;
            for (int index = 0; index < columns.Length; index++)
            {
                int nextCursor = cursor + columns[index].Width;
                if (localX >= cursor && localX < nextCursor - 1)
                    return index;

                cursor = nextCursor;
            }

            return -1;
        }

        private IEnumerable<int> GetCurrentPageViewIndexes()
        {
            for (int index = PageStartRowIndex; index <= PageEndRowIndex; index++)
                yield return index;
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            if (container is null)
            {
                cell = null!;
                return false;
            }

            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private int PageStartRowIndex => Math.Min(pageIndex * PageSize, viewRowIndexes.Count);

        private int PageEndRowIndex => Math.Min(PageStartRowIndex + PageRowCapacity, viewRowIndexes.Count) - 1;

        private int VisibleRowCapacity => Math.Max(0, Height - 2);

        private int PageRowCapacity => Math.Min(PageSize, VisibleRowCapacity);

        private int DefaultPageSize => Math.Max(1, VisibleRowCapacity);

        private static string GetCellValue(GridRow row, int columnIndex)
            => columnIndex >= 0 && columnIndex < row.cells.Length ? row.cells[columnIndex] ?? "" : "";

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }

        private void ValidateColumnIndex(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= columns.Length)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        private int ValidatePageSize(int value)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            if (VisibleRowCapacity > 0 && value > VisibleRowCapacity)
                throw new ArgumentOutOfRangeException(nameof(value), "PageSize cannot exceed the number of visible data rows.");

            return value;
        }

        private static void ValidateSortDirection(GridSortDirection direction)
        {
            if (!Enum.IsDefined(direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
        }

        private static void ValidateStringComparison(StringComparison comparison)
        {
            if (!Enum.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison));
        }

        private static void ValidateColumns(IReadOnlyList<GridColumn> columns)
        {
            if (columns.Count == 0)
                throw new ArgumentException("GridView requires at least one column.", nameof(columns));

            foreach (GridColumn column in columns)
            {
                ArgumentNullException.ThrowIfNull(column);
                ArgumentOutOfRangeException.ThrowIfLessThan(column.Width, (short)1);
            }
        }

        private static void ValidateRows(IReadOnlyList<GridColumn> columns, IReadOnlyList<GridRow> rows)
        {
            foreach (GridRow row in rows)
            {
                ArgumentNullException.ThrowIfNull(row);
                if (row.cells.Length < columns.Count)
                    throw new ArgumentException("Every GridView row must contain at least one cell for each column.", nameof(rows));
            }
        }

        private sealed record ActiveColumnFilter(
            GridViewFilterKind Kind,
            string Description,
            Func<string, bool> Predicate);

        private sealed record ActiveRowFilter(
            string Description,
            Func<GridRow, bool> Predicate);
    }
}
