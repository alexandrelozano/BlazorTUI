using System.Drawing;

namespace BlazorTUI.TUI
{
    public class Slider : Control
    {
        private int minimum;
        private int maximum;
        private int value;
        private int step;
        private int largeChange;

        public int Minimum
        {
            get => minimum;
            set
            {
                if (value >= maximum)
                    throw new ArgumentOutOfRangeException(nameof(Minimum), "Minimum must be less than Maximum.");

                minimum = value;
                if (this.value < minimum)
                    SetValueCore(minimum);
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                if (value <= minimum)
                    throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be greater than Minimum.");

                maximum = value;
                if (this.value > maximum)
                    SetValueCore(maximum);
            }
        }

        public int Value
        {
            get => value;
            set => SetValue(value);
        }

        public int Step
        {
            get => step;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                step = value;
            }
        }

        public int LargeChange
        {
            get => largeChange;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                largeChange = value;
            }
        }

        public SliderOrientation Orientation { get; }

        public short Length => Orientation == SliderOrientation.Horizontal ? Width : Height;

        public double Percentage
            => (this.value - (double)minimum) / (maximum - (double)minimum) * 100d;

        public event EventHandler<SliderValueChangedEventArgs>? ValueChanged;

        public Slider(
            string name,
            int minimum,
            int maximum,
            int value,
            int step,
            short X,
            short Y,
            short length,
            Color foreColor,
            Color backgroundColor)
            : this(
                name,
                minimum,
                maximum,
                value,
                step,
                X,
                Y,
                length,
                SliderOrientation.Horizontal,
                foreColor,
                backgroundColor)
        {
        }

        public Slider(
            string name,
            int minimum,
            int maximum,
            int value,
            int step,
            short X,
            short Y,
            short length,
            SliderOrientation orientation,
            Color foreColor,
            Color backgroundColor,
            int? largeChange = null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minimum, maximum);
            if (value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(nameof(value));
            ArgumentOutOfRangeException.ThrowIfLessThan(step, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(length, (short)3);
            if (!Enum.IsDefined(orientation))
                throw new ArgumentOutOfRangeException(nameof(orientation));

            int defaultLargeChange = (int)Math.Min((long)step * 10, int.MaxValue);
            int resolvedLargeChange = largeChange ?? defaultLargeChange;
            ArgumentOutOfRangeException.ThrowIfLessThan(resolvedLargeChange, 1);

            Name = name;
            this.minimum = minimum;
            this.maximum = maximum;
            this.value = value;
            this.step = step;
            this.largeChange = resolvedLargeChange;
            this.X = X;
            this.Y = Y;
            Orientation = orientation;
            Width = orientation == SliderOrientation.Horizontal ? length : (short)1;
            Height = orientation == SliderOrientation.Horizontal ? (short)1 : length;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
        }

        public void SetValue(int newValue)
        {
            if (newValue < minimum || newValue > maximum)
                throw new ArgumentOutOfRangeException(nameof(newValue));
            SetValueCore(newValue);
        }

        public void Increase()
            => ChangeValueBy(step);

        public void Decrease()
            => ChangeValueBy(-step);

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            bool handled = true;
            bool changed = key switch
            {
                "ArrowRight" or "ArrowUp" => ChangeValueBy(step),
                "ArrowLeft" or "ArrowDown" => ChangeValueBy(-step),
                "PageUp" => ChangeValueBy(largeChange),
                "PageDown" => ChangeValueBy(-largeChange),
                "Home" => SetValueCore(minimum),
                "End" => SetValueCore(maximum),
                _ => handled = false
            };

            if (changed)
                NotifyClicked();
            return handled;
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y < 0 || Y >= Height)
                return false;

            container.TopContainer().SetFocus(Name);
            int position = Orientation == SliderOrientation.Horizontal ? X : Height - 1 - Y;
            int length = Length;
            int newValue;
            if (position == 0)
            {
                newValue = minimum;
            }
            else if (position == length - 1)
            {
                newValue = maximum;
            }
            else
            {
                double rawValue = minimum + (maximum - (double)minimum) * position / (length - 1);
                double stepsFromMinimum = Math.Round((rawValue - minimum) / step, MidpointRounding.AwayFromZero);
                long snappedValue = minimum + (long)(stepsFromMinimum * step);
                newValue = (int)Math.Clamp(snappedValue, minimum, maximum);
            }

            SetValueCore(newValue);
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            int knobPosition = GetKnobPosition();
            for (int position = 0; position < Length; position++)
            {
                int localX = Orientation == SliderOrientation.Horizontal ? position : 0;
                int localY = Orientation == SliderOrientation.Horizontal ? 0 : position;
                if (!TryGetCell(rows, localX, localY, out Cell cell))
                    continue;

                bool isKnob = position == knobPosition;
                PrepareCell(
                    cell,
                    Focus && isKnob ? BackgroundColor : ForeColor,
                    Focus && isKnob ? ForeColor : BackgroundColor);
                cell.Character = Orientation == SliderOrientation.Horizontal
                    ? isKnob ? "●" : position < knobPosition ? "━" : "─"
                    : isKnob ? "●" : position < knobPosition ? "│" : "┃";
            }
        }

        private bool ChangeValueBy(int amount)
        {
            long candidate = (long)value + amount;
            return SetValueCore((int)Math.Clamp(candidate, minimum, maximum));
        }

        private bool SetValueCore(int newValue)
        {
            if (value == newValue)
                return false;

            int previousValue = value;
            value = newValue;
            ValueChanged?.Invoke(this, new SliderValueChangedEventArgs(previousValue, newValue));
            return true;
        }

        private int GetKnobPosition()
        {
            double ratio = (value - (double)minimum) / (maximum - (double)minimum);
            int position = (int)Math.Round(ratio * (Length - 1), MidpointRounding.AwayFromZero);
            return Orientation == SliderOrientation.Horizontal ? position : Length - 1 - position;
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }
    }
}
