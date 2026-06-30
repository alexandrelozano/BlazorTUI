namespace BlazorTUI.TUI
{
    public sealed class MultiSelectComboBoxSelectionChangedEventArgs : EventArgs
    {
        public MultiSelectComboBoxSelectionChangedEventArgs(string item, int index, bool isSelected)
        {
            ArgumentNullException.ThrowIfNull(item);
            Item = item;
            Index = index;
            IsSelected = isSelected;
        }

        public string Item { get; }

        public int Index { get; }

        public bool IsSelected { get; }
    }
}
