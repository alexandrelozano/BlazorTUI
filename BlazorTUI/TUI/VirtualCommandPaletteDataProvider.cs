namespace BlazorTUI.TUI
{
    public sealed class VirtualCommandPaletteDataProvider : IVirtualCommandPaletteDataProvider
    {
        private readonly Func<int> getCount;
        private readonly Func<string, int> getFilteredCount;
        private readonly Func<string, int, CommandPaletteItem> getFilteredCommand;
        private readonly Func<string, CommandPaletteItem?>? getCommand;

        public VirtualCommandPaletteDataProvider(
            Func<int> getCount,
            Func<string, int> getFilteredCount,
            Func<string, int, CommandPaletteItem> getFilteredCommand,
            Func<string, CommandPaletteItem?>? getCommand = null)
        {
            ArgumentNullException.ThrowIfNull(getCount);
            ArgumentNullException.ThrowIfNull(getFilteredCount);
            ArgumentNullException.ThrowIfNull(getFilteredCommand);

            this.getCount = getCount;
            this.getFilteredCount = getFilteredCount;
            this.getFilteredCommand = getFilteredCommand;
            this.getCommand = getCommand;
        }

        public int Count => Math.Max(0, getCount());

        public int GetFilteredCount(string searchText)
            => Math.Max(0, getFilteredCount(searchText ?? ""));

        public CommandPaletteItem GetFilteredCommand(string searchText, int filteredIndex)
        {
            if (filteredIndex < 0 || filteredIndex >= GetFilteredCount(searchText ?? ""))
                throw new ArgumentOutOfRangeException(nameof(filteredIndex));

            return getFilteredCommand(searchText ?? "", filteredIndex) ??
                throw new InvalidOperationException("The virtual command palette provider returned null.");
        }

        public CommandPaletteItem? GetCommand(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (getCommand is not null)
                return getCommand(name);

            int filteredCount = GetFilteredCount("");
            for (int index = 0; index < filteredCount; index++)
            {
                CommandPaletteItem command = GetFilteredCommand("", index);
                if (command.Name == name)
                    return command;
            }

            return null;
        }
    }
}
