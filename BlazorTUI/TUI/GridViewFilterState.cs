namespace BlazorTUI.TUI
{
    public sealed class GridViewFilterState
    {
        public GridViewFilterState(
            int columnIndex,
            GridView.GridColumn? column,
            GridViewFilterKind kind,
            string description,
            bool isActive)
        {
            ArgumentNullException.ThrowIfNull(description);

            ColumnIndex = columnIndex;
            Column = column;
            Kind = kind;
            Description = description;
            IsActive = isActive;
        }

        public int ColumnIndex { get; }

        public GridView.GridColumn? Column { get; }

        public GridViewFilterKind Kind { get; }

        public string Description { get; }

        public bool IsActive { get; }
    }
}
