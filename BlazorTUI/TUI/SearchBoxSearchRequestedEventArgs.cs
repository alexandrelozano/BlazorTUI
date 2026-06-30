namespace BlazorTUI.TUI
{
    public sealed class SearchBoxSearchRequestedEventArgs : EventArgs
    {
        public SearchBoxSearchRequestedEventArgs(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        public string Value { get; }
    }
}
