namespace BlazorTUI.TUI
{
    public sealed class GridViewSortedEventArgs : EventArgs
    {
        public GridViewSortedEventArgs(
            int columnIndex,
            GridView.GridColumn? column,
            GridSortDirection direction)
        {
            ColumnIndex = columnIndex;
            Column = column;
            Direction = direction;
        }

        public int ColumnIndex { get; }

        public GridView.GridColumn? Column { get; }

        public GridSortDirection Direction { get; }
    }
}
