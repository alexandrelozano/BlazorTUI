using System.Drawing;
using System.Globalization;
using System.Text;

namespace BlazorTUI.TUI
{
    public class Cell
    {
        private short _x;
        private short _y;
        private Color _foreColor;
        private Color _backgroundColor;
        private TextDecoration _textDecoration;
        private string _character = "";
        private bool _visible;
        private string _backgroundImage = "";
        private double _scaleX;
        private double _scaleY;
        private bool _styleDirty = true;
        private string _cssStyle = "";

        public short x { get => _x; set => SetField(ref _x, value, true); }
        public short X { get => x; set => x = value; }
        public short y { get => _y; set => SetField(ref _y, value, true); }
        public short Y { get => y; set => y = value; }
        public Color foreColor { get => _foreColor; set => SetField(ref _foreColor, value, true); }
        public Color ForeColor { get => foreColor; set => foreColor = value; }
        public Color backgroundColor { get => _backgroundColor; set => SetField(ref _backgroundColor, value, true); }
        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        public enum TextDecoration
        {
            UnderLine,
            OverLine,
            LineThrough,
            None
        }

        public TextDecoration textDecoration { get => _textDecoration; set => SetField(ref _textDecoration, value, true); }
        public TextDecoration Decoration { get => textDecoration; set => textDecoration = value; }
        public string character { get => _character; set => SetField(ref _character, value ?? ""); }
        public string Character { get => character; set => character = value; }
        public bool visible { get => _visible; set => SetField(ref _visible, value); }
        public bool IsVisible { get => visible; set => visible = value; }
        public string backgroundImage { get => _backgroundImage; set => SetField(ref _backgroundImage, value ?? "", true); }
        public string BackgroundImage { get => backgroundImage; set => backgroundImage = value; }
        public double scaleX { get => _scaleX; set => SetField(ref _scaleX, value, true); }
        public double ScaleX { get => scaleX; set => scaleX = value; }
        public double scaleY { get => _scaleY; set => SetField(ref _scaleY, value, true); }
        public double ScaleY { get => scaleY; set => scaleY = value; }

        internal string CssStyle
        {
            get
            {
                if (_styleDirty)
                {
                    _cssStyle = BuildCssStyle();
                    _styleDirty = false;
                }

                return _cssStyle;
            }
        }

        private void SetField<T>(ref T field, T value, bool affectsStyle = false)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            _styleDirty |= affectsStyle;
        }

        private string BuildCssStyle()
        {
            var style = new StringBuilder(128);
            style.Append("grid-column:").Append(_x + 1).Append(';');
            style.Append(" grid-row:").Append(_y + 1).Append(';');
            style.Append(" color:").Append(ToHex(_foreColor));
            style.Append("; background-color:").Append(ToHex(_backgroundColor)).Append(';');

            switch (_textDecoration)
            {
                case TextDecoration.UnderLine:
                    style.Append(" text-decoration:underline; box-shadow:inset 0 -0.08em currentColor;");
                    break;
                case TextDecoration.OverLine: style.Append(" text-decoration:overline;"); break;
                case TextDecoration.LineThrough: style.Append(" text-decoration:line-through;"); break;
            }

            if (!string.IsNullOrEmpty(_backgroundImage))
            {
                style.Append(" background-image:url('").Append(_backgroundImage);
                style.Append("'); background-repeat:no-repeat; background-size:100% 100%;");
                style.Append(" background-position:center top; background-attachment:fixed;");
            }

            if (_scaleX != 1 || _scaleY != 1)
            {
                style.Append(" transform:");
                if (_scaleX != 1) style.Append(" scaleX(").Append(_scaleX.ToString(CultureInfo.InvariantCulture)).Append(')');
                if (_scaleY != 1) style.Append(" scaleY(").Append(_scaleY.ToString(CultureInfo.InvariantCulture)).Append(')');
                style.Append("; transform-origin:left top;");
            }

            style.Append(" white-space:pre;");
            return style.ToString();
        }

        private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
