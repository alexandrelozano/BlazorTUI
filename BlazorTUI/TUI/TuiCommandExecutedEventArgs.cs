namespace BlazorTUI.TUI
{
    public sealed class TuiCommandExecutedEventArgs : EventArgs
    {
        public TuiCommandExecutedEventArgs(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            Command = command;
        }

        public TuiCommand Command { get; }

        public string CommandId => Command.Id;
    }
}
