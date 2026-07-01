using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Frame : Container
    {
        internal string title { get; set; }

        public string Title { get => title; set => title = value ?? ""; }

        internal Color foreColor { get; set; }

        public Color ForeColor { get => foreColor; set => foreColor = value; }

        internal Color backgroundColor { get; set; }

        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        public enum BorderStyle
        {
            None,
            Line,
            DoubleLine,
            Solid
        }

        internal BorderStyle borderStyle { get; set; }

        public BorderStyle Border { get => borderStyle; set => borderStyle = value; }

        public Frame(string name, string title, short X, short Y, short width, short height, BorderStyle borderStyle, Color foreColor, Color backgroundColor) : base(name)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            this.Name = name;
            this.title = title; 
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            this.borderStyle = borderStyle;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
        }

        public override void Render(IList<Row> rows)
        {
            short YI = (parent != null) ? (short)(parent.YOffset() + Y): Y;
            short XI = (parent != null) ? (short)(parent.XOffset() + X) : X;

            for (short r = YI; r < YI + height; r++)
            {
                if (this.parent == null || r < this.parent.Y + this.parent.height)
                {
                    for (short c = XI; c < XI + width; c++)
                    {
                        Cell cell = rows[r].Cells[c];
                        
                        if (this.parent == null || c < this.parent.X + this.parent.width)
                        {
                            cell.foreColor = foreColor;
                            cell.backgroundColor = backgroundColor;
                            cell.character = " ";
                            cell.textDecoration = Cell.TextDecoration.None;

                            if (c == XI || c == XI + width - 1)
                            {
                                switch (borderStyle)
                                {
                                    case BorderStyle.None:
                                        break;
                                    case BorderStyle.Solid:
                                        cell.backgroundColor = foreColor;
                                        cell.character = " ";
                                        break;
                                    case BorderStyle.Line:
                                        if (r == YI)
                                        {
                                            if (c == XI) 
                                                cell.character = "┌";
                                            else
                                                cell.character = "┐";
                                        }
                                        else if (r == YI + height - 1)
                                        {
                                            if (c == XI)
                                                cell.character = "└";
                                            else
                                                cell.character = "┘";
                                        }
                                        else
                                        {
                                            cell.character = "│";
                                        }
                                        break;
                                    case BorderStyle.DoubleLine:
                                        if (r == YI)
                                        {
                                            if (c == XI)
                                                cell.character = "╔";
                                            else
                                                cell.character = "╗";
                                        }
                                        else if (r == YI + height - 1)
                                        {
                                            if (c == XI)
                                                cell.character = "╚";
                                            else
                                                cell.character = "╝";
                                        }
                                        else
                                        {
                                            cell.character = "║";
                                        }
                                        break;
                                }
                            }
                            else if (r == YI || r == YI + height - 1)
                            {
                                switch (borderStyle)
                                {
                                    case BorderStyle.None:
                                        break;
                                    case BorderStyle.Solid:
                                        cell.backgroundColor = foreColor;
                                        cell.character = " ";
                                        break;
                                    case BorderStyle.Line:
                                        cell.character = "─";
                                        break;
                                    case BorderStyle.DoubleLine:
                                        cell.character = "═";
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                int titleWidth = Math.Min(TuiText.VisualWidth(title), width);
                int titleStart = XI + ((width / 2) - (titleWidth / 2));
                for (int c = 0; c < titleWidth; c++)
                {
                    Cell cell = rows[YI].Cells[titleStart + c];
                    cell.character = TuiText.CellAt(title, c);

                    if (borderStyle == BorderStyle.Solid)
                    {
                        cell.backgroundColor = foreColor;
                        cell.foreColor = backgroundColor;
                    }
                }
            }

            base.Render(rows);
        }
    }
}
