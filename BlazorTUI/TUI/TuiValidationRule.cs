namespace BlazorTUI.TUI
{
    public sealed class TuiValidationRule
    {
        private readonly Func<object?, bool> validate;
        private readonly Func<object?, TuiCultureOptions, string>? messageProvider;

        public TuiValidationRule(Func<object?, bool> validate, string message)
        {
            ArgumentNullException.ThrowIfNull(validate);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            this.validate = validate;
            Message = message;
        }

        public TuiValidationRule(Func<object?, bool> validate, Func<object?, TuiCultureOptions, string> messageProvider)
        {
            ArgumentNullException.ThrowIfNull(validate);
            ArgumentNullException.ThrowIfNull(messageProvider);

            this.validate = validate;
            this.messageProvider = messageProvider;
            Message = "";
        }

        public string Message { get; }

        public bool IsValid(object? value)
            => validate(value);

        public string GetMessage(object? value, TuiCultureOptions cultureOptions)
        {
            ArgumentNullException.ThrowIfNull(cultureOptions);
            return messageProvider is null
                ? cultureOptions.FormatValidationMessage(Message)
                : messageProvider(value, cultureOptions) ?? "";
        }
    }
}
