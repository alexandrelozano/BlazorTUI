using System.Drawing;
using System.Globalization;
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
            public bool isEditable;
            public bool IsEditable { get => isEditable; set => isEditable = value; }
            private GridViewCellEditorKind editorKind = GridViewCellEditorKind.TextBox;
            public GridViewCellEditorKind EditorKind
            {
                get => editorKind;
                set
                {
                    ValidateEditorKind(value);
                    editorKind = value;
                }
            }
            public string[] editorOptions = Array.Empty<string>();
            public IReadOnlyList<string> EditorOptions
            {
                get => editorOptions;
                set => editorOptions = value?.Select(option => option ?? "").ToArray() ??
                    throw new ArgumentNullException(nameof(value));
            }
            public TuiValidationRuleCollection ValidationRules { get; } = new();
        }

        private readonly GridColumn[] columns;
        private readonly GridRow[] gridrows;
        private readonly IVirtualGridViewDataProvider? virtualRows;
        private readonly ActiveColumnFilter?[] columnFilters;
        private readonly List<int> viewRowIndexes = new();
        private ActiveRowFilter? rowFilter;
        private int pageIndex;
        private int pageSize;
        private int selectedViewRowIndex = -1;
        private int selectedColumnIndex = -1;
        private int sortColumnIndex = -1;
        private GridSortDirection sortDirection = GridSortDirection.None;
        private bool isReadOnly = true;
        private CellEditState? editState;

        public IReadOnlyList<GridColumn> Columns => columns;

        public IReadOnlyList<GridRow> Rows => gridrows;

        public bool IsVirtualized => virtualRows is not null;

        public int RowCount => SourceRowCount;

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

        public int FilteredRowCount => ViewRowCount;

        public IReadOnlyList<GridRow> CurrentPageRows
            => GetCurrentPageViewIndexes()
                .Select(viewIndex => GetSourceRow(GetSourceRowIndex(viewIndex)))
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
            => ViewRowCount == 0
                ? 1
                : (int)Math.Ceiling(ViewRowCount / (double)pageSize);

        public int SortColumnIndex => sortColumnIndex;

        public GridSortDirection SortDirection => sortDirection;

        public int SelectedRowIndex
        {
            get => selectedViewRowIndex;
            set => SelectRow(value);
        }

        public int SelectedColumnIndex
        {
            get => selectedColumnIndex;
            set => SelectCell(selectedViewRowIndex, value);
        }

        public int SelectedSourceRowIndex
            => selectedViewRowIndex >= 0 && selectedViewRowIndex < ViewRowCount
                ? GetSourceRowIndex(selectedViewRowIndex)
                : -1;

        public GridRow? SelectedRow
            => SelectedSourceRowIndex >= 0 ? GetSourceRow(SelectedSourceRowIndex) : null;

        public string? SelectedRowKey
            => SelectedSourceRowIndex >= 0 ? GetSourceRowKey(SelectedSourceRowIndex) : null;

        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                if (isReadOnly == value)
                    return;

                isReadOnly = value;
                if (isReadOnly)
                    CancelEdit(raiseEvent: false);
            }
        }

        public bool IsEditing => editState is not null;

        public int EditingRowIndex => editState?.RowIndex ?? -1;

        public int EditingSourceRowIndex => editState?.SourceRowIndex ?? -1;

        public int EditingColumnIndex => editState?.ColumnIndex ?? -1;

        public string EditingValue => editState?.Value ?? "";

        public string EditValidationMessage { get; private set; } = "";

        public event EventHandler? SelectedRowChanged;

        public event EventHandler<GridViewSelectionChangedEventArgs>? SelectionChanged;

        public event EventHandler<GridViewSortedEventArgs>? Sorted;

        public event EventHandler<GridViewPageChangedEventArgs>? PageChanged;

        public event EventHandler<GridViewFilterChangedEventArgs>? FilterChanged;

        public event EventHandler<GridViewCellEditStartedEventArgs>? CellEditStarted;

        public event EventHandler<GridViewCellEditCommittedEventArgs>? CellEditCommitted;

        public event EventHandler<GridViewCellEditCanceledEventArgs>? CellEditCanceled;

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

        public GridView(
            string name,
            GridColumn[] columns,
            IVirtualGridViewDataProvider rows,
            short X,
            short Y,
            short width,
            short height,
            Color forecolor,
            Color backgroundcolor,
            int pageSize = 0)
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)2);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)2);
            ValidateColumns(columns);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = forecolor;
            BackgroundColor = backgroundcolor;
            this.columns = columns;
            gridrows = Array.Empty<GridRow>();
            virtualRows = rows;
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
                value => value.Contains(text, comparison),
                new[] { text },
                comparison);
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
                allowedValues.Contains,
                filterValues);
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

            CancelEdit(raiseEvent: false);
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

            CancelEdit(raiseEvent: false);
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

            CancelEdit(raiseEvent: false);
            SetPageIndex(index);
            return true;
        }

        public bool NextPage()
        {
            if (pageIndex >= PageCount - 1)
                return false;

            CancelEdit(raiseEvent: false);
            SetPageIndex(pageIndex + 1);
            return true;
        }

        public bool PreviousPage()
        {
            if (pageIndex <= 0)
                return false;

            CancelEdit(raiseEvent: false);
            SetPageIndex(pageIndex - 1);
            return true;
        }

        public void SelectRow(int rowIndex)
        {
            if (rowIndex < -1 || rowIndex >= ViewRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));

            SetSelectedRowIndex(rowIndex, adjustPage: true);
        }

        public void SelectCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < -1 || rowIndex >= ViewRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            if (columnIndex < -1 || columnIndex >= columns.Length)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            if (rowIndex >= 0)
                SetSelectedRowIndex(rowIndex, adjustPage: true);

            selectedColumnIndex = columnIndex;
        }

        public void SelectSourceRow(int sourceRowIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= SourceRowCount)
                throw new ArgumentOutOfRangeException(nameof(sourceRowIndex));

            int rowIndex = FindViewRowIndex(sourceRowIndex);
            if (rowIndex < 0)
                throw new ArgumentException("The source row is not visible in this GridView.", nameof(sourceRowIndex));

            SetSelectedRowIndex(rowIndex, adjustPage: true);
        }

        public void SelectRowKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            for (int sourceRowIndex = 0; sourceRowIndex < SourceRowCount; sourceRowIndex++)
            {
                if (!string.Equals(GetSourceRowKey(sourceRowIndex), key, StringComparison.Ordinal))
                    continue;

                int rowIndex = FindViewRowIndex(sourceRowIndex);
                if (rowIndex < 0)
                    throw new ArgumentException("The row key is not visible in this GridView.", nameof(key));

                SetSelectedRowIndex(rowIndex, adjustPage: true);
                return;
            }

            throw new ArgumentException("The row key does not belong to this GridView.", nameof(key));
        }

        public bool BeginEdit()
        {
            if (selectedViewRowIndex < 0)
                return false;

            int columnIndex = selectedColumnIndex >= 0
                ? selectedColumnIndex
                : GetFirstEditableColumnIndex();
            return columnIndex >= 0 && BeginEdit(selectedViewRowIndex, columnIndex);
        }

        public bool BeginEdit(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            ValidateColumnIndex(columnIndex);

            if (IsReadOnly || !columns[columnIndex].IsEditable || virtualRows is { CanUpdate: false })
                return false;

            CancelEdit(raiseEvent: false);

            int sourceRowIndex = GetSourceRowIndex(rowIndex);
            GridRow row = GetSourceRow(sourceRowIndex);
            string value = GetCellValue(row, columnIndex);
            editState = new CellEditState(
                rowIndex,
                sourceRowIndex,
                columnIndex,
                value,
                value,
                TuiText.TextElementCount(value));
            EditValidationMessage = "";
            selectedColumnIndex = columnIndex;
            SetSelectedRowIndex(rowIndex, adjustPage: true);

            CellEditStarted?.Invoke(
                this,
                new GridViewCellEditStartedEventArgs(
                    rowIndex,
                    sourceRowIndex,
                    columnIndex,
                    row,
                    columns[columnIndex],
                    value));
            return true;
        }

        public bool BeginEditSourceRow(int sourceRowIndex, int columnIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= SourceRowCount)
                throw new ArgumentOutOfRangeException(nameof(sourceRowIndex));
            ValidateColumnIndex(columnIndex);

            int rowIndex = FindViewRowIndex(sourceRowIndex);
            if (rowIndex < 0)
                throw new ArgumentException("The source row is not visible in this GridView.", nameof(sourceRowIndex));

            return BeginEdit(rowIndex, columnIndex);
        }

        public bool CommitEdit()
        {
            if (editState is null)
                return false;

            CellEditState state = editState;
            GridColumn column = columns[state.ColumnIndex];
            string value = NormalizeEditValue(column, state.Value);
            if (!TryValidateEditValue(column, value, out string validationMessage))
            {
                EditValidationMessage = validationMessage;
                return false;
            }

            int previousSelectedRowIndex = selectedViewRowIndex;
            int previousSourceRowIndex = SelectedSourceRowIndex;
            GridRow? previousSelectedRow = SelectedRow;

            SetSourceCellValue(state.SourceRowIndex, state.ColumnIndex, value);
            editState = null;
            EditValidationMessage = "";
            RebuildViewRowIndexes();
            RestoreSelection(state.SourceRowIndex);
            selectedColumnIndex = state.ColumnIndex;
            ClampPageIndexAfterDataChange();
            RaiseSelectionChangedIfNeeded(previousSelectedRowIndex, previousSourceRowIndex, previousSelectedRow);

            CellEditCommitted?.Invoke(
                this,
                new GridViewCellEditCommittedEventArgs(
                    selectedViewRowIndex,
                    state.SourceRowIndex,
                    state.ColumnIndex,
                    GetSourceRow(state.SourceRowIndex),
                    column,
                    state.OriginalValue,
                    value));
            return true;
        }

        public bool CancelEdit()
            => CancelEdit(raiseEvent: true);

        internal void ExportGridViewState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            state.SetBoolean("IsReadOnly", IsReadOnly);
            state.SetInteger("PageIndex", pageIndex);
            state.SetInteger("PageSize", pageSize);
            state.SetInteger("SelectedRowIndex", selectedViewRowIndex);
            state.SetInteger("SelectedSourceRowIndex", SelectedSourceRowIndex);
            state.SetInteger("SelectedColumnIndex", selectedColumnIndex);
            state.SetInteger("SortColumnIndex", sortColumnIndex);
            state.SetInteger("SortDirection", (int)sortDirection);
            state.SetBoolean("IsEditing", IsEditing);
            if (editState is not null)
            {
                state.SetInteger("EditingSourceRowIndex", editState.SourceRowIndex);
                state.SetInteger("EditingColumnIndex", editState.ColumnIndex);
                state.SetString("EditingValue", editState.Value);
            }

            if (virtualRows is null)
            {
                for (int rowIndex = 0; rowIndex < gridrows.Length; rowIndex++)
                    state.SetStringList($"Row:{rowIndex}", gridrows[rowIndex].cells);
            }

            for (int columnIndex = 0; columnIndex < columnFilters.Length; columnIndex++)
            {
                ActiveColumnFilter? filter = columnFilters[columnIndex];
                if (filter is null)
                    continue;

                state.SetInteger($"Filter:{columnIndex}:Kind", (int)filter.Kind);
                state.SetString($"Filter:{columnIndex}:Description", filter.Description);
                state.SetStringList($"Filter:{columnIndex}:Values", filter.Values);
                state.SetInteger($"Filter:{columnIndex}:Comparison", (int)filter.Comparison);
            }

            if (rowFilter is not null)
                state.SetString("RowFilterDescription", rowFilter.Description);
        }

        internal void RestoreGridViewState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            CancelEdit(raiseEvent: false);

            for (int rowIndex = 0; virtualRows is null && rowIndex < gridrows.Length; rowIndex++)
            {
                if (!state.TryGetStringList($"Row:{rowIndex}", out IReadOnlyList<string> cells))
                    continue;

                int columnCount = Math.Min(gridrows[rowIndex].cells.Length, cells.Count);
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                    gridrows[rowIndex].cells[columnIndex] = cells[columnIndex] ?? "";
            }

            Array.Clear(columnFilters);
            rowFilter = null;
            for (int columnIndex = 0; columnIndex < columnFilters.Length; columnIndex++)
            {
                if (!state.TryGetInteger($"Filter:{columnIndex}:Kind", out int filterKindValue) ||
                    !Enum.IsDefined((GridViewFilterKind)filterKindValue))
                {
                    continue;
                }

                var filterKind = (GridViewFilterKind)filterKindValue;
                state.TryGetString($"Filter:{columnIndex}:Description", out string description);
                state.TryGetStringList($"Filter:{columnIndex}:Values", out IReadOnlyList<string> values);
                var filterValues = values.ToArray();

                if (filterKind == GridViewFilterKind.Text && filterValues.Length > 0)
                {
                    var comparison = StringComparison.OrdinalIgnoreCase;
                    if (state.TryGetInteger($"Filter:{columnIndex}:Comparison", out int comparisonValue) &&
                        Enum.IsDefined((StringComparison)comparisonValue))
                    {
                        comparison = (StringComparison)comparisonValue;
                    }

                    string text = filterValues[0];
                    columnFilters[columnIndex] = new ActiveColumnFilter(
                        GridViewFilterKind.Text,
                        description.Length == 0 ? text : description,
                        value => value.Contains(text, comparison),
                        filterValues,
                        comparison);
                }
                else if (filterKind == GridViewFilterKind.Exact && filterValues.Length > 0)
                {
                    var allowedValues = new HashSet<string>(filterValues, StringComparer.OrdinalIgnoreCase);
                    columnFilters[columnIndex] = new ActiveColumnFilter(
                        GridViewFilterKind.Exact,
                        description.Length == 0 ? string.Join(", ", filterValues) : description,
                        allowedValues.Contains,
                        filterValues,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            if (state.TryGetInteger("PageSize", out int restoredPageSize) &&
                restoredPageSize >= 1 &&
                (VisibleRowCapacity == 0 || restoredPageSize <= VisibleRowCapacity))
            {
                pageSize = restoredPageSize;
            }

            if (state.TryGetInteger("SortColumnIndex", out int restoredSortColumnIndex) &&
                state.TryGetInteger("SortDirection", out int restoredSortDirectionValue) &&
                restoredSortColumnIndex >= 0 &&
                restoredSortColumnIndex < columns.Length &&
                Enum.IsDefined((GridSortDirection)restoredSortDirectionValue))
            {
                sortColumnIndex = restoredSortColumnIndex;
                sortDirection = (GridSortDirection)restoredSortDirectionValue;
            }
            else
            {
                sortColumnIndex = -1;
                sortDirection = GridSortDirection.None;
            }

            RebuildViewRowIndexes();

            int selectedSourceRowIndex = state.TryGetInteger("SelectedSourceRowIndex", out int restoredSourceRowIndex)
                ? restoredSourceRowIndex
                : -1;
            RestoreSelection(selectedSourceRowIndex);
            if (selectedViewRowIndex < 0 &&
                state.TryGetInteger("SelectedRowIndex", out int restoredRowIndex) &&
                restoredRowIndex >= 0 &&
                restoredRowIndex < ViewRowCount)
            {
                selectedViewRowIndex = restoredRowIndex;
            }

            selectedColumnIndex = state.TryGetInteger("SelectedColumnIndex", out int restoredColumnIndex) &&
                restoredColumnIndex >= -1 &&
                restoredColumnIndex < columns.Length
                    ? restoredColumnIndex
                    : -1;

            pageIndex = state.TryGetInteger("PageIndex", out int restoredPageIndex)
                ? Math.Clamp(restoredPageIndex, 0, PageCount - 1)
                : 0;
            if (selectedViewRowIndex >= 0)
                pageIndex = Math.Clamp(selectedViewRowIndex / PageSize, 0, PageCount - 1);

            isReadOnly = !state.TryGetBoolean("IsReadOnly", out bool restoredReadOnly) || restoredReadOnly;
            EditValidationMessage = "";
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (!Visible)
                return false;

            if (editState is not null)
                return HandleEditKeyDown(key);

            switch (key)
            {
                case "ArrowLeft" when !IsReadOnly:
                    return MoveSelectedColumn(-1);
                case "ArrowRight" when !IsReadOnly:
                    return MoveSelectedColumn(1);
                case "ArrowUp":
                    MoveSelection(-1);
                    return ViewRowCount > 0;
                case "ArrowDown":
                    MoveSelection(1);
                    return ViewRowCount > 0;
                case "PageUp":
                    return PreviousPage();
                case "PageDown":
                    return NextPage();
                case "Home":
                    SelectFirstRow();
                    return ViewRowCount > 0;
                case "End":
                    SelectLastRow();
                    return ViewRowCount > 0;
                case "Enter":
                case "Space":
                case " ":
                    if (SelectedRow is null)
                        return false;

                    if (!IsReadOnly && BeginEdit())
                        return true;

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
            CancelEdit(raiseEvent: false);

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
            if (pageRowIndex < 0 || pageRowIndex >= VisibleRowCapacity || rowIndex >= ViewRowCount)
                return false;

            selectedColumnIndex = GetColumnIndexAt(X);
            SetSelectedRowIndex(rowIndex, adjustPage: false);
            if (!IsReadOnly && selectedColumnIndex >= 0 && columns[selectedColumnIndex].IsEditable)
                BeginEdit(rowIndex, selectedColumnIndex);

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
                    bool editingCell = IsEditingCell(localX, localY);
                    PrepareCell(
                        cell,
                        editingCell ? Color.White : selected ? BackgroundColor : ForeColor,
                        editingCell ? Color.DarkGreen : selected ? ForeColor : BackgroundColor);
                    cell.Character = character;
                    if (editingCell && IsEditCursorCell(localX, localY))
                        cell.Decoration = Cell.TextDecoration.UnderLine;
                }
            }
        }

        private void SetPageSize(int value)
        {
            int newPageSize = ValidatePageSize(value);
            if (newPageSize == pageSize)
                return;

            CancelEdit(raiseEvent: false);
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
            if (ViewRowCount == 0)
                return;

            if (selectedViewRowIndex < 0)
            {
                SetSelectedRowIndex(direction < 0 ? PageEndRowIndex : PageStartRowIndex, adjustPage: false);
                return;
            }

            int nextRowIndex = Math.Clamp(selectedViewRowIndex + direction, 0, ViewRowCount - 1);
            SetSelectedRowIndex(nextRowIndex, adjustPage: true);
        }

        private void SelectFirstRow()
        {
            if (ViewRowCount > 0)
                SetSelectedRowIndex(0, adjustPage: true);
        }

        private void SelectLastRow()
        {
            if (ViewRowCount > 0)
                SetSelectedRowIndex(ViewRowCount - 1, adjustPage: true);
        }

        private void SetSelectedRowIndex(int rowIndex, bool adjustPage)
        {
            if (selectedViewRowIndex == rowIndex)
                return;

            int previousSelectedRowIndex = selectedViewRowIndex;
            int previousSourceRowIndex = SelectedSourceRowIndex;
            GridRow? previousSelectedRow = SelectedRow;

            selectedViewRowIndex = rowIndex;
            if (selectedViewRowIndex < 0)
                selectedColumnIndex = -1;

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
            if (selectedViewRowIndex < 0 || ViewRowCount == 0)
                return;

            if (selectedViewRowIndex < PageStartRowIndex || selectedViewRowIndex > PageEndRowIndex)
                SetSelectedRowIndex(PageStartRowIndex, adjustPage: false);
        }

        private void RestoreSelection(int sourceRowIndex)
        {
            selectedViewRowIndex = sourceRowIndex < 0 ? -1 : FindViewRowIndex(sourceRowIndex);
            if (selectedViewRowIndex < 0)
                selectedColumnIndex = -1;
        }

        private void RebuildViewRowIndexes()
        {
            if (UsesSequentialVirtualView)
            {
                viewRowIndexes.Clear();
                return;
            }

            IEnumerable<int> rowIndexes = Enumerable.Range(0, SourceRowCount)
                .Where(RowMatchesFilters);
            if (sortDirection != GridSortDirection.None && sortColumnIndex >= 0)
            {
                rowIndexes = sortDirection == GridSortDirection.Ascending
                    ? rowIndexes
                        .OrderBy(index => GetCellValue(GetSourceRow(index), sortColumnIndex), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(index => index)
                    : rowIndexes
                        .OrderByDescending(index => GetCellValue(GetSourceRow(index), sortColumnIndex), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(index => index);
            }

            viewRowIndexes.Clear();
            viewRowIndexes.AddRange(rowIndexes);
        }

        private void SetColumnFilterCore(
            int columnIndex,
            GridViewFilterKind kind,
            string description,
            Func<string, bool> predicate,
            IEnumerable<string>? values = null,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            columnFilters[columnIndex] = new ActiveColumnFilter(
                kind,
                description,
                predicate,
                values?.ToArray() ?? Array.Empty<string>(),
                comparison);
            ApplyFilterChange(GetFilter(columnIndex));
        }

        private void ApplyFilterChange(GridViewFilterState filter)
        {
            CancelEdit(raiseEvent: false);
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
            GridRow row = GetSourceRow(rowIndex);
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

        private bool CancelEdit(bool raiseEvent)
        {
            if (editState is null)
                return false;

            CellEditState state = editState;
            editState = null;
            EditValidationMessage = "";

            if (raiseEvent)
            {
                CellEditCanceled?.Invoke(
                    this,
                    new GridViewCellEditCanceledEventArgs(
                        state.RowIndex,
                        state.SourceRowIndex,
                        state.ColumnIndex,
                        GetSourceRow(state.SourceRowIndex),
                        columns[state.ColumnIndex],
                        state.OriginalValue));
            }

            return true;
        }

        private bool HandleEditKeyDown(string key)
        {
            if (editState is null)
                return false;

            switch (key)
            {
                case "Enter":
                    return CommitEdit();
                case "Escape":
                    return CancelEdit();
                case "Home":
                    editState = editState with { Cursor = 0 };
                    EditValidationMessage = "";
                    return true;
                case "End":
                    editState = editState with { Cursor = TuiText.TextElementCount(editState.Value) };
                    EditValidationMessage = "";
                    return true;
            }

            GridColumn column = columns[editState.ColumnIndex];
            if (column.EditorKind == GridViewCellEditorKind.ComboBox)
            {
                return key switch
                {
                    "ArrowDown" or "ArrowRight" => MoveComboEdit(1),
                    "ArrowUp" or "ArrowLeft" => MoveComboEdit(-1),
                    _ => true
                };
            }

            if (column.EditorKind == GridViewCellEditorKind.CheckBox)
            {
                if (key is "Space" or " ")
                    ToggleCheckEdit();
                return true;
            }

            switch (key)
            {
                case "ArrowLeft":
                    editState = editState with
                    {
                        Cursor = TuiText.PreviousTextElementIndex(editState.Value, editState.Cursor)
                    };
                    return true;
                case "ArrowRight":
                    editState = editState with
                    {
                        Cursor = TuiText.NextTextElementIndex(editState.Value, editState.Cursor)
                    };
                    return true;
                case "Backspace":
                    BackspaceEdit();
                    return true;
                case "Delete":
                    DeleteEdit();
                    return true;
                default:
                    if (TuiText.TextElementCount(key) == 1 && IsAllowedEditCharacter(column, key))
                        InsertEditText(key);
                    return true;
            }
        }

        private bool MoveSelectedColumn(int direction)
        {
            if (selectedViewRowIndex < 0 || columns.Length == 0)
                return false;

            int currentColumnIndex = selectedColumnIndex < 0 ? 0 : selectedColumnIndex;
            selectedColumnIndex = Math.Clamp(currentColumnIndex + direction, 0, columns.Length - 1);
            return true;
        }

        private int GetFirstEditableColumnIndex()
        {
            for (int index = 0; index < columns.Length; index++)
            {
                if (columns[index].IsEditable)
                    return index;
            }

            return -1;
        }

        private bool MoveComboEdit(int direction)
        {
            if (editState is null)
                return false;

            string[] options = columns[editState.ColumnIndex].editorOptions;
            if (options.Length == 0)
                return true;

            int index = Array.FindIndex(options, option => option == editState.Value);
            int nextIndex = index < 0
                ? 0
                : (index + direction + options.Length) % options.Length;
            string value = options[nextIndex];
            editState = editState with
            {
                Value = value,
                Cursor = TuiText.TextElementCount(value)
            };
            EditValidationMessage = "";
            return true;
        }

        private void ToggleCheckEdit()
        {
            if (editState is null)
                return;

            (string falseValue, string trueValue) = GetCheckValues(columns[editState.ColumnIndex]);
            string value = string.Equals(editState.Value, trueValue, StringComparison.OrdinalIgnoreCase)
                ? falseValue
                : trueValue;
            editState = editState with
            {
                Value = value,
                Cursor = TuiText.TextElementCount(value)
            };
            EditValidationMessage = "";
        }

        private void BackspaceEdit()
        {
            if (editState is null || editState.Cursor <= 0)
                return;

            string value = TuiText.RemoveTextElements(editState.Value, editState.Cursor - 1, 1);
            editState = editState with
            {
                Value = value,
                Cursor = editState.Cursor - 1
            };
            EditValidationMessage = "";
        }

        private void DeleteEdit()
        {
            if (editState is null || editState.Cursor >= TuiText.TextElementCount(editState.Value))
                return;

            string value = TuiText.RemoveTextElements(editState.Value, editState.Cursor, 1);
            editState = editState with { Value = value };
            EditValidationMessage = "";
        }

        private void InsertEditText(string value)
        {
            if (editState is null)
                return;

            int contentWidth = Math.Max(0, columns[editState.ColumnIndex].Width - 1);
            string candidate = TuiText.InsertAtTextElement(editState.Value, editState.Cursor, value);
            if (TuiText.VisualWidth(candidate) > contentWidth)
                return;

            editState = editState with
            {
                Value = candidate,
                Cursor = editState.Cursor + TuiText.TextElementCount(value)
            };
            EditValidationMessage = "";
        }

        private static bool IsAllowedEditCharacter(GridColumn column, string value)
        {
            if (column.EditorKind == GridViewCellEditorKind.NumericBox)
                return value.All(character => char.IsDigit(character) || character is '-' or '.' or ',');

            if (column.EditorKind == GridViewCellEditorKind.DateBox)
                return value.All(character => char.IsDigit(character) || character is '/' or '-');

            return column.EditorKind == GridViewCellEditorKind.TextBox;
        }

        private string NormalizeEditValue(GridColumn column, string value)
            => column.EditorKind switch
            {
                GridViewCellEditorKind.CheckBox => NormalizeCheckValue(column, value),
                _ => value
            };

        private static string NormalizeCheckValue(GridColumn column, string value)
        {
            (string falseValue, string trueValue) = GetCheckValues(column);
            return string.Equals(value, trueValue, StringComparison.OrdinalIgnoreCase) ? trueValue : falseValue;
        }

        private static (string FalseValue, string TrueValue) GetCheckValues(GridColumn column)
            => column.editorOptions.Length >= 2
                ? (column.editorOptions[0], column.editorOptions[1])
                : ("False", "True");

        private static bool TryValidateEditValue(GridColumn column, string value, out string message)
        {
            message = "";
            switch (column.EditorKind)
            {
                case GridViewCellEditorKind.ComboBox:
                    if (column.editorOptions.Length > 0 &&
                        !column.editorOptions.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        message = "Value must be one of the configured options.";
                        return false;
                    }
                    break;
                case GridViewCellEditorKind.NumericBox:
                    if (!double.TryParse(value, out _))
                    {
                        message = "Value must be numeric.";
                        return false;
                    }
                    break;
                case GridViewCellEditorKind.DateBox:
                    if (!DateOnly.TryParse(value, out _))
                    {
                        message = "Value must be a valid date.";
                        return false;
                    }
                    break;
            }

            foreach (TuiValidationRule rule in column.ValidationRules)
            {
                if (!rule.IsValid(value))
                {
                    message = rule.Message;
                    return false;
                }
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
            if (pageRowIndex < 0 || pageRowIndex >= VisibleRowCapacity || rowIndex >= ViewRowCount)
                return " ";

            int sourceRowIndex = GetSourceRowIndex(rowIndex);
            return GetCharacterAt(BuildDataRow(GetSourceRow(sourceRowIndex), sourceRowIndex), localX);
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

        private string BuildDataRow(GridRow row, int sourceRowIndex)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < columns.Length; index++)
            {
                string value = editState is not null &&
                    editState.SourceRowIndex == sourceRowIndex &&
                    editState.ColumnIndex == index
                        ? GetEditDisplayValue(columns[index], editState.Value)
                        : GetCellValue(row, index);
                builder.Append(FormatCell(value, columns[index].Width));
            }

            return builder.ToString();
        }

        private static string GetEditDisplayValue(GridColumn column, string value)
        {
            if (column.EditorKind != GridViewCellEditorKind.CheckBox)
                return value;

            (_, string trueValue) = GetCheckValues(column);
            return string.Equals(value, trueValue, StringComparison.OrdinalIgnoreCase) ? "[X]" : "[ ]";
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

        private int GetColumnStart(int columnIndex)
        {
            int cursor = 0;
            for (int index = 0; index < columnIndex; index++)
                cursor += columns[index].Width;
            return cursor;
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

        private bool IsEditingCell(int localX, int localY)
        {
            if (editState is null || localY < 2)
                return false;

            int rowIndex = PageStartRowIndex + localY - 2;
            if (rowIndex != editState.RowIndex)
                return false;

            int columnStart = GetColumnStart(editState.ColumnIndex);
            int columnEnd = columnStart + columns[editState.ColumnIndex].Width - 1;
            return localX >= columnStart && localX < columnEnd;
        }

        private bool IsEditCursorCell(int localX, int localY)
        {
            if (editState is null || !IsEditingCell(localX, localY))
                return false;

            int columnStart = GetColumnStart(editState.ColumnIndex);
            int cursorColumn = columnStart + Math.Min(
                TuiText.VisualWidth(GetEditDisplayValue(columns[editState.ColumnIndex], editState.Value), editState.Cursor),
                Math.Max(0, columns[editState.ColumnIndex].Width - 2));
            return localX == cursorColumn;
        }

        private int PageStartRowIndex => Math.Min(pageIndex * PageSize, ViewRowCount);

        private int PageEndRowIndex => Math.Min(PageStartRowIndex + PageRowCapacity, ViewRowCount) - 1;

        private int VisibleRowCapacity => Math.Max(0, Height - 2);

        private int PageRowCapacity => Math.Min(PageSize, VisibleRowCapacity);

        private int DefaultPageSize => Math.Max(1, VisibleRowCapacity);

        private int SourceRowCount => virtualRows?.Count ?? gridrows.Length;

        private int ViewRowCount => UsesSequentialVirtualView ? SourceRowCount : viewRowIndexes.Count;

        private bool UsesSequentialVirtualView
            => virtualRows is not null && sortDirection == GridSortDirection.None && !HasActiveFilters;

        private int GetSourceRowIndex(int viewRowIndex)
        {
            if (viewRowIndex < 0 || viewRowIndex >= ViewRowCount)
                throw new ArgumentOutOfRangeException(nameof(viewRowIndex));

            return UsesSequentialVirtualView ? viewRowIndex : viewRowIndexes[viewRowIndex];
        }

        private int FindViewRowIndex(int sourceRowIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= SourceRowCount)
                return -1;

            return UsesSequentialVirtualView ? sourceRowIndex : viewRowIndexes.IndexOf(sourceRowIndex);
        }

        private GridRow GetSourceRow(int sourceRowIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= SourceRowCount)
                throw new ArgumentOutOfRangeException(nameof(sourceRowIndex));

            return virtualRows is null ? gridrows[sourceRowIndex] : virtualRows.GetRow(sourceRowIndex);
        }

        private string GetSourceRowKey(int sourceRowIndex)
        {
            if (sourceRowIndex < 0 || sourceRowIndex >= SourceRowCount)
                throw new ArgumentOutOfRangeException(nameof(sourceRowIndex));

            return virtualRows?.GetRowKey(sourceRowIndex) ?? sourceRowIndex.ToString(CultureInfo.InvariantCulture);
        }

        private void SetSourceCellValue(int sourceRowIndex, int columnIndex, string value)
        {
            if (virtualRows is not null)
            {
                virtualRows.SetCellValue(sourceRowIndex, columnIndex, value);
                return;
            }

            gridrows[sourceRowIndex].cells[columnIndex] = value;
        }

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

        private static void ValidateEditorKind(GridViewCellEditorKind editorKind)
        {
            if (!Enum.IsDefined(editorKind))
                throw new ArgumentOutOfRangeException(nameof(editorKind));
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
                ValidateEditorKind(column.EditorKind);
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
            Func<string, bool> Predicate,
            string[] Values,
            StringComparison Comparison);

        private sealed record ActiveRowFilter(
            string Description,
            Func<GridRow, bool> Predicate);

        private sealed record CellEditState(
            int RowIndex,
            int SourceRowIndex,
            int ColumnIndex,
            string OriginalValue,
            string Value,
            int Cursor);
    }
}
