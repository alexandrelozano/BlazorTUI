namespace BlazorTUI.TUI
{
    public sealed class GridViewCellEditCanceledEventArgs : EventArgs
    {
        public GridViewCellEditCanceledEventArgs(
            int rowIndex,
            int sourceRowIndex,
            int columnIndex,
            GridView.GridRow row,
            GridView.GridColumn column,
            string originalValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rowIndex, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(sourceRowIndex, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(columnIndex, 0);
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(column);
            ArgumentNullException.ThrowIfNull(originalValue);

            RowIndex = rowIndex;
            SourceRowIndex = sourceRowIndex;
            ColumnIndex = columnIndex;
            Row = row;
            Column = column;
            OriginalValue = originalValue;
        }

        public int RowIndex { get; }

        public int SourceRowIndex { get; }

        public int ColumnIndex { get; }

        public GridView.GridRow Row { get; }

        public GridView.GridColumn Column { get; }

        public string OriginalValue { get; }
    }
}
