using System.Drawing;

namespace BlazorTUI.TUI
{
    public class ModalPanel : Dialog
    {
        public bool CloseOnEscape { get; set; } = true;

        public event EventHandler<ModalPanelClosedEventArgs>? Closed;

        public ModalPanel(
            string name,
            string title,
            short width,
            short height,
            BorderStyle borderStyle,
            Color foreColor,
            Color backgroundColor,
            Screen screen)
            : base(name, title, width, height, borderStyle, foreColor, backgroundColor, screen)
        {
        }

        public new void Show()
        {
            if (Visible)
                return;

            base.Show();
        }

        public override void KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (CloseOnEscape && key == "Escape")
            {
                Close("Escape");
                return;
            }

            base.KeyDown(key, shiftKey);
        }

        public new void Close()
            => Close("");

        public void Close(string reason)
        {
            bool wasVisible = Visible;
            base.Close();
            if (wasVisible)
                Closed?.Invoke(this, new ModalPanelClosedEventArgs(reason));
        }
    }
}
