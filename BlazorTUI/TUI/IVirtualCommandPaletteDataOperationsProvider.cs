namespace BlazorTUI.TUI
{
    public interface IVirtualCommandPaletteDataOperationsProvider : IVirtualCommandPaletteDataProvider
    {
        VirtualCommandPaletteQuery CurrentQuery { get; }

        void ApplyQuery(VirtualCommandPaletteQuery query);

        Task ApplyQueryAsync(VirtualCommandPaletteQuery query, CancellationToken cancellationToken = default);
    }
}
