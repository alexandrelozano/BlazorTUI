namespace BlazorTUI.TUI
{
    public sealed class VirtualGridViewQuery
    {
        public static VirtualGridViewQuery Empty { get; } = new(
            Array.Empty<VirtualGridViewColumnFilter>(),
            null,
            null,
            -1,
            GridSortDirection.None,
            -1,
            0,
            1);

        public VirtualGridViewQuery(
            IReadOnlyList<VirtualGridViewColumnFilter>? columnFilters,
            string? rowFilterDescription,
            Func<GridView.GridRow, bool>? rowPredicate,
            int sortColumnIndex,
            GridSortDirection sortDirection,
            int groupColumnIndex,
            int pageIndex,
            int pageSize)
        {
            if (!Enum.IsDefined(sortDirection))
                throw new ArgumentOutOfRangeException(nameof(sortDirection));
            ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

            ColumnFilters = columnFilters?.ToArray() ?? Array.Empty<VirtualGridViewColumnFilter>();
            RowFilterDescription = rowFilterDescription;
            RowPredicate = rowPredicate;
            SortColumnIndex = sortColumnIndex;
            SortDirection = sortDirection;
            GroupColumnIndex = groupColumnIndex;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public IReadOnlyList<VirtualGridViewColumnFilter> ColumnFilters { get; }

        public string? RowFilterDescription { get; }

        public Func<GridView.GridRow, bool>? RowPredicate { get; }

        public int SortColumnIndex { get; }

        public GridSortDirection SortDirection { get; }

        public int GroupColumnIndex { get; }

        public int PageIndex { get; }

        public int PageSize { get; }

        public int PageStartIndex => PageIndex * PageSize;

        public bool HasFilters => ColumnFilters.Count > 0 || RowPredicate is not null;

        public bool HasSorting => SortDirection != GridSortDirection.None && SortColumnIndex >= 0;

        public bool HasGrouping => GroupColumnIndex >= 0;
    }
}
