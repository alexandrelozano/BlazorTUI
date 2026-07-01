namespace BlazorTUI.TUI
{
    public sealed class VirtualGridViewColumnFilter
    {
        public VirtualGridViewColumnFilter(
            int columnIndex,
            GridViewFilterKind kind,
            string description,
            IReadOnlyList<string>? values,
            StringComparison comparison,
            Func<string, bool>? predicate)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            ArgumentNullException.ThrowIfNull(description);
            if (!Enum.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison));

            ColumnIndex = columnIndex;
            Kind = kind;
            Description = description;
            Values = values?.Select(value => value ?? "").ToArray() ?? Array.Empty<string>();
            Comparison = comparison;
            Predicate = predicate;
        }

        public int ColumnIndex { get; }

        public GridViewFilterKind Kind { get; }

        public string Description { get; }

        public IReadOnlyList<string> Values { get; }

        public StringComparison Comparison { get; }

        public Func<string, bool>? Predicate { get; }
    }
}
