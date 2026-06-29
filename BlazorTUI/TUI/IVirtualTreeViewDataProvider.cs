namespace BlazorTUI.TUI
{
    public interface IVirtualTreeViewDataProvider
    {
        int VisibleCount { get; }

        VirtualTreeViewNode GetVisibleNode(int visibleIndex);

        int FindVisibleIndexByKey(string key);

        void SetExpanded(string key, bool expanded);
    }
}
