using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Tooltip : Control
    {
        private string text;
        private string targetControlName;
        private TuiCommand? command;

        public string Text
        {
            get => command is null
                ? text
                : string.IsNullOrWhiteSpace(command.Description) ? command.Label : command.Description;
            set
            {
                if (command is not null)
                    command.Description = value;
                else
                    text = value ?? "";
            }
        }

        public string TargetControlName
        {
            get => targetControlName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                targetControlName = value;
            }
        }

        public bool IsOpen { get; private set; }

        public bool AutoShowOnFocus { get; set; } = true;

        public TuiCommand? Command => command;

        public Tooltip(
            string name,
            string text,
            string targetControlName,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetControlName);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);

            Name = name;
            this.text = text ?? "";
            this.targetControlName = targetControlName;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
        }

        public Tooltip(
            string name,
            TuiCommand command,
            string targetControlName,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
            : this(
                name,
                command is null ? "" : string.IsNullOrWhiteSpace(command.Description) ? command.Label : command.Description,
                targetControlName,
                X,
                Y,
                width,
                foreColor,
                backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(command);
            BindCommand(command);
        }

        public void BindCommand(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            this.command = command;
            text = string.IsNullOrWhiteSpace(command.Description) ? command.Label : command.Description;
        }

        public void Show() => IsOpen = true;

        public void Hide() => IsOpen = false;

        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        internal void ExportTooltipState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetString("Text", text);
            state.SetBoolean("IsOpen", IsOpen);
            state.SetBoolean("AutoShowOnFocus", AutoShowOnFocus);
        }

        internal void RestoreTooltipState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetString("Text", out string restoredText))
                text = restoredText;

            IsOpen = state.TryGetBoolean("IsOpen", out bool isOpen) && isOpen;
            if (state.TryGetBoolean("AutoShowOnFocus", out bool autoShowOnFocus))
                AutoShowOnFocus = autoShowOnFocus;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || container is null || !ShouldRenderTooltip())
                return;

            string value = TuiText.PadRightToVisualWidth(Text, Width);
            for (int x = 0; x < Width; x++)
                WriteCell(rows, x, TuiText.CellAt(value, x));
        }

        private bool ShouldRenderTooltip()
        {
            if (command is not null && !command.Visible)
                return false;

            if (IsOpen)
                return true;

            if (!AutoShowOnFocus)
                return false;

            return container.TopContainer().GetControl(targetControlName) is { Focus: true };
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
    }
}
