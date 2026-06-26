using System.Drawing;

namespace BlazorTUI.TUI
{
    public abstract class Control
    {
        private bool focus;

        public string name { get; set; } = "";

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public Container container { get; set; } = null!;

        public Container? ParentContainer => container;

        public short X { get; set; }

        public short Y { get; set; }

        public short width { get; set; }

        public short Width { get => width; set => width = value; }

        public short height { get; set; }

        public short Height { get => height; set => height = value; }

        public Color foreColor { get; set; }

        public Color ForeColor { get => foreColor; set => foreColor = value; }

        public Color backgroundColor { get; set; }

        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        public bool Focus
        {
            get => focus;
            set
            {
                if (value == focus)
                    return;

                focus = value;
                if (value)
                {
                    OnFocus?.Invoke();
                    GotFocus?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnLostFocus?.Invoke();
                    LostFocus?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsFocused { get => Focus; set => Focus = value; }

        public bool TabStop { get; set; }

        public short TabIndex { get; set; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        public TuiThemeRole ThemeRole { get; set; } = TuiThemeRole.Default;

        public TuiThemeState ThemeState { get; set; } = TuiThemeState.Normal;

        public Action<Control>? OnClick;

        public Action? OnFocus;

        public Action? OnLostFocus;

        public event EventHandler? Clicked;

        public event EventHandler? GotFocus;

        public event EventHandler? LostFocus;

        public abstract void Render(IList<Row> rows);

        public virtual bool KeyDown(string key, bool shiftKey) => false;

        public virtual bool Click(short X, short Y) => false;

        internal void NotifyClicked()
        {
            OnClick?.Invoke(this);
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
