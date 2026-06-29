namespace BlazorTUI.TUI
{
    public interface IVirtualCommandPaletteDataProvider
    {
        int Count { get; }

        int GetFilteredCount(string searchText);

        CommandPaletteItem GetFilteredCommand(string searchText, int filteredIndex);

        CommandPaletteItem? GetCommand(string name);
    }
}
