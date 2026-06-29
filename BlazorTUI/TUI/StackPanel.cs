using System.Drawing;

namespace BlazorTUI.TUI
{
    public class StackPanel : LayoutPanel
    {
        private short spacing;
        private LayoutOrientation orientation;
        private LayoutAlignment alignment = LayoutAlignment.Stretch;

        public StackPanel(
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

        public short Spacing
        {
            get => spacing;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                spacing = value;
            }
        }

        public LayoutAlignment Alignment
        {
            get => alignment;
            set
            {
                ValidateAlignment(value);
                alignment = value;
            }
        }

        protected override void ArrangeChildren()
        {
            int cursor = orientation == LayoutOrientation.Vertical ? ContentTop : ContentLeft;
            foreach (LayoutChild child in GetLayoutChildren())
            {
                int childWidth = GetChildWidth(child);
                int childHeight = GetChildHeight(child);
                if (orientation == LayoutOrientation.Vertical)
                {
                    int width = alignment == LayoutAlignment.Stretch ? ContentWidth : childWidth;
                    int x = GetAlignedOffset(ContentLeft, ContentWidth, width);
                    SetChildBounds(child, x, cursor, width, childHeight);
                    cursor += childHeight + spacing;
                }
                else
                {
                    int height = alignment == LayoutAlignment.Stretch ? ContentHeight : childHeight;
                    int y = GetAlignedOffset(ContentTop, ContentHeight, height);
                    SetChildBounds(child, cursor, y, childWidth, height);
                    cursor += childWidth + spacing;
                }
            }
        }

        private int GetAlignedOffset(int start, int available, int size)
            => alignment switch
            {
                LayoutAlignment.Center => start + Math.Max(0, (available - size) / 2),
                LayoutAlignment.End => start + Math.Max(0, available - size),
                _ => start
            };

        private static void ValidateOrientation(LayoutOrientation orientation)
        {
            if (!Enum.IsDefined(orientation))
                throw new ArgumentOutOfRangeException(nameof(orientation));
        }

        private static void ValidateAlignment(LayoutAlignment alignment)
        {
            if (!Enum.IsDefined(alignment))
                throw new ArgumentOutOfRangeException(nameof(alignment));
        }
    }
}
