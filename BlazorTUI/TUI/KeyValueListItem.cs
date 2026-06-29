namespace BlazorTUI.TUI
{
    public sealed class KeyValueListItem
    {
        private string name;
        private string key;
        private string value;

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public string Key
        {
            get => key;
            set => key = value ?? "";
        }

        public string Value
        {
            get => value;
            set => this.value = value ?? "";
        }

        public KeyValueListItem(string name, string key, string value)
        {
            this.name = ValidateName(name);
            this.key = key ?? "";
            this.value = value ?? "";
        }

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
