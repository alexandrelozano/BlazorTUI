namespace BlazorTUI.TUI
{
    public sealed class TuiCommand
    {
        private readonly List<TuiKeyGesture> shortcuts = new();
        private string label = "";
        private string description = "";

        public TuiCommand(
            string id,
            string label,
            string description = "",
            Action<TuiCommand>? handler = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            Id = id;
            Label = label;
            Description = description;
            Handler = handler;
        }

        public string Id { get; }

        public string Label
        {
            get => label;
            set => label = ValidateText(value);
        }

        public string Description
        {
            get => description;
            set => description = ValidateText(value);
        }

        public bool Enabled { get; set; } = true;

        public bool Visible { get; set; } = true;

        public Action<TuiCommand>? Handler { get; set; }

        public IReadOnlyList<TuiKeyGesture> Shortcuts => shortcuts.AsReadOnly();

        public string ShortcutText => string.Join(", ", shortcuts.Select(shortcut => shortcut.ToString()));

        public event EventHandler<TuiCommandExecutedEventArgs>? Executed;

        public void SetShortcuts(params string[] gestures)
        {
            ArgumentNullException.ThrowIfNull(gestures);
            SetShortcuts(gestures.Select(TuiKeyGesture.Parse));
        }

        public void SetShortcuts(IEnumerable<TuiKeyGesture> gestures)
        {
            ArgumentNullException.ThrowIfNull(gestures);
            shortcuts.Clear();
            foreach (TuiKeyGesture gesture in NormalizeGestures(gestures))
                shortcuts.Add(gesture);
        }

        public void AddShortcut(string gesture)
            => AddShortcut(TuiKeyGesture.Parse(gesture));

        public void AddShortcut(TuiKeyGesture gesture)
        {
            ArgumentNullException.ThrowIfNull(gesture);
            if (!shortcuts.Contains(gesture))
                shortcuts.Add(gesture);
        }

        public bool RemoveShortcut(string gesture)
            => RemoveShortcut(TuiKeyGesture.Parse(gesture));

        public bool RemoveShortcut(TuiKeyGesture gesture)
        {
            ArgumentNullException.ThrowIfNull(gesture);
            return shortcuts.Remove(gesture);
        }

        public void ClearShortcuts()
            => shortcuts.Clear();

        public bool Execute()
        {
            if (!Enabled || !Visible)
                return false;

            Handler?.Invoke(this);
            Executed?.Invoke(this, new TuiCommandExecutedEventArgs(this));
            return true;
        }

        private static IReadOnlyList<TuiKeyGesture> NormalizeGestures(IEnumerable<TuiKeyGesture> gestures)
        {
            var result = new List<TuiKeyGesture>();
            foreach (TuiKeyGesture? gesture in gestures)
            {
                ArgumentNullException.ThrowIfNull(gesture);
                if (!result.Contains(gesture))
                    result.Add(gesture);
            }

            return result;
        }

        private static string ValidateText(string? value)
        {
            string text = value ?? "";
            if (text.Contains('\r') || text.Contains('\n'))
                throw new ArgumentException("Command text cannot contain line breaks.", nameof(value));

            return text;
        }
    }
}
