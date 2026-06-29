using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Timeline : Control
    {
        private readonly List<TimelineItem> items = new();

        public IReadOnlyList<TimelineItem> Items => items;

        public Timeline(
            string name,
            IEnumerable<TimelineItem> items,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)3);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
            SetItems(items);
        }

        public void SetItems(IEnumerable<TimelineItem> nextItems)
        {
            ArgumentNullException.ThrowIfNull(nextItems);
            items.Clear();
            foreach (TimelineItem item in nextItems)
                AddItem(item);
        }

        public void AddItem(TimelineItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (GetItem(item.Name) is not null)
                throw new InvalidOperationException($"A timeline item named '{item.Name}' already exists.");

            items.Add(item);
        }

        public TimelineItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            TimelineItem? item = GetItem(name);
            return item is not null && items.Remove(item);
        }

        public void ClearItems() => items.Clear();

        internal void ExportTimelineState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetStringList(
                "Items",
                items.Select(item => string.Join("\t", item.Name, item.Text, item.Marker)));
        }

        internal void RestoreTimelineState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (!state.TryGetStringList("Items", out IReadOnlyList<string> storedItems))
                return;

            var restored = new List<TimelineItem>();
            foreach (string storedItem in storedItems)
            {
                string[] parts = storedItem.Split('\t');
                if (parts.Length >= 2)
                    restored.Add(new TimelineItem(parts[0], parts[1], parts.Length >= 3 ? parts[2] : "●"));
            }

            SetItems(restored);
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            ClearSurface(rows);
            int rowsToRender = Math.Min(Height, items.Count);
            for (int row = 0; row < rowsToRender; row++)
            {
                TimelineItem item = items[row];
                WriteText(rows, 0, row, TuiText.CellAt(item.Marker, 0));
                if (row < items.Count - 1 && row + 1 < Height)
                    WriteText(rows, 0, row + 1, "│");
                WriteText(rows, 2, row, item.Text);
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

        private void WriteText(IList<Row> rows, int localX, int localY, string text)
        {
            for (int x = 0; x < TuiText.VisualWidth(text) && localX + x < Width; x++)
                WriteCell(rows, localX + x, localY, TuiText.CellAt(text, x));
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
