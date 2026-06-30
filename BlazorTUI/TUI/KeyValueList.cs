using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class KeyValueList : Control
    {
        private readonly List<KeyValueListItem> items = new();

        public IReadOnlyList<KeyValueListItem> Items => items;

        public short KeyWidth { get; set; }

        public string Separator { get; set; } = ": ";

        public KeyValueList(
            string name,
            IEnumerable<KeyValueListItem> items,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor,
            short keyWidth = 10)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(keyWidth, (short)0);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            KeyWidth = keyWidth;
            TabStop = false;
            SetItems(items);
        }

        public void SetItems(IEnumerable<KeyValueListItem> nextItems)
        {
            ArgumentNullException.ThrowIfNull(nextItems);
            items.Clear();
            foreach (KeyValueListItem item in nextItems)
                AddItem(item);
        }

        public void AddItem(KeyValueListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (GetItem(item.Name) is not null)
                throw new InvalidOperationException($"A key/value item named '{item.Name}' already exists.");

            items.Add(item);
        }

        public KeyValueListItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool SetValue(string name, string value)
        {
            KeyValueListItem? item = GetItem(name);
            if (item is null)
                return false;

            item.Value = value;
            return true;
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            KeyValueListItem? item = GetItem(name);
            return item is not null && items.Remove(item);
        }

        public void ClearItems() => items.Clear();

        public override string GetAccessibilitySummary()
            => FormatAccessibilitySummary($"KeyValueList {Name}: {items.Count} items.");

        internal void ExportKeyValueListState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetStringList(
                "Items",
                items.Select(item => string.Join("\t", item.Name, item.Key, item.Value)));
            state.SetInteger("KeyWidth", KeyWidth);
            state.SetString("Separator", Separator);
        }

        internal void RestoreKeyValueListState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetStringList("Items", out IReadOnlyList<string> storedItems))
            {
                var restored = new List<KeyValueListItem>();
                foreach (string storedItem in storedItems)
                {
                    string[] parts = storedItem.Split('\t');
                    if (parts.Length >= 3)
                        restored.Add(new KeyValueListItem(parts[0], parts[1], parts[2]));
                }

                SetItems(restored);
            }

            if (state.TryGetInteger("KeyWidth", out int storedKeyWidth) &&
                storedKeyWidth >= 0 &&
                storedKeyWidth <= short.MaxValue)
            {
                KeyWidth = (short)storedKeyWidth;
            }

            if (state.TryGetString("Separator", out string storedSeparator))
                Separator = storedSeparator;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            ClearSurface(rows);
            int rowsToRender = Math.Min(Height, items.Count);
            int keyWidth = Math.Clamp(KeyWidth, (short)0, Width);
            int separatorWidth = TuiText.VisualWidth(Separator);
            int valueStart = Math.Min(Width, keyWidth + separatorWidth);

            for (int row = 0; row < rowsToRender; row++)
            {
                KeyValueListItem item = items[row];
                string key = TuiText.PadRightToVisualWidth(item.Key, keyWidth);
                WriteText(rows, 0, row, key);
                WriteText(rows, keyWidth, row, Separator);
                WriteText(rows, valueStart, row, item.Value);
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
