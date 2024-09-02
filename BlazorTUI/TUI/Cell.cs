using System.Drawing;


namespace BlazorTUI.TUI
{
    public class Cell
    {
        public short x { get; set; }
        public short y { get; set; }

        public Color foreColor { get; set; }
        public Color backgroundColor { get; set; }

        public enum TextDecoration
        {
            UnderLine,
            OverLine,
            LineThrough,
            None
        }

        public TextDecoration textDecoration { get; set; }

        public string character { get; set; }

        public bool visible { get; set; }

        public string backgroundImage {get; set;}

        public double scaleX { get; set;}

        public double scaleY { get; set;}

    }
}
