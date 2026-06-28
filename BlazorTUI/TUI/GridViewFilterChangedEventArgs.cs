namespace BlazorTUI.TUI
{
    public sealed class GridViewFilterChangedEventArgs : EventArgs
    {
        public GridViewFilterChangedEventArgs(GridViewFilterState filter, int filteredRowCount)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentOutOfRangeException.ThrowIfLessThan(filteredRowCount, 0);

            Filter = filter;
            FilteredRowCount = filteredRowCount;
        }

        public GridViewFilterState Filter { get; }

        public int ColumnIndex => Filter.ColumnIndex;

        public GridView.GridColumn? Column => Filter.Column;

        public GridViewFilterKind Kind => Filter.Kind;

        public string Description => Filter.Description;

        public bool IsActive => Filter.IsActive;

        public int FilteredRowCount { get; }
    }
}
