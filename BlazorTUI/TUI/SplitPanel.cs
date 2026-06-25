using System.Drawing;

namespace BlazorTUI.TUI
{
    public class SplitPanel : Container
    {
        private SplitPanelOrientation orientation;
        private short splitterPosition;
        private short minimumFirstPaneSize = 1;
        private short minimumSecondPaneSize = 1;
        private string verticalSplitterCharacter = "│";
        private string horizontalSplitterCharacter = "─";

        public Container FirstPanel { get; }

        public Container SecondPanel { get; }

        public Container FirstPane => FirstPanel;

        public Container SecondPane => SecondPanel;

        public SplitPanelOrientation Orientation
        {
            get => orientation;
            set
            {
                ValidateOrientation(value);
                if (value == orientation)
                    return;

                orientation = value;
                ValidateSplitConstraints(minimumFirstPaneSize, minimumSecondPaneSize);
                ValidateSplitterPosition(splitterPosition);
                UpdatePaneLayout();
            }
        }

        public short SplitterPosition
        {
            get => splitterPosition;
            set => SetSplitterPosition(value);
        }

        public short FirstPaneSize => splitterPosition;

        public short SecondPaneSize => (short)(AxisLength - splitterPosition - 1);

        public short MinimumFirstPaneSize
        {
            get => minimumFirstPaneSize;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, (short)1);
                ValidateSplitConstraints(value, minimumSecondPaneSize);
                minimumFirstPaneSize = value;
                ClampSplitterPosition();
            }
        }

        public short MinimumSecondPaneSize
        {
            get => minimumSecondPaneSize;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, (short)1);
                ValidateSplitConstraints(minimumFirstPaneSize, value);
                minimumSecondPaneSize = value;
                ClampSplitterPosition();
            }
        }

        public Color ForeColor { get; set; }

        public Color BackgroundColor { get; set; }

        public string VerticalSplitterCharacter
        {
            get => verticalSplitterCharacter;
            set => verticalSplitterCharacter = NormalizeSplitterCharacter(value);
        }

        public string HorizontalSplitterCharacter
        {
            get => horizontalSplitterCharacter;
            set => horizontalSplitterCharacter = NormalizeSplitterCharacter(value);
        }

        public event EventHandler<SplitPanelResizedEventArgs>? SplitterMoved;

        public SplitPanel(
            string name,
            short X,
            short Y,
            short width,
            short height,
            SplitPanelOrientation orientation,
            Color foreColor,
            Color backgroundColor)
            : this(
                name,
                X,
                Y,
                width,
                height,
                orientation,
                GetDefaultSplitterPosition(width, height, orientation),
                foreColor,
                backgroundColor)
        {
        }

        public SplitPanel(
            string name,
            short X,
            short Y,
            short width,
            short height,
            SplitPanelOrientation orientation,
            short splitterPosition,
            Color foreColor,
            Color backgroundColor)
            : base(name)
        {
            ValidateOrientation(orientation);
            ValidateDimensions(width, height, orientation);

            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            this.orientation = orientation;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            FirstPanel = new Container($"{name}FirstPanel") { Width = 1, Height = 1 };
            SecondPanel = new Container($"{name}SecondPanel") { Width = 1, Height = 1 };

            base.AddContainer(FirstPanel);
            base.AddContainer(SecondPanel);

            SetSplitterPosition(splitterPosition, raiseEvent: false);
        }

        public void SetSplitterPosition(short position)
            => SetSplitterPosition(position, raiseEvent: true);

        public void MoveSplitter(short delta)
        {
            if (delta == 0)
                return;

            int target = splitterPosition + delta;
            short clampedTarget = (short)Math.Clamp(target, MinimumSplitterPosition, MaximumSplitterPosition);
            SetSplitterPosition(clampedTarget);
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            UpdatePaneLayout();
            int originX = XOffset();
            int originY = YOffset();
            FillBackground(rows, originX, originY);
            DrawSplitter(rows, originX, originY);

            base.Render(rows);
        }

        internal IReadOnlyList<Control> GetFocusableControlsInTabOrder()
            => EnumerateFocusableControls(this)
                .OrderBy(control => control.TabIndex)
                .ThenBy(control => control.Name)
                .ToList();

        private void SetSplitterPosition(short position, bool raiseEvent)
        {
            ValidateSplitConstraints(minimumFirstPaneSize, minimumSecondPaneSize);
            ValidateSplitterPosition(position);

            short previousPosition = splitterPosition;
            splitterPosition = position;
            UpdatePaneLayout();

            if (raiseEvent && previousPosition != splitterPosition)
            {
                SplitterMoved?.Invoke(
                    this,
                    new SplitPanelResizedEventArgs(
                        previousPosition,
                        splitterPosition,
                        FirstPaneSize,
                        SecondPaneSize));
            }
        }

        private void ClampSplitterPosition()
        {
            short clampedPosition = (short)Math.Clamp(
                splitterPosition,
                MinimumSplitterPosition,
                MaximumSplitterPosition);
            SetSplitterPosition(clampedPosition);
        }

        private int AxisLength => orientation == SplitPanelOrientation.Vertical ? Width : Height;

        private short MinimumSplitterPosition => minimumFirstPaneSize;

        private short MaximumSplitterPosition => (short)(AxisLength - minimumSecondPaneSize - 1);

        private void ValidateSplitterPosition(short position)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(position, MinimumSplitterPosition);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(position, MaximumSplitterPosition);
        }

        private void ValidateSplitConstraints(short firstMinimum, short secondMinimum)
        {
            if (AxisLength < firstMinimum + secondMinimum + 1)
                throw new ArgumentOutOfRangeException(nameof(firstMinimum), "The split panel is too small for the configured pane minimums.");
        }

        private void UpdatePaneLayout()
        {
            if (orientation == SplitPanelOrientation.Vertical)
            {
                FirstPanel.X = 0;
                FirstPanel.Y = 0;
                FirstPanel.Width = splitterPosition;
                FirstPanel.Height = Height;
                SecondPanel.X = (short)(splitterPosition + 1);
                SecondPanel.Y = 0;
                SecondPanel.Width = SecondPaneSize;
                SecondPanel.Height = Height;
            }
            else
            {
                FirstPanel.X = 0;
                FirstPanel.Y = 0;
                FirstPanel.Width = Width;
                FirstPanel.Height = splitterPosition;
                SecondPanel.X = 0;
                SecondPanel.Y = (short)(splitterPosition + 1);
                SecondPanel.Width = Width;
                SecondPanel.Height = SecondPaneSize;
            }
        }

        private void FillBackground(IList<Row> rows, int originX, int originY)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, originX + x, originY + y, out Cell cell))
                        continue;

                    cell.ForeColor = ForeColor;
                    cell.BackgroundColor = BackgroundColor;
                    cell.Character = " ";
                    cell.Decoration = Cell.TextDecoration.None;
                    cell.BackgroundImage = "";
                    cell.ScaleX = 1;
                    cell.ScaleY = 1;
                }
            }
        }

        private void DrawSplitter(IList<Row> rows, int originX, int originY)
        {
            if (orientation == SplitPanelOrientation.Vertical)
            {
                int splitterX = originX + splitterPosition;
                for (int y = 0; y < Height; y++)
                {
                    if (TryGetCell(rows, splitterX, originY + y, out Cell cell))
                        cell.Character = verticalSplitterCharacter;
                }
            }
            else
            {
                int splitterY = originY + splitterPosition;
                for (int x = 0; x < Width; x++)
                {
                    if (TryGetCell(rows, originX + x, splitterY, out Cell cell))
                        cell.Character = horizontalSplitterCharacter;
                }
            }
        }

        private bool TryGetCell(IList<Row> rows, int x, int y, out Cell cell)
        {
            int minimumX = parent?.XOffset() ?? 0;
            int minimumY = parent?.YOffset() ?? 0;
            int maximumX = parent is null ? int.MaxValue : minimumX + parent.Width;
            int maximumY = parent is null ? int.MaxValue : minimumY + parent.Height;

            if (y < minimumY || y >= maximumY || y < 0 || y >= rows.Count ||
                x < minimumX || x >= maximumX || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private static IEnumerable<Control> EnumerateFocusableControls(Container container)
        {
            foreach (Control control in container.Controls.Where(control => control.Visible && control.TabStop))
                yield return control;

            foreach (Container child in container.Containers.Where(child => child.Visible).OrderBy(child => child.ZOrder))
            {
                foreach (Control control in EnumerateFocusableControls(child))
                    yield return control;
            }
        }

        private static short GetDefaultSplitterPosition(short width, short height, SplitPanelOrientation orientation)
        {
            ValidateOrientation(orientation);
            ValidateDimensions(width, height, orientation);
            int axisLength = orientation == SplitPanelOrientation.Vertical ? width : height;
            return (short)Math.Max(1, (axisLength - 1) / 2);
        }

        private static void ValidateDimensions(short width, short height, SplitPanelOrientation orientation)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            if (orientation == SplitPanelOrientation.Vertical)
                ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)3);
            else
                ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)3);
        }

        private static void ValidateOrientation(SplitPanelOrientation orientation)
        {
            if (!Enum.IsDefined(orientation))
                throw new ArgumentOutOfRangeException(nameof(orientation));
        }

        private static string NormalizeSplitterCharacter(string? value)
            => string.IsNullOrEmpty(value) ? " " : value[..1];
    }
}
