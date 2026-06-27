namespace BlazorTUI.TUI
{
    public sealed class TuiValidationRule
    {
        private readonly Func<object?, bool> validate;

        public TuiValidationRule(Func<object?, bool> validate, string message)
        {
            ArgumentNullException.ThrowIfNull(validate);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            this.validate = validate;
            Message = message;
        }

        public string Message { get; }

        public bool IsValid(object? value)
            => validate(value);
    }
}
