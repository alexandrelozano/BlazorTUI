namespace BlazorTUI.TUI
{
    public sealed class CalendarDateSelectedEventArgs : EventArgs
    {
        public DateOnly? PreviousValue { get; }

        public DateOnly? Value { get; }

        public CalendarDateSelectedEventArgs(DateOnly? previousValue, DateOnly? value)
        {
            PreviousValue = previousValue;
            Value = value;
        }
    }
}
