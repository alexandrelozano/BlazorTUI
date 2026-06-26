namespace BlazorTUI.TUI
{
    public sealed class BreadcrumbItemActivatedEventArgs : EventArgs
    {
        public BreadcrumbItemActivatedEventArgs(int index, BreadcrumbItem item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }

        public BreadcrumbItem Item { get; }

        public string ItemName => Item.Name;
    }
}
