using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class ToggleSwitch : Control
    {
        private bool value;

        public ToggleSwitch(
            string name,
            string text,
            bool value,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)8);

            Name = name;
            Text = text ?? "";
            this.value = value;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
        }

        public string Text { get; set; }

        public bool Value
        {
            get => value;
            set
            {
                if (this.value == value)
                    return;

                this.value = value;
                if (!TuiEventScope.EventsSuppressed)
                    ValueChanged?.Invoke(this, new ToggleSwitchValueChangedEventArgs(this.value));
            }
        }

        public event EventHandler<ToggleSwitchValueChangedEventArgs>? ValueChanged;

        public override string GetAccessibilitySummary()
            => FormatAccessibilitySummary($"ToggleSwitch {Name}: {Text}, {(Value ? "on" : "off")}.");

        public void Toggle()
            => Value = !Value;

        public override bool Click(short X, short Y)
        {
            if (!Visible || Y != 0)
                return false;

            container.TopContainer().SetFocus(Name);
            Toggle();
            NotifyClicked();
            return true;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            switch (key)
            {
                case "Enter":
                case "Space":
                case " ":
                    Toggle();
                    NotifyClicked();
                    return true;
                default:
                    return false;
            }
        }

        public override void Render(IList<Row> rows)
        {
            if (!Visible || container is null)
                return;

            string stateText = Value ? " ON " : " OFF";
            string rendered = $"[{stateText}] {Text}";
            rendered = TuiText.PadRightToVisualWidth(rendered, Width);
            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, out Cell cell))
                    continue;

                cell.ForeColor = Focus ? BackgroundColor : ForeColor;
                cell.BackgroundColor = Focus ? ForeColor : BackgroundColor;
                cell.Character = TuiText.CellAt(rendered, x);
                cell.Decoration = Cell.TextDecoration.None;
                cell.IsVisible = true;
                cell.BackgroundImage = "";
                cell.ScaleX = 1;
                cell.ScaleY = 1;
            }
        }

        protected override object? GetValidationValue() => Value;

        private bool TryGetCell(IList<Row> rows, int localX, out Cell cell)
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
                cell = null!;
                return false;
            }

            cell = rows[absoluteY].Cells[absoluteX];
            return true;
        }
    }
}
