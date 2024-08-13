using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class DateBox : TextBox
    {
        public DateOnly? date;

        public enum DateFormat
        {
            DDMMYYYY,
            MMDDYYYY,
            YYYYMMDD
        }

        public DateFormat dateFormat { get; set; }

        public DateBox(string name, DateOnly? date, DateFormat dateFormat, short X, short Y, Color forecolor, Color backgroundcolor) : base(name, "", X, Y, 11, forecolor, backgroundcolor)
        {
            this.date = date;
            this.dateFormat = dateFormat;

            if (this.date != null)
            {
                switch (this.dateFormat)
                {
                    case DateFormat.DDMMYYYY:
                        text = this.date.Value.ToString("DD/MM/YYYY");
                        break;
                    case DateFormat.MMDDYYYY:
                        text = this.date.Value.ToString("MM/DD/YYYY");
                        break;
                    case DateFormat.YYYYMMDD:
                        text = this.date.Value.ToString("YYYY/MM/DD");
                        break;
                }
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

                        switch (this.dateFormat)
                        {
                            case DateFormat.DDMMYYYY:
                            case DateFormat.MMDDYYYY:
                                if (text.Length == 2)
                                    text = text.Remove(text.Length - 1, 1);

                                if (text.Length == 5)
                                    text = text.Remove(text.Length - 1, 1);
                                break;
                            case DateFormat.YYYYMMDD:
                                if (text.Length == 4)
                                    text = text.Remove(text.Length - 1, 1);

                                if (text.Length == 7)
                                    text = text.Remove(text.Length - 1, 1);
                                break;
                        }

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
                            text += key;
                            handled = true;
                        }
                        break;
                }

                switch (this.dateFormat)
                {
                    case DateFormat.DDMMYYYY:
                    case DateFormat.MMDDYYYY:
                        if (text.Length == 2)
                            text += "/";

                        if (text.Length == 5)
                            text += "/";
                        break;
                    case DateFormat.YYYYMMDD:
                        if (text.Length == 4)
                            text += "/";

                        if (text.Length == 7)
                            text += "/";
                        break;
                }

                if (text.Length == 10)
                {
                    DateOnly dt;
                    this.date = null;

                    switch (this.dateFormat)
                    {
                        case DateFormat.DDMMYYYY:
                            if (DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                this.date = dt;
                            break;
                        case DateFormat.MMDDYYYY:
                            if (DateOnly.TryParseExact(text, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                this.date = dt;
                            break;
                        case DateFormat.YYYYMMDD:
                            if (DateOnly.TryParseExact(text, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                this.date = dt;
                            break;
                    }
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

                            switch (this.dateFormat)
                            {
                                case DateFormat.DDMMYYYY:
                                case DateFormat.MMDDYYYY:
                                    if (n == 2)
                                        ch = "/";

                                    if (n == 5)
                                        ch = "/";
                                    break;
                                case DateFormat.YYYYMMDD:
                                    if (n == 4)
                                        ch = "/";

                                    if (n == 7)
                                        ch = "/";
                                    break;
                            }

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
