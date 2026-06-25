namespace BlazorTUI.TUI
{
    public sealed class CommandPaletteItem
    {
        private string title = "";
        private string description = "";

        public string Name { get; }

        public string Title
        {
            get => title;
            set => title = ValidateText(value);
        }

        public string Description
        {
            get => description;
            set => description = ValidateText(value);
        }

        public Action<CommandPaletteItem>? Action { get; set; }

        public event EventHandler? Executed;

        public CommandPaletteItem(
            string name,
            string title,
            string description = "",
            Action<CommandPaletteItem>? action = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name;
            Title = title;
            Description = description;
            Action = action;
        }

        public void Execute()
        {
            Action?.Invoke(this);
            Executed?.Invoke(this, EventArgs.Empty);
        }

        private static string ValidateText(string? value)
        {
            string text = value ?? "";
            if (text.Contains('\r') || text.Contains('\n'))
                throw new ArgumentException("Command palette text cannot contain line breaks.", nameof(value));

            return text;
        }
    }
}
