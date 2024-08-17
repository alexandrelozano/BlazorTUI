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
        public TimeOnly? value;

        public TimeBox(string name, TimeOnly? value, short X, short Y, Color forecolor, Color backgroundcolor) : base(name, "", X, Y, 6, forecolor, backgroundcolor)
        {
            this.value = value;

            if (this.value != null)
                text = this.value.Value.ToString("HH:mm");
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
                        if (cursor > 0)
                        {
                            if (cursor == 3)
                                cursor = 2;

                            text = text.Remove(cursor - 1);
                            cursor--;
                        }
                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursor < (short)text.Length)
                            cursor++;
                        if (cursor == 2)
                            cursor++;
                        break;
                    case "ArrowLeft":
                        if (cursor > 0)
                            cursor--;
                        if (cursor == 2)
                            cursor--;
                        break;
                    default:
                        int n;
                        string tmp = "";
                        if (int.TryParse(key, out n))
                        {
                            switch (cursor)
                            {
                                case 0:
                                    if (n < 2)
                                        tmp = key;
                                    break;
                                case 1:
                                    if (n < 10)
                                        tmp = key;
                                    break;
                                case 3:
                                    if (n < 6)
                                        tmp = key;
                                    break;
                                case 4:
                                    if (n < 10)
                                        tmp = key;
                                    break;
                            }

                            if (!string.IsNullOrEmpty(tmp))
                            {
                                if (cursor != text.Length)
                                {
                                    text = text.Insert(cursor, key);
                                    text = text.Remove(cursor + 1, 1);
                                }
                                else
                                    text += key;

                                cursor++;
                            }

                            handled = true;
                        }
                        break;
                }

                if (text.Length == 2)
                {
                    text += ":";
                    cursor++;
                }

                if (cursor == 2)
                {
                    cursor++;
                }

                if (text.Length == 5)
                {
                    this.value = TimeOnly.ParseExact(text, "HH:mm", CultureInfo.InvariantCulture);
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
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.None;

                            string ch = (n < text.Length) ? text.Substring(n, 1) : " ";

                            if (n == 2)
                                ch = ":";

                            if (Focus)
                            {
                                if (n == cursor)
                                {
                                    if (blinkCursor)
                                        rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.UnderLine;

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
