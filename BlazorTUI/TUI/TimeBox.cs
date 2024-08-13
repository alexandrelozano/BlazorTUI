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
    public class TimeBox : TextBox
    {
        public TimeOnly? time;

        public TimeBox(string name, TimeOnly? time, short X, short Y, Color forecolor, Color backgroundcolor) : base(name, "", X, Y, 6, forecolor, backgroundcolor)
        {
            this.time = time;

            if (this.time != null)
                text = this.time.Value.ToString("HH:mm");
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

                        if (text.Length == 2)
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
                            switch (text.Length)
                            {
                                case 0:
                                    if (n < 2)
                                        text += key;
                                    break;
                                case 1:
                                    if (n < 10)
                                        text += key;
                                    break;
                                case 3:
                                    if (n < 6)
                                        text += key;
                                    break;
                                case 4:
                                    if (n < 10)
                                        text += key;
                                    break;
                            }
                            handled = true;
                        }
                        break;
                }

                if (text.Length == 2)
                    text += ":";

                if (text.Length == 5)
                {
                    this.time = TimeOnly.ParseExact(text, "HH:mm", CultureInfo.InvariantCulture);
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

                            if (n == 2)
                                ch = ":";

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
