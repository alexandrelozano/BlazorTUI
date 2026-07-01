using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class StatusBarItem
    {
        private string text = "";
        private short width;
        private StatusBarItemAlignment alignment;
        private bool enabled = true;
        private bool visible = true;

        public string Name { get; }

        public TuiCommand? Command { get; }

        public string Text
        {
            get => Command is null ? text : FormatCommandText();
            set
            {
                if (Command is not null)
                    Command.Label = value;
                else
                    text = value ?? "";
            }
        }

        public short Width
        {
            get => width;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, (short)0);
                width = value;
            }
        }

        public StatusBarItemAlignment Alignment
        {
            get => alignment;
            set
            {
                if (!Enum.IsDefined(value))
                    throw new ArgumentOutOfRangeException(nameof(value));

                alignment = value;
            }
        }

        public Color? ForeColor { get; set; }

        public Color? BackgroundColor { get; set; }

        public bool IncludeShortcut { get; set; }

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

        public StatusBarItem(
            string name,
            string text,
            short width = 0,
            StatusBarItemAlignment alignment = StatusBarItemAlignment.Right,
            Color? foreColor = null,
            Color? backgroundColor = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)0);

            Name = name;
            Text = text;
            Width = width;
            Alignment = alignment;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
        }

        public StatusBarItem(
            TuiCommand command,
            short width = 0,
            StatusBarItemAlignment alignment = StatusBarItemAlignment.Right,
            Color? foreColor = null,
            Color? backgroundColor = null,
            bool includeShortcut = true)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)0);

            Name = command.Id;
            Command = command;
            text = command.Label;
            Width = width;
            Alignment = alignment;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            IncludeShortcut = includeShortcut;
        }

        private string FormatCommandText()
        {
            if (Command is null)
                return text;

            if (!IncludeShortcut || Command.Shortcuts.Count == 0)
                return Command.Label;

            return $"{Command.ShortcutText} {Command.Label}";
        }
    }
}
