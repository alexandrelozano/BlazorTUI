namespace BlazorTUI.TUI
{
    public sealed class DateRangePickerValueChangedEventArgs : EventArgs
    {
        public DateOnly? PreviousStartValue { get; }

        public DateOnly? PreviousEndValue { get; }

        public DateOnly? StartValue { get; }

        public DateOnly? EndValue { get; }

        public DateRangePickerValueChangedEventArgs(
            DateOnly? previousStartValue,
            DateOnly? previousEndValue,
            DateOnly? startValue,
            DateOnly? endValue)
        {
            PreviousStartValue = previousStartValue;
            PreviousEndValue = previousEndValue;
            StartValue = startValue;
            EndValue = endValue;
        }
    }
}
