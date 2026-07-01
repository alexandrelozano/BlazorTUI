using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class BarChart : Control
    {
        private readonly List<BarChartItem> items = new();
        private double? maximum;

        public IReadOnlyList<BarChartItem> Items => items;

        public BarChartOrientation Orientation { get; set; }

        public short LabelWidth { get; set; } = 8;

        public double? Maximum
        {
            get => maximum;
            set
            {
                if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
                    throw new ArgumentOutOfRangeException(nameof(value));

                maximum = value;
            }
        }

        public BarChart(
            string name,
            IEnumerable<BarChartItem> items,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor,
            BarChartOrientation orientation = BarChartOrientation.Horizontal)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            Orientation = orientation;
            TabStop = false;
            SetItems(items);
        }

        public void SetItems(IEnumerable<BarChartItem> nextItems)
        {
            ArgumentNullException.ThrowIfNull(nextItems);
            items.Clear();
            foreach (BarChartItem item in nextItems)
                AddItem(item);
        }

        public void AddItem(BarChartItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (GetItem(item.Name) is not null)
                throw new InvalidOperationException($"A bar chart item named '{item.Name}' already exists.");

            items.Add(item);
        }

        public BarChartItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            BarChartItem? item = GetItem(name);
            return item is not null && items.Remove(item);
        }

        public void ClearItems() => items.Clear();

        public override string GetAccessibilitySummary()
        {
            string maximumSummary = Maximum.HasValue
                ? $", maximum {CultureOptions.FormatNumber(Maximum.Value, "G")}"
                : "";
            return FormatAccessibilitySummary($"BarChart {Name}: {items.Count} items, {Orientation} orientation{maximumSummary}.");
        }

        internal void ExportBarChartState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetStringList(
                "Items",
                items.Select(item => string.Join(
                    "\t",
                    item.Name,
                    item.Label,
                    item.Value.ToString("R", CultureInfo.InvariantCulture))));
            state.SetInteger("Orientation", (int)Orientation);
            state.SetInteger("LabelWidth", LabelWidth);
            if (maximum.HasValue)
                state.SetString("Maximum", maximum.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal void RestoreBarChartState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetStringList("Items", out IReadOnlyList<string> storedItems))
            {
                var restored = new List<BarChartItem>();
                foreach (string storedItem in storedItems)
                {
                    string[] parts = storedItem.Split('\t');
                    if (parts.Length == 3 &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    {
                        restored.Add(new BarChartItem(parts[0], parts[1], value));
                    }
                }

                SetItems(restored);
            }

            if (state.TryGetInteger("Orientation", out int storedOrientation) &&
                Enum.IsDefined((BarChartOrientation)storedOrientation))
            {
                Orientation = (BarChartOrientation)storedOrientation;
            }

            if (state.TryGetInteger("LabelWidth", out int storedLabelWidth) &&
                storedLabelWidth >= 0 &&
                storedLabelWidth <= short.MaxValue)
            {
                LabelWidth = (short)storedLabelWidth;
            }

            if (state.TryGetString("Maximum", out string storedMaximum) &&
                double.TryParse(storedMaximum, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMaximum))
            {
                Maximum = parsedMaximum;
            }
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            ClearSurface(rows);
            if (items.Count == 0)
                return;

            if (Orientation == BarChartOrientation.Vertical)
                RenderVertical(rows);
            else
                RenderHorizontal(rows);
        }

        private void RenderHorizontal(IList<Row> rows)
        {
            double max = ResolveMaximum();
            int rowsToRender = Math.Min(Height, items.Count);
            int labelWidth = Math.Clamp(LabelWidth, (short)0, Width);
            int barStart = labelWidth;
            int barWidth = Math.Max(0, Width - barStart);

            for (int row = 0; row < rowsToRender; row++)
            {
                BarChartItem item = items[row];
                string label = TuiText.PadRightToVisualWidth(item.Label, labelWidth);
                for (int x = 0; x < labelWidth; x++)
                    WriteCell(rows, x, row, TuiText.CellAt(label, x), ForeColor, BackgroundColor);

                int filled = max <= 0 ? 0 : (int)Math.Round(barWidth * item.Value / max, MidpointRounding.AwayFromZero);
                filled = Math.Clamp(filled, 0, barWidth);
                for (int x = 0; x < barWidth; x++)
                    WriteCell(rows, barStart + x, row, x < filled ? "█" : " ", ForeColor, BackgroundColor);
            }
        }

        private void RenderVertical(IList<Row> rows)
        {
            double max = ResolveMaximum();
            int bars = Math.Min(Width, items.Count);
            for (int x = 0; x < bars; x++)
            {
                int filled = max <= 0 ? 0 : (int)Math.Round(Height * items[x].Value / max, MidpointRounding.AwayFromZero);
                filled = Math.Clamp(filled, 0, Height);
                for (int y = 0; y < Height; y++)
                {
                    bool isFilled = Height - y <= filled;
                    WriteCell(rows, x, y, isFilled ? "█" : " ", ForeColor, BackgroundColor);
                }
            }
        }

        private double ResolveMaximum()
            => maximum ?? items.Select(item => item.Value).DefaultIfEmpty(0).Max();

        private void ClearSurface(IList<Row> rows)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    WriteCell(rows, x, y, " ", ForeColor, BackgroundColor);
            }
        }

        private void WriteCell(IList<Row> rows, int localX, int localY, string character, Color foreColor, Color backgroundColor)
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
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = character;
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }
    }
}
