namespace BlazorTUI.TUI
{
    public sealed class VirtualListBoxQuery
    {
        public static VirtualListBoxQuery Empty { get; } = new("", 0, 1);

        public VirtualListBoxQuery(string? searchText, int pageIndex, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

            SearchText = searchText ?? "";
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public string SearchText { get; }

        public int PageIndex { get; }

        public int PageSize { get; }

        public int PageStartIndex => PageIndex * PageSize;

        public bool HasSearch => SearchText.Length > 0;
    }
}
