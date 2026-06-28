namespace BlazorTUI.TUI
{
    public sealed class GridViewCellEditStartedEventArgs : EventArgs
    {
        public GridViewCellEditStartedEventArgs(
            int rowIndex,
            int sourceRowIndex,
            int columnIndex,
            GridView.GridRow row,
            GridView.GridColumn column,
            string value)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rowIndex, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(sourceRowIndex, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(columnIndex, 0);
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(column);
            ArgumentNullException.ThrowIfNull(value);

            RowIndex = rowIndex;
            SourceRowIndex = sourceRowIndex;
            ColumnIndex = columnIndex;
            Row = row;
            Column = column;
            Value = value;
        }

        public int RowIndex { get; }

        public int SourceRowIndex { get; }

        public int ColumnIndex { get; }

        public GridView.GridRow Row { get; }

        public GridView.GridColumn Column { get; }

        public string Value { get; }
    }
}
