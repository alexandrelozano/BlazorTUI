using System.Drawing;

namespace BlazorTUI.TUI
{
    public class Frame : Container
    {
        public string title { get; set; }

        public Color foreColor { get; set; }

        public Color backgroundColor { get; set; }

        public enum BorderStyle
        {
            none,
            line,
            doubleline,
            solid
        }

        public BorderStyle borderStyle { get; set; }

        public Frame(string name, string title, short X, short Y, short width, short height, BorderStyle borderStyle, Color foreColor, Color backgroundColor) : base(name)
        {
            this.name = name;
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

                            if (c == XI || c == XI + width - 1)
                            {
                                switch (borderStyle)
                                {
                                    case BorderStyle.none:
                                        break;
                                    case BorderStyle.solid:
                                        cell.backgroundColor = foreColor;
                                        cell.character = " ";
                                        break;
                                    case BorderStyle.line:
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
                                    case BorderStyle.doubleline:
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
                                    case BorderStyle.none:
                                        break;
                                    case BorderStyle.solid:
                                        cell.backgroundColor = foreColor;
                                        cell.character = " ";
                                        break;
                                    case BorderStyle.line:
                                        cell.character = "─";
                                        break;
                                    case BorderStyle.doubleline:
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
                for (int c = 0; c < title.Length; c++)
                {
                    Cell cell = rows[YI].Cells[XI + c + ((width / 2) - (title.Length / 2))];
                    cell.character = title.Substring(c);

                    if (borderStyle == BorderStyle.solid)
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