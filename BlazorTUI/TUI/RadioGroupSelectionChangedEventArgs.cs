namespace BlazorTUI.TUI
{
    public sealed class RadioGroupSelectionChangedEventArgs : EventArgs
    {
        public RadioGroupSelectionChangedEventArgs(
            int previousSelectedIndex,
            int selectedIndex,
            RadioGroupOption? previousSelectedOption,
            RadioGroupOption? selectedOption)
        {
            PreviousSelectedIndex = previousSelectedIndex;
            SelectedIndex = selectedIndex;
            PreviousSelectedOption = previousSelectedOption;
            SelectedOption = selectedOption;
        }

        public int PreviousSelectedIndex { get; }

        public int SelectedIndex { get; }

        public RadioGroupOption? PreviousSelectedOption { get; }

        public RadioGroupOption? SelectedOption { get; }
    }
}
