using System.Drawing;

namespace BlazorTUI.TUI
{
    public class WrapPanel : LayoutPanel
    {
        private LayoutOrientation orientation;
        private short itemSpacing;
        private short lineSpacing;

        public WrapPanel(
            string name,
            short X,
            short Y,
            short width,
            short height,
            LayoutOrientation orientation,
            Color foreColor,
            Color backgroundColor)
            : base(name, X, Y, width, height, foreColor, backgroundColor)
        {
            ValidateOrientation(orientation);
            this.orientation = orientation;
        }

        public LayoutOrientation Orientation
        {
            get => orientation;
            set
            {
                ValidateOrientation(value);
                orientation = value;
            }
        }

        public short ItemSpacing
        {
            get => itemSpacing;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                itemSpacing = value;
            }
        }

        public short LineSpacing
        {
            get => lineSpacing;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                lineSpacing = value;
            }
        }

        protected override void ArrangeChildren()
        {
            if (orientation == LayoutOrientation.Horizontal)
                ArrangeHorizontal();
            else
                ArrangeVertical();
        }

        private void ArrangeHorizontal()
        {
            int x = ContentLeft;
            int y = ContentTop;
            int lineHeight = 0;
            int right = ContentLeft + ContentWidth;

            foreach (LayoutChild child in GetLayoutChildren())
            {
                int childWidth = GetChildWidth(child);
                int childHeight = GetChildHeight(child);
                if (x > ContentLeft && x + childWidth > right)
                {
                    x = ContentLeft;
                    y += lineHeight + lineSpacing;
                    lineHeight = 0;
                }

                SetChildBounds(child, x, y, childWidth, childHeight);
                x += childWidth + itemSpacing;
                lineHeight = Math.Max(lineHeight, childHeight);
            }
        }

        private void ArrangeVertical()
        {
            int x = ContentLeft;
            int y = ContentTop;
            int columnWidth = 0;
            int bottom = ContentTop + ContentHeight;

            foreach (LayoutChild child in GetLayoutChildren())
            {
                int childWidth = GetChildWidth(child);
                int childHeight = GetChildHeight(child);
                if (y > ContentTop && y + childHeight > bottom)
                {
                    y = ContentTop;
                    x += columnWidth + lineSpacing;
                    columnWidth = 0;
                }

                SetChildBounds(child, x, y, childWidth, childHeight);
                y += childHeight + itemSpacing;
                columnWidth = Math.Max(columnWidth, childWidth);
            }
        }

        private static void ValidateOrientation(LayoutOrientation orientation)
        {
            if (!Enum.IsDefined(orientation))
                throw new ArgumentOutOfRangeException(nameof(orientation));
        }
    }
}
