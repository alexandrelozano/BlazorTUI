namespace BlazorTUI.TUI
{
    public sealed class StatusBarMessageChangedEventArgs : EventArgs
    {
        public StatusBarMessageChangedEventArgs(string previousMessage, string message)
        {
            PreviousMessage = previousMessage;
            Message = message;
        }

        public string PreviousMessage { get; }

        public string Message { get; }
    }
}
