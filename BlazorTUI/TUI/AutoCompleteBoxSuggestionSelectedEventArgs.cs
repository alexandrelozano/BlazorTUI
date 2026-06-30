namespace BlazorTUI.TUI
{
    public sealed class AutoCompleteBoxSuggestionSelectedEventArgs : EventArgs
    {
        public AutoCompleteBoxSuggestionSelectedEventArgs(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Item = item;
        }

        public string Item { get; }
    }
}
