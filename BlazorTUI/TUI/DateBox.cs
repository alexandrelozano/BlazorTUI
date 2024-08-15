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

                        if (cursor > 0)
                        {
                            text = text.Remove(cursor - 1, 1);
                            cursor--;
                        }

                        switch (this.dateFormat)
                        {
                            case DateFormat.DDMMYYYY:
                            case DateFormat.MMDDYYYY:
                                if (cursor == 2 || cursor == 5)
                                {
                                    text = text.Remove(cursor - 1, 1);
                                    cursor--;
                                }
                                break;
                            case DateFormat.YYYYMMDD:
                                if (cursor == 4 || cursor == 7)
                                {
                                    text = text.Remove(cursor - 1, 1);
                                    cursor--;
                                }
                                break;
                        }

                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursor < (short)text.Length)
                            cursor++;

                        switch (this.dateFormat)
                        {
                            case DateFormat.DDMMYYYY:
                            case DateFormat.MMDDYYYY:
                                if (cursor == 2 || cursor == 5)
                                    cursor++;
                                break;
                            case DateFormat.YYYYMMDD:
                                if (cursor == 4 || cursor == 7)
                                    cursor++;
                                break;
                        }
                        break;
                    case "ArrowLeft":
                        if (cursor > 0)
                            cursor--;

                        switch (this.dateFormat)
                        {
                            case DateFormat.DDMMYYYY:
                            case DateFormat.MMDDYYYY:
                                if (cursor == 2 || cursor == 5)
                                    cursor--;
                                break;
                            case DateFormat.YYYYMMDD:
                                if (cursor == 4 || cursor == 7)
                                    cursor--;
                                break;
                        }

                        break;
                    default:
                        int n;
                        if (int.TryParse(key, out n))
                        {
                            if (cursor != text.Length)
                            {
                                text = text.Insert(cursor, key);
                                text = text.Remove(cursor + 1, 1);
                                cursor++;
                            }
                            else if (text.Length < width - 1)
                            {
                                text += key;
                                cursor++;
                            }

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

                switch (this.dateFormat)
                {
                    case DateFormat.DDMMYYYY:
                    case DateFormat.MMDDYYYY:
                        if (cursor == 2 || cursor == 5)
                        {
                            cursor++;
                        }
                        break;
                    case DateFormat.YYYYMMDD:
                        if (cursor == 4 || cursor == 7)
                        {
                            cursor++;
                        }
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
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.None;

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
