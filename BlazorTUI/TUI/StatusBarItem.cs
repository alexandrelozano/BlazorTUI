using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class StatusBarItem
    {
        private string text = "";
        private short width;
        private StatusBarItemAlignment alignment;

        public string Name { get; }

        public string Text
        {
            get => text;
            set => text = value ?? "";
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
    }
}
