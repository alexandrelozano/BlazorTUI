namespace BlazorTUI.TUI
{
    public sealed class RadioGroupOption
    {
        private string text = "";
        private string value = "";

        public string Name { get; }

        public string Text
        {
            get => text;
            set => text = ValidateText(value);
        }

        public string Value
        {
            get => value;
            set => this.value = ValidateText(value);
        }

        public RadioGroupOption(string name, string text, string? value = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
            Text = text;
            Value = value ?? text;
        }

        private static string ValidateText(string? value)
        {
            string text = value ?? "";
            if (text.Contains('\r') || text.Contains('\n'))
                throw new ArgumentException("RadioGroup option text cannot contain line breaks.", nameof(value));

            return text;
        }
    }
}
