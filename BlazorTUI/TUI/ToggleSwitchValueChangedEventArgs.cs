namespace BlazorTUI.TUI
{
    public sealed class ToggleSwitchValueChangedEventArgs : EventArgs
    {
        public ToggleSwitchValueChangedEventArgs(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
    }
}
