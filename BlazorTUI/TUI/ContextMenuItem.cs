namespace BlazorTUI.TUI
{
    public sealed class ContextMenuItem
    {
        private string name;
        private string text;
        private bool enabled = true;
        private bool visible = true;

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public TuiCommand? Command { get; }

        public string Text
        {
            get => Command?.Label ?? text;
            set
            {
                if (Command is not null)
                    Command.Label = value;
                else
                    text = value ?? "";
            }
        }

        public ContextMenuItemType Type { get; set; }

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

        public Action? OnClick { get; set; }

        public event EventHandler? Clicked;

        public ContextMenuItem(
            string name,
            string text,
            ContextMenuItemType type = ContextMenuItemType.Item)
        {
            this.name = ValidateName(name);
            this.text = text ?? "";
            Type = type;
        }

        public ContextMenuItem(
            TuiCommand command,
            ContextMenuItemType type = ContextMenuItemType.Item)
        {
            ArgumentNullException.ThrowIfNull(command);

            this.name = command.Id;
            Command = command;
            this.text = command.Label;
            Type = type;
        }

        internal bool Invoke()
        {
            if (!Enabled || !Visible || Type == ContextMenuItemType.Separator)
                return false;

            if (Command is not null && !Command.Execute())
                return false;

            if (Command is null)
                OnClick?.Invoke();

            Clicked?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
