using System.Drawing;

namespace BlazorTUI.TUI
{
    public class Dialog : Container
    {
        public string title { get; set; }

        public Color foreColor { get; set; }

        public Color backgroundColor { get; set; }

        private Screen screen { get; set; }

        public BorderStyle borderStyle { get; set; }

        public Dialog(string name, string title, short width, short height, BorderStyle borderStyle, Color foreColor, Color backgroundColor, Screen screen) : base(name)
        {
            this.name = name;
            this.title = title;
            this.width = width;
            this.height = height;
            this.borderStyle = borderStyle;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
            this.screen = screen;
            this.Visible = false;
        }

        public void Show()
        {
            Visible = true;
            screen.dialogs.Add(this);
        }

        public void Close()
        {
            Visible = false;
            screen.dialogs.Remove(this);
        }

        public override void Render(IList<Row> rows)
        {
            if (X == 0 && Y == 0)
            {
                X = (short)((screen.width / 2) - (width / 2));
                Y = (short)((screen.height / 2) - (height / 2));
            }

            short XI = X;
            short YI = Y;

            for (short r = YI; r < YI + height; r++)
            {
                for (short c = XI; c < XI + width; c++)
                {
                    Cell cell = rows[r].Cells[c];

                    cell.foreColor = foreColor;
                    cell.backgroundColor = backgroundColor;
                    cell.backgroundImage = "";
                    cell.scaleX = 1;
                    cell.scaleY = 1;

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
                    else
                    {
                        cell.character = " ";
                    }
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                for (int c = 0; c < title.Length; c++)
                {
                    Cell cell = rows[YI].Cells[XI + c + ((width / 2) - (title.Length / 2))];
                    cell.character = title.Substring(c, 1);

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
