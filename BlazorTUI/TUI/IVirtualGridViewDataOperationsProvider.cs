namespace BlazorTUI.TUI
{
    public interface IVirtualGridViewDataOperationsProvider : IVirtualGridViewDataProvider
    {
        VirtualGridViewQuery CurrentQuery { get; }

        int ViewCount { get; }

        void ApplyQuery(VirtualGridViewQuery query);

        Task ApplyQueryAsync(VirtualGridViewQuery query, CancellationToken cancellationToken = default);

        GridView.GridRow GetViewRow(int viewIndex);

        string GetViewRowKey(int viewIndex);

        int GetSourceIndex(int viewIndex);

        int FindViewIndexBySourceIndex(int sourceIndex);

        int FindViewIndexByKey(string key);
    }
}
