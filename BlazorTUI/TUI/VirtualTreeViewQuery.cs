namespace BlazorTUI.TUI
{
    public sealed class VirtualTreeViewQuery
    {
        public static VirtualTreeViewQuery Empty { get; } = new(0, 1);

        public VirtualTreeViewQuery(int scrollIndex, int visibleCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(scrollIndex);
            ArgumentOutOfRangeException.ThrowIfLessThan(visibleCount, 1);

            ScrollIndex = scrollIndex;
            VisibleCount = visibleCount;
        }

        public int ScrollIndex { get; }

        public int VisibleCount { get; }
    }
}
