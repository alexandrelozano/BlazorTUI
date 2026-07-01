namespace BlazorTUI.TUI
{
    public interface IVirtualListBoxDataOperationsProvider : IVirtualListBoxDataProvider
    {
        VirtualListBoxQuery CurrentQuery { get; }

        int ViewCount { get; }

        void ApplyQuery(VirtualListBoxQuery query);

        Task ApplyQueryAsync(VirtualListBoxQuery query, CancellationToken cancellationToken = default);

        string GetViewItem(int viewIndex);

        string GetViewKey(int viewIndex);

        int FindViewIndexByKey(string key);
    }
}
