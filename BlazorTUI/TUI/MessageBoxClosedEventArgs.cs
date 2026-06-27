namespace BlazorTUI.TUI
{
    public sealed class MessageBoxClosedEventArgs : EventArgs
    {
        public MessageBoxClosedEventArgs(MessageBox.Result result)
        {
            Result = result;
        }

        public MessageBox.Result Result { get; }
    }
}
