namespace BlazorTUI.TUI
{
    public sealed class ContextMenuItemClickedEventArgs : EventArgs
    {
        public ContextMenuItem Item { get; }

        public ContextMenuItemClickedEventArgs(ContextMenuItem item)
        {
            Item = item;
        }
    }
}
