namespace BlazorTUI.TUI
{
    public sealed class TuiValidationChangedEventArgs : EventArgs
    {
        public TuiValidationChangedEventArgs(bool isValid, string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            IsValid = isValid;
            Message = message;
        }

        public bool IsValid { get; }

        public string Message { get; }
    }
}
