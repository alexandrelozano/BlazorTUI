namespace BlazorTUI.TUI
{
    public sealed class CommandPaletteItem
    {
        private string title = "";
        private string description = "";
        private bool enabled = true;
        private bool visible = true;

        public string Name { get; }

        public TuiCommand? Command { get; }

        public string Title
        {
            get => Command?.Label ?? title;
            set
            {
                if (Command is not null)
                    Command.Label = ValidateText(value);
                else
                    title = ValidateText(value);
            }
        }

        public string Description
        {
            get => Command?.Description ?? description;
            set
            {
                if (Command is not null)
                    Command.Description = ValidateText(value);
                else
                    description = ValidateText(value);
            }
        }

        public bool Enabled
        {
            get => Command?.Enabled ?? enabled;
            set
            {
                if (Command is not null)
                    Command.Enabled = value;
                else
                    enabled = value;
            }
        }

        public bool Visible
        {
            get => Command?.Visible ?? visible;
            set
            {
                if (Command is not null)
                    Command.Visible = value;
                else
                    visible = value;
            }
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

        public CommandPaletteItem(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            Name = command.Id;
            Command = command;
            title = command.Label;
            description = command.Description;
        }

        public void Execute()
        {
            if (!Enabled || !Visible)
                return;

            if (Command is not null && !Command.Execute())
                return;

            if (Command is null)
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
