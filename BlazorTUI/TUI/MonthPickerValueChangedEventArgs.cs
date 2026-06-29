namespace BlazorTUI.TUI
{
    public sealed class MonthPickerValueChangedEventArgs : EventArgs
    {
        public DateOnly? PreviousValue { get; }

        public DateOnly? Value { get; }

        public MonthPickerValueChangedEventArgs(DateOnly? previousValue, DateOnly? value)
        {
            PreviousValue = previousValue;
            Value = value;
        }
    }
}
