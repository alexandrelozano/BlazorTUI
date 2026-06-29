using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Toast : Control
    {
        private readonly List<ToastItem> items = new();

        public IReadOnlyList<ToastItem> Items => items;

        public Toast(
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

        public ToastItem AddToast(string name, string text, TimeSpan? duration = null)
        {
            var item = new ToastItem(name, text, duration);
            AddToast(item);
            return item;
        }

        public void AddToast(ToastItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (GetToast(item.Name) is not null)
                throw new InvalidOperationException($"A toast named '{item.Name}' already exists.");

            item.CreatedAt = DateTimeOffset.UtcNow;
            items.Add(item);
        }

        public ToastItem? GetToast(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            RemoveExpired();
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool Dismiss(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ToastItem? item = GetToast(name);
            return item is not null && items.Remove(item);
        }

        public void ClearToasts() => items.Clear();

        internal void ExportToastState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            RemoveExpired();
            state.SetStringList(
                "Items",
                items.Select(item => string.Join(
                    "\t",
                    item.Name,
                    item.Text,
                    item.Duration.HasValue ? item.Duration.Value.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) : "")));
        }

        internal void RestoreToastState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (!state.TryGetStringList("Items", out IReadOnlyList<string> storedItems))
                return;

            items.Clear();
            foreach (string storedItem in storedItems)
            {
                string[] parts = storedItem.Split('\t');
                if (parts.Length < 2)
                    continue;

                TimeSpan? duration = null;
                if (parts.Length >= 3 &&
                    double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double milliseconds))
                {
                    duration = TimeSpan.FromMilliseconds(milliseconds);
                }

                items.Add(new ToastItem(parts[0], parts[1], duration));
            }
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            RemoveExpired();
            ClearSurface(rows);
            List<ToastItem> visibleItems = items.TakeLast(Height).ToList();
            for (int row = 0; row < visibleItems.Count; row++)
            {
                string text = TuiText.PadRightToVisualWidth(visibleItems[row].Text, Width);
                for (int x = 0; x < Width; x++)
                    WriteCell(rows, x, row, TuiText.CellAt(text, x));
            }
        }

        private void RemoveExpired()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            items.RemoveAll(item => item.IsExpired(now));
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
