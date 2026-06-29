namespace BlazorTUI.TUI
{
    public sealed class ToastItem
    {
        private string name;
        private string text;

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

        public TimeSpan? Duration { get; set; }

        public DateTimeOffset CreatedAt { get; internal set; } = DateTimeOffset.UtcNow;

        public ToastItem(string name, string text, TimeSpan? duration = null)
        {
            this.name = ValidateName(name);
            this.text = text ?? "";
            Duration = duration;
        }

        internal bool IsExpired(DateTimeOffset now)
            => Duration.HasValue && now - CreatedAt >= Duration.Value;

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
