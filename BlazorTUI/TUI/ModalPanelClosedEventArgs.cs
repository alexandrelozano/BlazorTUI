namespace BlazorTUI.TUI
{
    public sealed class ModalPanelClosedEventArgs : EventArgs
    {
        public ModalPanelClosedEventArgs(string reason)
        {
            Reason = reason ?? "";
        }

        public string Reason { get; }
    }
}
