using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class ValidationSummary : Control
    {
        private readonly List<string> messages = new();

        public IReadOnlyList<string> Messages => messages;

        public string EmptyMessage { get; set; } = "";

        public bool ShowWhenValid { get; set; }

        public string Bullet { get; set; } = "! ";

        public ValidationSummary(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
        }

        public void SetMessages(IEnumerable<string> nextMessages)
        {
            ArgumentNullException.ThrowIfNull(nextMessages);
            messages.Clear();
            messages.AddRange(nextMessages.Where(message => !string.IsNullOrWhiteSpace(message)));
        }

        public void ClearMessages()
            => messages.Clear();

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            ClearSurface(rows);
            IReadOnlyList<string> visibleMessages = messages.Count > 0
                ? messages
                : ShowWhenValid && !string.IsNullOrWhiteSpace(EmptyMessage)
                    ? new[] { EmptyMessage }
                    : Array.Empty<string>();

            for (int row = 0; row < Height && row < visibleMessages.Count; row++)
            {
                string text = messages.Count > 0 ? Bullet + visibleMessages[row] : visibleMessages[row];
                string rendered = TuiText.TruncateByVisualWidth(text, Width);
                int textWidth = TuiText.VisualWidth(rendered);
                for (int x = 0; x < textWidth; x++)
                    WriteCell(rows, x, row, TuiText.CellAt(rendered, x));
            }
        }

        private void ClearSurface(IList<Row> rows)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    WriteCell(rows, x, y, " ");
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
