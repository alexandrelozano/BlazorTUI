namespace BlazorTUI.TUI
{
    public sealed class GridViewSelectionChangedEventArgs : EventArgs
    {
        public GridViewSelectionChangedEventArgs(
            int previousRowIndex,
            int rowIndex,
            int previousSourceRowIndex,
            int sourceRowIndex,
            GridView.GridRow? previousRow,
            GridView.GridRow? row)
        {
            PreviousRowIndex = previousRowIndex;
            RowIndex = rowIndex;
            PreviousSourceRowIndex = previousSourceRowIndex;
            SourceRowIndex = sourceRowIndex;
            PreviousRow = previousRow;
            Row = row;
        }

        public int PreviousRowIndex { get; }

        public int RowIndex { get; }

        public int PreviousSourceRowIndex { get; }

        public int SourceRowIndex { get; }

        public GridView.GridRow? PreviousRow { get; }

        public GridView.GridRow? Row { get; }
    }
}
