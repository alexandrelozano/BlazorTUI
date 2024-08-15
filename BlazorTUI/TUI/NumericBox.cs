using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorTUI.TUI
{
    public class NumericBox : TextBox
    {
        public Double? number;

        short integerPlaces;
        short decimalPlaces;
        char separator;

        public NumericBox(string name, Double? number, short integerPlaces, short decimalPlaces, char separator, short X, short Y, Color forecolor, Color backgroundcolor) 
            : base(name, "", X, Y, 1, forecolor, backgroundcolor)
        {
            this.number = number;
            this.integerPlaces = integerPlaces;
            this.decimalPlaces = decimalPlaces;
            this.separator = separator;

            if (decimalPlaces > 0)
                this.width = (short)(integerPlaces + decimalPlaces + 2);
            else
                this.width = (short)(integerPlaces + 1);

            if (this.number != null)
            {
                string format = $"{new string('0', integerPlaces)}.{new string('0', decimalPlaces)}";
                text = this.number.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture).Replace('.', separator);
            }
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            bool handled = false;

            if (Visible)
            {
                switch (key)
                {
                    case "Tab":
                        break;
                    case "Enter":
                        break;
                    case "Backspace":
                        if (!string.IsNullOrEmpty(text) && text.Length > 0)
                            text = text.Remove(text.Length - 1, 1);

                        if (text.Length == integerPlaces && decimalPlaces > 0)
                            text = text.Remove(text.Length - 1, 1);

                        handled = true;
                        break;
                    case "ArrowRight":
                        break;
                    case "ArrowLeft":
                        break;
                    default:
                        int n;
                        if (int.TryParse(key, out n))
                        {
                            if (text.Length < (short)(integerPlaces + decimalPlaces + 1) && text.Length != integerPlaces)
                                text += key;
                            handled = true;
                        }
                        break;
                }

                if (text.Length == integerPlaces && decimalPlaces > 0)
                    text += separator;

                if (text.Length == integerPlaces + decimalPlaces + 1)
                {
                    this.number = double.Parse(text.Replace(separator, '.'), CultureInfo.InvariantCulture);
                }
            }

            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                for (short n = 0; n < width; n++)
                {
                    if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                    {
                        if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                        {
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = foreColor;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;

                            string ch = (n < text.Length) ? text.Substring(n, 1) : " ";

                            if (n == integerPlaces && decimalPlaces > 0)
                                ch = separator.ToString();

                            if (Focus)
                            {
                                if (n == text.Length)
                                {
                                    if (blinkCursor)
                                        ch = "_";

                                    blinkCursor = !blinkCursor;
                                }
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }
    }
}
