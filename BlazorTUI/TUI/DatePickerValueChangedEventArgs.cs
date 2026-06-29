namespace BlazorTUI.TUI
{
    public sealed class DatePickerValueChangedEventArgs : EventArgs
    {
        public DateOnly? PreviousValue { get; }

        public DateOnly? Value { get; }

        public DatePickerValueChangedEventArgs(DateOnly? previousValue, DateOnly? value)
        {
            PreviousValue = previousValue;
            Value = value;
        }
    }
}
