namespace BlazorTUI.TUI
{
    public sealed class ContextMenuItem
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

        public ContextMenuItemType Type { get; set; }

        public bool Enabled { get; set; } = true;

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

        internal void Invoke()
        {
            if (!Enabled || Type == ContextMenuItemType.Separator)
                return;

            OnClick?.Invoke();
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
