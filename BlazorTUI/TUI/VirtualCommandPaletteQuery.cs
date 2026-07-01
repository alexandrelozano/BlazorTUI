namespace BlazorTUI.TUI
{
    public sealed class VirtualCommandPaletteQuery
    {
        public static VirtualCommandPaletteQuery Empty { get; } = new("", 0, 1);

        public VirtualCommandPaletteQuery(string? searchText, int scrollIndex, int visibleCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(scrollIndex);
            ArgumentOutOfRangeException.ThrowIfLessThan(visibleCount, 1);

            SearchText = searchText ?? "";
            ScrollIndex = scrollIndex;
            VisibleCount = visibleCount;
        }

        public string SearchText { get; }

        public int ScrollIndex { get; }

        public int VisibleCount { get; }

        public bool HasSearch => SearchText.Length > 0;
    }
}
