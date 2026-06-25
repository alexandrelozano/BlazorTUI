namespace BlazorTUI.TUI
{
    public sealed class CommandPaletteExecutedEventArgs : EventArgs
    {
        public CommandPaletteExecutedEventArgs(CommandPaletteItem command)
        {
            Command = command;
        }

        public CommandPaletteItem Command { get; }

        public string CommandName => Command.Name;
    }
}
