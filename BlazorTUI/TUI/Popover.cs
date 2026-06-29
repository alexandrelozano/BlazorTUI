using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Popover : Control, IPopupControl
    {
        private string title;
        private string text;

        public string Title
        {
            get => title;
            set => title = value ?? "";
        }

        public string Text
        {
            get => text;
            set => text = value ?? "";
        }

        public bool IsOpen { get; private set; }

        bool IPopupControl.IsPopupOpen => IsOpen;

        public Popover(
            string name,
            string title,
            string text,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)3);

            Name = name;
            this.title = title ?? "";
            this.text = text ?? "";
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
        }

        public void Show()
        {
            if (!Visible)
                return;

            IsOpen = true;
            container?.BringToFront(this);
        }

        public void Hide() => IsOpen = false;

        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (key == "Escape" && IsOpen)
            {
                Hide();
                return true;
            }

            return false;
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || !IsOpen || X < 0 || X >= Width || Y < 0 || Y >= Height)
                return false;

            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || !IsOpen || container is null)
                return;

            DrawBox(rows);
            DrawText(rows);
        }

        internal void ExportPopoverState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetString("Title", title);
            state.SetString("Text", text);
            state.SetBoolean("IsOpen", IsOpen);
        }

        internal void RestorePopoverState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetString("Title", out string restoredTitle))
                title = restoredTitle;
            if (state.TryGetString("Text", out string restoredText))
                text = restoredText;

            IsOpen = state.TryGetBoolean("IsOpen", out bool isOpen) && isOpen;
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && IsOpen && X >= this.X && X < this.X + Width &&
                Y >= this.Y && Y < this.Y + Height;

        void IPopupControl.ClosePopup()
            => Hide();

        private void DrawBox(IList<Row> rows)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    string character = (x, y) switch
                    {
                        (0, 0) => "┌",
                        var p when p.x == Width - 1 && p.y == 0 => "┐",
                        var p when p.x == 0 && p.y == Height - 1 => "└",
                        var p when p.x == Width - 1 && p.y == Height - 1 => "┘",
                        var p when p.y == 0 || p.y == Height - 1 => "─",
                        var p when p.x == 0 || p.x == Width - 1 => "│",
                        _ => " "
                    };
                    WriteCell(rows, x, y, character);
                }
            }

            if (title.Length > 0)
            {
                string renderedTitle = TuiText.TruncateByVisualWidth(title, Width - 2);
                for (int x = 0; x < TuiText.VisualWidth(renderedTitle); x++)
                    WriteCell(rows, 1 + x, 0, TuiText.CellAt(renderedTitle, x));
            }
        }

        private void DrawText(IList<Row> rows)
        {
            string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            int maxLines = Math.Max(0, Height - 2);
            int contentWidth = Math.Max(0, Width - 2);
            for (int index = 0; index < maxLines && index < lines.Length; index++)
            {
                string line = TuiText.TruncateByVisualWidth(lines[index], contentWidth);
                for (int x = 0; x < TuiText.VisualWidth(line); x++)
                    WriteCell(rows, 1 + x, 1 + index, TuiText.CellAt(line, x));
            }
        }

        private void WriteCell(IList<Row> rows, int localX, int localY, string character)
        {
            int absoluteX = container.XOffset() + X + localX;
            int absoluteY = container.YOffset() + Y + localY;
            if (absoluteX < container.XOffset() ||
                absoluteX >= container.XOffset() + container.Width ||
                absoluteY < container.YOffset() ||
                absoluteY >= container.YOffset() + container.Height ||
                absoluteY < 0 ||
                absoluteY >= rows.Count ||
                absoluteX < 0 ||
                absoluteX >= rows[absoluteY].Cells.Count)
            {
                return;
            }

            Cell cell = rows[absoluteY].Cells[absoluteX];
            cell.ForeColor = ForeColor;
            cell.BackgroundColor = BackgroundColor;
            cell.Character = character;
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }
    }
}
