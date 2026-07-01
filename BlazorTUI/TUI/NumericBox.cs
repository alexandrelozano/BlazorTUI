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
        public new Double? value;

        public new double? Value { get => value; set => this.value = value; }

        short integerPlaces;
        short decimalPlaces;
        char separator;

        public short IntegerPlaces { get => integerPlaces; set => integerPlaces = value; }

        public short DecimalPlaces { get => decimalPlaces; set => decimalPlaces = value; }

        public char DecimalSeparator { get => separator; set => separator = value; }

        public bool UseCultureDecimalSeparator { get; set; }

        public NumericBox(
            string name,
            Double? value,
            short integerPlaces,
            short decimalPlaces,
            short X,
            short Y,
            Color forecolor,
            Color backgroundcolor,
            TuiCultureOptions? cultureOptions = null)
            : this(
                name,
                value,
                integerPlaces,
                decimalPlaces,
                ResolveDecimalSeparator(cultureOptions),
                X,
                Y,
                forecolor,
                backgroundcolor,
                cultureOptions)
        {
            UseCultureDecimalSeparator = true;
        }

        public NumericBox(
            string name,
            Double? value,
            short integerPlaces,
            short decimalPlaces,
            char separator,
            short X,
            short Y,
            Color forecolor,
            Color backgroundcolor,
            TuiCultureOptions? cultureOptions = null) 
            : base(name, "", X, Y, 1, forecolor, backgroundcolor)
        {
            this.value = value;
            this.integerPlaces = integerPlaces;
            this.decimalPlaces = decimalPlaces;
            this.separator = separator;
            if (cultureOptions is not null)
                CultureOptions = cultureOptions;

            if (decimalPlaces > 0)
                this.width = (short)(integerPlaces + decimalPlaces + 2);
            else
                this.width = (short)(integerPlaces + 1);

            if (this.value != null)
            {
                string format = $"{new string('0', integerPlaces)}.{new string('0', decimalPlaces)}";
                text = this.value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture).Replace('.', EffectiveSeparator);
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
                            if (cursor == (short)(integerPlaces + 1) && decimalPlaces > 0)
                                cursor--;

                            text = text.Remove(cursor - 1);
                            cursor--;
                        }

                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursor < (short)text.Length)
                            cursor++;
                        if (cursor == integerPlaces && decimalPlaces > 0)
                            cursor++;
                        break;
                    case "ArrowLeft":
                        if (cursor > 0)
                            cursor--;
                        if (cursor == integerPlaces && decimalPlaces > 0)
                            cursor--;
                        break;
                    default:
                        int n;
                        if (int.TryParse(key, out n))
                        {
                            if (cursor < (short)(integerPlaces + decimalPlaces + 1) && cursor != integerPlaces)
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

                if (text.Length == integerPlaces && decimalPlaces > 0)
                {
                    text += EffectiveSeparator;
                    cursor++;
                }

                if (text.Length == integerPlaces + decimalPlaces + 1)
                {
                    this.value = double.Parse(text.Replace(EffectiveSeparator, '.'), CultureInfo.InvariantCulture);
                }

                if (cursor == integerPlaces && decimalPlaces > 0)
                {
                    cursor++;
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

                            if (n == integerPlaces && decimalPlaces > 0)
                                ch = EffectiveSeparator.ToString();

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

        private char EffectiveSeparator => UseCultureDecimalSeparator ? CultureOptions.DecimalSeparator : separator;

        private static char ResolveDecimalSeparator(TuiCultureOptions? cultureOptions)
            => (cultureOptions ?? TuiCultureOptions.Current).DecimalSeparator;
    }
}
