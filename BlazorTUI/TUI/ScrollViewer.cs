using System.Drawing;

namespace BlazorTUI.TUI
{
    public class ScrollViewer : LayoutPanel
    {
        private int horizontalOffset;
        private int verticalOffset;

        public ScrollViewer(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
            : base(name, X, Y, width, height, foreColor, backgroundColor)
        {
        }

        public int HorizontalOffset
        {
            get => horizontalOffset;
            set => ScrollTo(value, verticalOffset);
        }

        public int VerticalOffset
        {
            get => verticalOffset;
            set => ScrollTo(horizontalOffset, value);
        }

        public bool ShowScrollIndicators { get; set; } = true;

        public int ScrollableContentWidth => Math.Max(Width, GetContentRight());

        public int ScrollableContentHeight => Math.Max(Height, GetContentBottom());

        public int MaximumHorizontalOffset => Math.Max(0, ScrollableContentWidth - Width);

        public int MaximumVerticalOffset => Math.Max(0, ScrollableContentHeight - Height);

        public void ScrollTo(int horizontalOffset, int verticalOffset)
        {
            this.horizontalOffset = Math.Clamp(horizontalOffset, 0, MaximumHorizontalOffset);
            this.verticalOffset = Math.Clamp(verticalOffset, 0, MaximumVerticalOffset);
        }

        public void ScrollBy(int horizontalDelta, int verticalDelta)
            => ScrollTo(horizontalOffset + horizontalDelta, verticalOffset + verticalDelta);

        public override void Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y < 0 || Y >= Height)
                return;

            if (ShowScrollIndicators && TryHandleIndicatorClick(X, Y))
                return;

            short contentX = ToShort(X + horizontalOffset);
            short contentY = ToShort(Y + verticalOffset);
            base.Click(contentX, contentY);
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            ScrollTo(horizontalOffset, verticalOffset);
            ChildPosition[] positions = CaptureChildPositions();
            try
            {
                OffsetChildren(-horizontalOffset, -verticalOffset);
                base.Render(rows);
            }
            finally
            {
                RestoreChildPositions(positions);
            }

            if (ShowScrollIndicators)
                DrawScrollIndicators(rows);
        }

        protected override void ArrangeChildren()
        {
        }

        private bool TryHandleIndicatorClick(short X, short Y)
        {
            if (X == Width - 1 && MaximumVerticalOffset > 0)
            {
                short downArrowY = MaximumHorizontalOffset > 0 ? (short)(Height - 2) : (short)(Height - 1);
                if (Y == 0)
                {
                    ScrollBy(0, -1);
                    return true;
                }

                if (Y == downArrowY)
                {
                    ScrollBy(0, 1);
                    return true;
                }
            }

            if (Y == Height - 1 && MaximumHorizontalOffset > 0)
            {
                if (X == 0)
                {
                    ScrollBy(-1, 0);
                    return true;
                }

                if (X == Width - 1)
                {
                    ScrollBy(1, 0);
                    return true;
                }
            }

            return false;
        }

        private int GetContentRight()
        {
            int right = 0;
            foreach (LayoutChild child in GetLayoutChildren(visibleOnly: false))
            {
                int childRight = child.Control is not null
                    ? child.Control.X + child.Control.Width
                    : child.Container!.X + child.Container.Width;
                right = Math.Max(right, childRight);
            }

            return right;
        }

        private int GetContentBottom()
        {
            int bottom = 0;
            foreach (LayoutChild child in GetLayoutChildren(visibleOnly: false))
            {
                int childBottom = child.Control is not null
                    ? child.Control.Y + child.Control.Height
                    : child.Container!.Y + child.Container.Height;
                bottom = Math.Max(bottom, childBottom);
            }

            return bottom;
        }

        private ChildPosition[] CaptureChildPositions()
            => GetLayoutChildren(visibleOnly: false)
                .Select(child => new ChildPosition(child, GetX(child), GetY(child), GetVisible(child)))
                .ToArray();

        private void OffsetChildren(int deltaX, int deltaY)
        {
            foreach (LayoutChild child in GetLayoutChildren(visibleOnly: false))
            {
                int x = GetX(child) + deltaX;
                int y = GetY(child) + deltaY;
                bool visible = GetVisible(child);
                SetVisible(child, visible && x >= 0 && y >= 0 && x < Width && y < Height);
                SetX(child, ToShort(x));
                SetY(child, ToShort(y));
            }
        }

        private static void RestoreChildPositions(IEnumerable<ChildPosition> positions)
        {
            foreach (ChildPosition position in positions)
            {
                SetX(position.Child, position.X);
                SetY(position.Child, position.Y);
                SetVisible(position.Child, position.Visible);
            }
        }

        private void DrawScrollIndicators(IList<Row> rows)
        {
            int originX = XOffset();
            int originY = YOffset();

            if (MaximumVerticalOffset > 0)
            {
                int x = originX + Width - 1;
                bool hasHorizontalScrollBar = MaximumHorizontalOffset > 0;
                int downArrowRow = hasHorizontalScrollBar ? Height - 2 : Height - 1;
                for (int y = 0; y < Height; y++)
                {
                    if (!TryGetCell(rows, x, originY + y, out Cell cell))
                        continue;

                    cell.ForeColor = ForeColor;
                    cell.BackgroundColor = BackgroundColor;
                    cell.Character = y == 0
                        ? "↑"
                        : y == downArrowRow
                            ? "↓"
                            : "│";
                }
            }

            if (MaximumHorizontalOffset > 0)
            {
                int y = originY + Height - 1;
                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, originX + x, y, out Cell cell))
                        continue;

                    cell.ForeColor = ForeColor;
                    cell.BackgroundColor = BackgroundColor;
                    cell.Character = x == 0
                        ? "←"
                        : x == Width - 1
                            ? "→"
                            : "─";
                }
            }
        }

        private static short GetX(LayoutChild child)
            => child.Control?.X ?? child.Container!.X;

        private static short GetY(LayoutChild child)
            => child.Control?.Y ?? child.Container!.Y;

        private static void SetX(LayoutChild child, short value)
        {
            if (child.Control is not null)
                child.Control.X = value;
            else
                child.Container!.X = value;
        }

        private static void SetY(LayoutChild child, short value)
        {
            if (child.Control is not null)
                child.Control.Y = value;
            else
                child.Container!.Y = value;
        }

        private static bool GetVisible(LayoutChild child)
            => child.Control?.Visible ?? child.Container!.Visible;

        private static void SetVisible(LayoutChild child, bool value)
        {
            if (child.Control is not null)
                child.Control.Visible = value;
            else
                child.Container!.Visible = value;
        }

        private static short ToShort(int value)
            => (short)Math.Clamp(value, short.MinValue, short.MaxValue);

        private readonly record struct ChildPosition(LayoutChild Child, short X, short Y, bool Visible);
    }
}
