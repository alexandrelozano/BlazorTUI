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
        internal new DateOnly? value;

        public new DateOnly? Value { get => value; set => this.value = value; }

        public enum DateFormat
        {
            DDMMYYYY,
            MMDDYYYY,
            YYYYMMDD,
            CultureShortDate
        }

        internal DateFormat dateFormat { get; set; }

        public DateFormat Format { get => dateFormat; set => dateFormat = value; }

        public DateBox(
            string name,
            DateOnly? value,
            DateFormat dateFormat,
            short X,
            short Y,
            Color forecolor,
            Color backgroundcolor,
            TuiCultureOptions? cultureOptions = null)
            : base(name, "", X, Y, 11, forecolor, backgroundcolor)
        {
            this.value = value;
            this.dateFormat = dateFormat;
            if (cultureOptions is not null)
                CultureOptions = cultureOptions;

            if (this.value != null)
                text = CultureOptions.FormatDate(this.value.Value, this.dateFormat);
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
                            if (IsDateSeparatorIndex(cursor - 1))
                                cursor--;

                            text = text.Remove(cursor - 1, 1);
                            cursor--;
                        }

                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursor < (short)text.Length)
                            cursor++;

                        if (IsDateSeparatorIndex(cursor))
                            cursor++;
                        break;
                    case "ArrowLeft":
                        if (cursor > 0)
                            cursor--;

                        if (IsDateSeparatorIndex(cursor))
                            cursor--;

                        break;
                    default:
                        int n;
                        if (int.TryParse(key, out n))
                        {
                            if (IsDateSeparatorIndex(cursor))
                                cursor++;

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

                ApplyDateSeparators();

                if (IsDateSeparatorIndex(cursor))
                    cursor++;

                if (text.Length == CultureOptions.GetDateTextLength(dateFormat))
                {
                    DateOnly dt;
                    this.value = null;

                    if (CultureOptions.TryParseDate(text, dateFormat, out dt))
                        this.value = dt;
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

                            ch = GetDateSeparatorOrDefault(n, ch);

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

        protected override object? GetValidationValue() => value;

        private void ApplyDateSeparators()
        {
            string pattern = CultureOptions.GetDatePattern(dateFormat);
            string digits = new(text.Where(char.IsDigit).Take(pattern.Count(IsPatternDigit)).ToArray());
            var builder = new StringBuilder();
            int digitIndex = 0;
            foreach (char patternCharacter in pattern)
            {
                if (IsPatternDigit(patternCharacter))
                {
                    if (digitIndex >= digits.Length)
                        break;

                    builder.Append(digits[digitIndex]);
                    digitIndex++;
                }
                else if (builder.Length > 0 || digitIndex > 0)
                {
                    builder.Append(patternCharacter);
                }
            }

            text = builder.ToString();
            if (cursor > text.Length)
                cursor = (short)text.Length;
        }

        private bool IsDateSeparatorIndex(int index)
            => index >= 0 && CultureOptions.GetDateSeparatorIndexes(dateFormat).Contains(index);

        private string GetDateSeparatorOrDefault(int index, string fallback)
        {
            string pattern = CultureOptions.GetDatePattern(dateFormat);
            return index >= 0 &&
                index < pattern.Length &&
                !IsPatternDigit(pattern[index])
                    ? pattern[index].ToString()
                    : fallback;
        }

        private static bool IsPatternDigit(char value)
            => value is 'd' or 'M' or 'y';
    }
}
