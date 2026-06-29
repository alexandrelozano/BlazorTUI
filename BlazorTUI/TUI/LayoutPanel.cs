using System.Drawing;

namespace BlazorTUI.TUI
{
    public abstract class LayoutPanel : Container
    {
        private readonly List<object> childOrder = new();
        private short paddingLeft;
        private short paddingTop;
        private short paddingRight;
        private short paddingBottom;

        protected LayoutPanel(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
            : base(name)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
        }

        public Color ForeColor { get; set; }

        public Color BackgroundColor { get; set; }

        public bool FillBackground { get; set; } = true;

        public short PaddingLeft
        {
            get => paddingLeft;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                paddingLeft = value;
            }
        }

        public short PaddingTop
        {
            get => paddingTop;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                paddingTop = value;
            }
        }

        public short PaddingRight
        {
            get => paddingRight;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                paddingRight = value;
            }
        }

        public short PaddingBottom
        {
            get => paddingBottom;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                paddingBottom = value;
            }
        }

        public short Padding
        {
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                paddingLeft = value;
                paddingTop = value;
                paddingRight = value;
                paddingBottom = value;
            }
        }

        public new void AddControl(Control control)
        {
            base.AddControl(control);
            TrackChild(control);
        }

        public new void AddContainer(Container container)
        {
            base.AddContainer(container);
            TrackChild(container);
        }

        public override void Click(short X, short Y)
        {
            ArrangeChildren();
            base.Click(X, Y);
        }

        public override void KeyDown(string key, bool shiftKey)
        {
            ArrangeChildren();
            base.KeyDown(key, shiftKey);
        }

        public override bool Validate(bool focusFirstInvalid = true)
        {
            ArrangeChildren();
            return base.Validate(focusFirstInvalid);
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            ArrangeChildren();
            if (FillBackground)
                FillPanelBackground(rows);

            base.Render(rows);
        }

        protected abstract void ArrangeChildren();

        protected int ContentLeft => PaddingLeft;

        protected int ContentTop => PaddingTop;

        protected int ContentWidth => Math.Max(1, Width - PaddingLeft - PaddingRight);

        protected int ContentHeight => Math.Max(1, Height - PaddingTop - PaddingBottom);

        protected IReadOnlyList<LayoutChild> GetLayoutChildren(bool visibleOnly = true)
        {
            var result = new List<LayoutChild>();
            var emitted = new HashSet<object>(ReferenceEqualityComparer.Instance);

            foreach (object child in childOrder)
            {
                if (TryCreateLayoutChild(child, visibleOnly, out LayoutChild layoutChild))
                {
                    result.Add(layoutChild);
                    emitted.Add(child);
                }
            }

            foreach (Control control in controls)
            {
                if (emitted.Contains(control))
                    continue;
                if (visibleOnly && !control.Visible)
                    continue;
                result.Add(new LayoutChild(control));
            }

            foreach (Container container in containers)
            {
                if (emitted.Contains(container))
                    continue;
                if (visibleOnly && !container.Visible)
                    continue;
                result.Add(new LayoutChild(container));
            }

            return result;
        }

        protected static int GetChildWidth(LayoutChild child)
            => child.Control?.Width ?? child.Container!.Width;

        protected static int GetChildHeight(LayoutChild child)
            => child.Control?.Height ?? child.Container!.Height;

        protected static void SetChildBounds(LayoutChild child, int x, int y, int width, int height)
        {
            short childX = ToPositiveShort(x);
            short childY = ToPositiveShort(y);
            short childWidth = ToPositiveShort(Math.Max(1, width));
            short childHeight = ToPositiveShort(Math.Max(1, height));

            if (child.Control is not null)
            {
                child.Control.X = childX;
                child.Control.Y = childY;
                child.Control.Width = childWidth;
                child.Control.Height = childHeight;
            }
            else
            {
                child.Container!.X = childX;
                child.Container.Y = childY;
                child.Container.Width = childWidth;
                child.Container.Height = childHeight;
            }
        }

        protected void FillPanelBackground(IList<Row> rows)
        {
            int originX = XOffset();
            int originY = YOffset();
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
                    cell.IsVisible = true;
                    cell.BackgroundImage = "";
                    cell.ScaleX = 1;
                    cell.ScaleY = 1;
                }
            }
        }

        protected bool TryGetCell(IList<Row> rows, int x, int y, out Cell cell)
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

        private void TrackChild(object child)
        {
            if (!childOrder.Any(existing => ReferenceEquals(existing, child)))
                childOrder.Add(child);
        }

        private bool TryCreateLayoutChild(object child, bool visibleOnly, out LayoutChild layoutChild)
        {
            if (child is Control control && controls.Any(existing => ReferenceEquals(existing, control)))
            {
                if (visibleOnly && !control.Visible)
                {
                    layoutChild = default;
                    return false;
                }

                layoutChild = new LayoutChild(control);
                return true;
            }

            if (child is Container container && containers.Any(existing => ReferenceEquals(existing, container)))
            {
                if (visibleOnly && !container.Visible)
                {
                    layoutChild = default;
                    return false;
                }

                layoutChild = new LayoutChild(container);
                return true;
            }

            layoutChild = default;
            return false;
        }

        private static short ToPositiveShort(int value)
            => (short)Math.Clamp(value, 0, short.MaxValue);

        protected readonly struct LayoutChild
        {
            public LayoutChild(Control control)
            {
                Control = control;
                Container = null;
            }

            public LayoutChild(Container container)
            {
                Control = null;
                Container = container;
            }

            public Control? Control { get; }

            public Container? Container { get; }

            public object Instance => Control ?? (object)Container!;
        }
    }
}
