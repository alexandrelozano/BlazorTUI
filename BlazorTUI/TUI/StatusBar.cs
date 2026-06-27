using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class StatusBar : Control
    {
        private readonly List<StatusBarItem> items = new();
        private string message = "";
        private string separator = " ";

        public string Text
        {
            get => message;
            set => SetMessage(value);
        }

        public string Message
        {
            get => message;
            set => SetMessage(value);
        }

        public string Value
        {
            get => message;
            set => SetMessage(value);
        }

        public string Separator
        {
            get => separator;
            set => separator = value ?? "";
        }

        public IReadOnlyList<StatusBarItem> Items => items;

        public event EventHandler<StatusBarMessageChangedEventArgs>? MessageChanged;

        public StatusBar(
            string name,
            string message,
            short X,
            short Y,
            short width,
            Color forecolor,
            Color backgroundcolor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            height = 1;
            this.message = message ?? "";
            foreColor = forecolor;
            backgroundColor = backgroundcolor;
            TabStop = false;
        }

        public void SetMessage(string? value)
        {
            string newMessage = value ?? "";
            if (newMessage == message)
                return;

            string previousMessage = message;
            message = newMessage;
            MessageChanged?.Invoke(this, new StatusBarMessageChangedEventArgs(previousMessage, message));
        }

        public StatusBarItem AddItem(
            string name,
            string text,
            short width = 0,
            StatusBarItemAlignment alignment = StatusBarItemAlignment.Right,
            Color? foreColor = null,
            Color? backgroundColor = null)
        {
            var item = new StatusBarItem(name, text, width, alignment, foreColor, backgroundColor);
            AddItem(item);
            return item;
        }

        public void AddItem(StatusBarItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (GetItem(item.Name) is not null)
                throw new InvalidOperationException($"A status bar item named '{item.Name}' already exists.");

            items.Add(item);
        }

        public StatusBarItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            StatusBarItem? item = GetItem(name);
            return item is not null && items.Remove(item);
        }

        public bool RemoveItem(StatusBarItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return items.Remove(item);
        }

        public void ClearItems() => items.Clear();

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);

            if (!Visible || container is null)
                return;

            int absoluteY = container.YOffset() + Y;
            if (absoluteY < container.YOffset() ||
                absoluteY >= container.YOffset() + container.height ||
                absoluteY < 0 ||
                absoluteY >= rows.Count)
            {
                return;
            }

            for (int localX = 0; localX < width; localX++)
            {
                WriteCell(rows, absoluteY, localX, " ", foreColor, backgroundColor);
            }

            int rightLimit = RenderRightItems(rows, absoluteY);
            RenderLeftContent(rows, absoluteY, rightLimit);
        }

        private int RenderRightItems(IList<Row> rows, int absoluteY)
        {
            int cursor = width;
            List<StatusBarItem> rightItems = items
                .Where(item => item.Alignment == StatusBarItemAlignment.Right)
                .ToList();

            for (int index = rightItems.Count - 1; index >= 0 && cursor > 0; index--)
            {
                StatusBarItem item = rightItems[index];
                string text = FormatItemText(item);
                int start = cursor - TuiText.VisualWidth(text);
                RenderText(rows, absoluteY, start, width, text, ItemForeColor(item), ItemBackgroundColor(item));
                cursor = start;

                if (index > 0 && TuiText.VisualWidth(separator) > 0)
                {
                    start = cursor - TuiText.VisualWidth(separator);
                    RenderText(rows, absoluteY, start, width, separator, foreColor, backgroundColor);
                    cursor = start;
                }
            }

            return Math.Max(0, cursor);
        }

        private void RenderLeftContent(IList<Row> rows, int absoluteY, int rightLimit)
        {
            int cursor = RenderText(rows, absoluteY, 0, rightLimit, message, foreColor, backgroundColor);

            foreach (StatusBarItem item in items.Where(item => item.Alignment == StatusBarItemAlignment.Left))
            {
                if (cursor > 0 && TuiText.VisualWidth(separator) > 0)
                    cursor = RenderText(rows, absoluteY, cursor, rightLimit, separator, foreColor, backgroundColor);

                cursor = RenderText(
                    rows,
                    absoluteY,
                    cursor,
                    rightLimit,
                    FormatItemText(item),
                    ItemForeColor(item),
                    ItemBackgroundColor(item));
            }
        }

        private int RenderText(
            IList<Row> rows,
            int absoluteY,
            int localStartX,
            int localEndX,
            string text,
            Color textForeColor,
            Color textBackgroundColor)
        {
            int textWidth = TuiText.VisualWidth(text);
            for (int index = 0; index < textWidth; index++)
            {
                int localX = localStartX + index;
                if (localX >= 0 && localX < localEndX)
                {
                    WriteCell(
                        rows,
                        absoluteY,
                        localX,
                        TuiText.CellAt(text, index),
                        textForeColor,
                        textBackgroundColor);
                }
            }

            return localStartX + textWidth;
        }

        private void WriteCell(
            IList<Row> rows,
            int absoluteY,
            int localX,
            string character,
            Color textForeColor,
            Color textBackgroundColor)
        {
            int absoluteX = container.XOffset() + X + localX;
            if (absoluteX < container.XOffset() ||
                absoluteX >= container.XOffset() + container.width ||
                absoluteX < 0 ||
                absoluteY < 0 ||
                absoluteY >= rows.Count ||
                absoluteX >= rows[absoluteY].Cells.Count)
            {
                return;
            }

            rows[absoluteY].Cells[absoluteX].foreColor = textForeColor;
            rows[absoluteY].Cells[absoluteX].backgroundColor = textBackgroundColor;
            rows[absoluteY].Cells[absoluteX].character = character;
        }

        private string FormatItemText(StatusBarItem item)
        {
            if (item.Width == 0)
                return item.Text;

            return TuiText.PadRightToVisualWidth(item.Text, item.Width);
        }

        private Color ItemForeColor(StatusBarItem item) => item.ForeColor ?? foreColor;

        private Color ItemBackgroundColor(StatusBarItem item) => item.BackgroundColor ?? backgroundColor;
    }
}
