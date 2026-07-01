namespace BlazorTUI.TUI
{
    public interface IVirtualTreeViewDataOperationsProvider : IVirtualTreeViewDataProvider
    {
        VirtualTreeViewQuery CurrentQuery { get; }

        void ApplyQuery(VirtualTreeViewQuery query);

        Task ApplyQueryAsync(VirtualTreeViewQuery query, CancellationToken cancellationToken = default);
    }
}
