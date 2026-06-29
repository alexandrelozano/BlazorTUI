using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Gauge : Control
    {
        private double minimum;
        private double maximum;
        private double value;

        public double Minimum
        {
            get => minimum;
            set
            {
                ValidateFinite(value, nameof(value));
                if (value >= maximum)
                    throw new ArgumentOutOfRangeException(nameof(value));

                minimum = value;
                this.value = Math.Clamp(this.value, minimum, maximum);
            }
        }

        public double Maximum
        {
            get => maximum;
            set
            {
                ValidateFinite(value, nameof(value));
                if (value <= minimum)
                    throw new ArgumentOutOfRangeException(nameof(value));

                maximum = value;
                this.value = Math.Clamp(this.value, minimum, maximum);
            }
        }

        public double Value
        {
            get => value;
            set
            {
                ValidateFinite(value, nameof(value));
                this.value = Math.Clamp(value, minimum, maximum);
            }
        }

        public bool ShowPercentage { get; set; } = true;

        public Gauge(
            string name,
            double minimum,
            double maximum,
            double value,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            bool showPercentage = true)
        {
            ValidateFinite(minimum, nameof(minimum));
            ValidateFinite(maximum, nameof(maximum));
            ValidateFinite(value, nameof(value));
            if (maximum <= minimum)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)5);

            Name = name;
            this.minimum = minimum;
            this.maximum = maximum;
            this.value = Math.Clamp(value, minimum, maximum);
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            ShowPercentage = showPercentage;
            TabStop = false;
        }

        internal void ExportGaugeState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetString("Minimum", minimum.ToString("R", CultureInfo.InvariantCulture));
            state.SetString("Maximum", maximum.ToString("R", CultureInfo.InvariantCulture));
            state.SetString("Value", value.ToString("R", CultureInfo.InvariantCulture));
            state.SetBoolean("ShowPercentage", ShowPercentage);
        }

        internal void RestoreGaugeState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetString("Minimum", out string storedMinimum) &&
                state.TryGetString("Maximum", out string storedMaximum) &&
                double.TryParse(storedMinimum, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMinimum) &&
                double.TryParse(storedMaximum, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMaximum) &&
                parsedMaximum > parsedMinimum)
            {
                minimum = parsedMinimum;
                maximum = parsedMaximum;
            }

            if (state.TryGetString("Value", out string storedValue) &&
                double.TryParse(storedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
            {
                Value = parsedValue;
            }

            if (state.TryGetBoolean("ShowPercentage", out bool storedShowPercentage))
                ShowPercentage = storedShowPercentage;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null)
                return;

            double ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
            int innerWidth = Width - 2;
            int filled = (int)Math.Round(innerWidth * ratio, MidpointRounding.AwayFromZero);
            string text = ShowPercentage
                ? $"{(ratio * 100.0).ToString("0", CultureInfo.InvariantCulture)}%"
                : "";
            int textStart = Math.Max(1, (Width - TuiText.VisualWidth(text)) / 2);

            for (int x = 0; x < Width; x++)
            {
                string character = x switch
                {
                    0 => "[",
                    _ when x == Width - 1 => "]",
                    _ when x - 1 < filled => "█",
                    _ => " "
                };

                if (ShowPercentage && x >= textStart && x < textStart + TuiText.VisualWidth(text))
                    character = TuiText.CellAt(text, x - textStart);

                WriteCell(rows, x, character);
            }
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
