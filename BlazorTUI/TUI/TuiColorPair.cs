using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class TuiColorPair
    {
        public TuiColorPair(Color foreColor, Color backgroundColor)
        {
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
        }

        public Color ForeColor { get; set; }

        public Color BackgroundColor { get; set; }

        public TuiColorPair Clone()
            => new(ForeColor, BackgroundColor);
    }
}
