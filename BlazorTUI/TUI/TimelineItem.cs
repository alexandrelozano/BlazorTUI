namespace BlazorTUI.TUI
{
    public sealed class TimelineItem
    {
        private string name;
        private string text;
        private string marker;

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public string Text
        {
            get => text;
            set => text = value ?? "";
        }

        public string Marker
        {
            get => marker;
            set => marker = string.IsNullOrEmpty(value) ? "●" : value;
        }

        public TimelineItem(string name, string text, string marker = "●")
        {
            this.name = ValidateName(name);
            this.text = text ?? "";
            this.marker = string.IsNullOrEmpty(marker) ? "●" : marker;
        }

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
