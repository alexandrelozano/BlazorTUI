namespace BlazorTUI.TUI
{
    public sealed class SliderValueChangedEventArgs : EventArgs
    {
        public int PreviousValue { get; }

        public int Value { get; }

        public SliderValueChangedEventArgs(int previousValue, int value)
        {
            PreviousValue = previousValue;
            Value = value;
        }
    }
}
