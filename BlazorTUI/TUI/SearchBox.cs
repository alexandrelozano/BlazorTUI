using System.Drawing;

namespace BlazorTUI.TUI
{
    public class SearchBox : TextBox
    {
        public SearchBox(
            string name,
            string text,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
            : base(name, text, X, Y, width, foreColor, backgroundColor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)5);
        }

        public event EventHandler<SearchBoxSearchRequestedEventArgs>? SearchRequested;

        public event EventHandler? Cleared;

        public string ClearGlyph { get; set; } = "×";

        public string SearchGlyph { get; set; } = "⌕";

        public void Clear()
        {
            if (Value.Length == 0)
                return;

            Value = "";
            Cleared?.Invoke(this, EventArgs.Empty);
        }

        public void Search()
            => SearchRequested?.Invoke(this, new SearchBoxSearchRequestedEventArgs(Value));

        public override bool Click(short X, short Y)
        {
            if (!Visible || Y != 0 || X < 0 || X >= Width)
                return false;

            if (X == Width - 2)
            {
                container.TopContainer().SetFocus(Name);
                Clear();
                NotifyClicked();
                return true;
            }

            if (X == Width - 1)
            {
                container.TopContainer().SetFocus(Name);
                Search();
                NotifyClicked();
                return true;
            }

            return WithInputWidth(() => base.Click(X, Y));
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            switch (key)
            {
                case "Enter":
                    Search();
                    NotifyClicked();
                    return true;
                case "Escape":
                    Clear();
                    return true;
                default:
                    return WithInputWidth(() => base.KeyDown(key, shiftKey));
            }
        }

        public override void Render(IList<Row> rows)
        {
            WithRenderInputWidth(() => base.Render(rows));
            DrawActionCells(rows);
        }

        private short InputWidth => (short)Math.Max(1, Width - 2);

        private T WithInputWidth<T>(Func<T> action)
        {
            short previousWidth = Width;
            Width = (short)Math.Max(1, InputWidth + 1);
            try
            {
                return action();
            }
            finally
            {
                Width = previousWidth;
            }
        }

        private void WithRenderInputWidth(Action action)
        {
            short previousWidth = Width;
            Width = InputWidth;
            try
            {
                action();
            }
            finally
            {
                Width = previousWidth;
            }
        }

        private void DrawActionCells(IList<Row> rows)
        {
            if (!Visible || container is null)
                return;

            DrawActionCell(rows, Width - 2, ClearGlyph);
            DrawActionCell(rows, Width - 1, SearchGlyph);
        }

        private void DrawActionCell(IList<Row> rows, int localX, string glyph)
        {
            int absoluteX = container.XOffset() + X + localX;
            int absoluteY = container.YOffset() + Y;
            if (absoluteY < 0 || absoluteY >= rows.Count ||
                absoluteX < 0 || absoluteX >= rows[absoluteY].Cells.Count ||
                absoluteX < container.XOffset() || absoluteX >= container.XOffset() + container.Width ||
                absoluteY < container.YOffset() || absoluteY >= container.YOffset() + container.Height)
            {
                return;
            }

            Cell cell = rows[absoluteY].Cells[absoluteX];
            cell.ForeColor = Focus ? BackgroundColor : ForeColor;
            cell.BackgroundColor = Focus ? ForeColor : BackgroundColor;
            cell.Character = glyph;
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }
    }
}
