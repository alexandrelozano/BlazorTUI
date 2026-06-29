using System.Drawing;
using System.Globalization;

namespace BlazorTUI.TUI
{
    public class Sparkline : Control
    {
        private static readonly string[] Glyphs = { "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" };
        private readonly List<double> values = new();
        private double? minimum;
        private double? maximum;

        public IReadOnlyList<double> Values => values;

        public double? Minimum
        {
            get => minimum;
            set
            {
                if (value.HasValue)
                    ValidateFinite(value.Value, nameof(value));

                minimum = value;
            }
        }

        public double? Maximum
        {
            get => maximum;
            set
            {
                if (value.HasValue)
                    ValidateFinite(value.Value, nameof(value));

                maximum = value;
            }
        }

        public Sparkline(
            string name,
            IEnumerable<double> values,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(values);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
            SetValues(values);
        }

        public void SetValues(IEnumerable<double> nextValues)
        {
            ArgumentNullException.ThrowIfNull(nextValues);
            values.Clear();
            foreach (double value in nextValues)
            {
                ValidateFinite(value, nameof(nextValues));
                values.Add(value);
            }
        }

        public void AddValue(double value)
        {
            ValidateFinite(value, nameof(value));
            values.Add(value);
        }

        public void ClearValues() => values.Clear();

        internal void ExportSparklineState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetStringList(
                "Values",
                values.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            if (minimum.HasValue)
                state.SetString("Minimum", minimum.Value.ToString("R", CultureInfo.InvariantCulture));
            if (maximum.HasValue)
                state.SetString("Maximum", maximum.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal void RestoreSparklineState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetStringList("Values", out IReadOnlyList<string> storedValues))
            {
                SetValues(storedValues
                    .Select(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : double.NaN)
                    .Where(value => !double.IsNaN(value)));
            }

            if (state.TryGetString("Minimum", out string storedMinimum) &&
                double.TryParse(storedMinimum, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMinimum))
            {
                Minimum = parsedMinimum;
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

            for (int x = 0; x < Width; x++)
                WriteCell(rows, x, " ");

            if (values.Count == 0)
                return;

            double min = minimum ?? values.Min();
            double max = maximum ?? values.Max();
            for (int x = 0; x < Width; x++)
            {
                double value = SampleValue(x);
                WriteCell(rows, x, GlyphFor(value, min, max));
            }
        }

        private double SampleValue(int localX)
        {
            if (values.Count == 1 || Width <= 1)
                return values[0];

            double position = localX * (values.Count - 1.0) / (Width - 1.0);
            int index = (int)Math.Round(position, MidpointRounding.AwayFromZero);
            return values[Math.Clamp(index, 0, values.Count - 1)];
        }

        private static string GlyphFor(double value, double min, double max)
        {
            if (max <= min)
                return Glyphs[^1];

            double ratio = Math.Clamp((value - min) / (max - min), 0.0, 1.0);
            int index = (int)Math.Round(ratio * (Glyphs.Length - 1), MidpointRounding.AwayFromZero);
            return Glyphs[index];
        }

        private void WriteCell(IList<Row> rows, int localX, string character)
        {
            int absoluteX = container.XOffset() + X + localX;
            int absoluteY = container.YOffset() + Y;
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

        private static void ValidateFinite(double value, string paramName)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
