namespace BlazorTUI.TUI
{
    public sealed class BreadcrumbSelectionChangedEventArgs : EventArgs
    {
        public BreadcrumbSelectionChangedEventArgs(
            int previousSelectedIndex,
            int selectedIndex,
            BreadcrumbItem? previousSelectedItem,
            BreadcrumbItem? selectedItem)
        {
            PreviousSelectedIndex = previousSelectedIndex;
            SelectedIndex = selectedIndex;
            PreviousSelectedItem = previousSelectedItem;
            SelectedItem = selectedItem;
        }

        public int PreviousSelectedIndex { get; }

        public int SelectedIndex { get; }

        public BreadcrumbItem? PreviousSelectedItem { get; }

        public BreadcrumbItem? SelectedItem { get; }
    }
}
