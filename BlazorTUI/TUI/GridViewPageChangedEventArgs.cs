namespace BlazorTUI.TUI
{
    public sealed class GridViewPageChangedEventArgs : EventArgs
    {
        public GridViewPageChangedEventArgs(
            int previousPageIndex,
            int pageIndex,
            int pageCount,
            int pageSize)
        {
            PreviousPageIndex = previousPageIndex;
            PageIndex = pageIndex;
            PageCount = pageCount;
            PageSize = pageSize;
        }

        public int PreviousPageIndex { get; }

        public int PageIndex { get; }

        public int PageCount { get; }

        public int PageSize { get; }
    }
}
