using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorTUI.TUI
{
    public class GridView : Control
    {
        public class GridRow
        {
            public string[] cells;
        }

        public class GridColumn
        {
            public string title;
            public short width;
        }

        private GridColumn[] columns;
        private GridRow[] gridrows;

        private short cursorY;
        private short scrollY;

        private string titleRow;

        public GridView(string name, GridColumn[] columns, GridRow[] gridrows, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;   
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;
            this.columns = columns;
            this.gridrows = gridrows;
            this.TabStop = true;

            scrollY = 0;
            cursorY = 0;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < columns.Length; i++)
            {
                string tmp = columns[i].title.PadRight(columns[i].width).Substring(0, columns[i].width);
                sb.Append(tmp);
                sb[sb.Length - 1] = '|';
            }
            titleRow = sb.ToString();   
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
                    case "ArrowUp":
                        if (cursorY > 2)
                        {
                            cursorY--;

                            if (cursorY < scrollY + 2)
                                Click((short)(width - 1), 0);
                        }
                        break;
                    case "ArrowDown":
                        if (cursorY < gridrows.Count() + 1)
                        {
                            cursorY++;

                            if (cursorY >= height)
                                Click((short)(width - 1), (short)(height - 1));
                        }
                        break;
                    default:
                        break;
                }
            }

            return handled;
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                if (X == width - 1)
                {
                    if (Y == 0)
                    {
                        if (scrollY > 0)
                            scrollY--;
                    }
                    else if (Y == height - 1)
                    {
                        if (scrollY < gridrows.Length - height + 2)
                            scrollY++;
                    }
                }
                else
                {
                    cursorY = (short)(Y + scrollY);
                }

                Container c = container.TopContainer();
                c.SetFocus(name);
                handled = true;
            }

            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                for (short Yn = 0; Yn < height; Yn++)
                {
                    StringBuilder sbRow = new StringBuilder();

                    if ((Yn + scrollY - 2) >= 0 && (Yn + scrollY - 2) < gridrows.Count())
                    { 
                        for (int i = 0; i < columns.Length; i++)
                        {
                            string tmp = gridrows[Yn + scrollY - 2].cells[i].PadRight(columns[i].width).Substring(0,columns[i].width);
                            sbRow.Append(tmp);
                            sbRow[sbRow.Length - 1] = '│';
                        }
                    }
                    
                    for (short Xn = 0; Xn < width; Xn++)
                    {
                        if (container.YOffset() + Y + Yn < container.YOffset() + container.height && container.YOffset() + Y + Yn < rows.Count)
                        {
                            if (container.XOffset() + X + Xn < container.XOffset() + container.width && container.XOffset() + X + Xn < rows[Y].Cells.Count)
                            {
                                string ch = " ";

                                Color foreColorTmp = foreColor;
                                Color backgroundColorTmp = backgroundColor;

                                if (Xn == width - 1)
                                {
                                    if (Yn == 0)
                                        ch = "↑";
                                    else if (Yn == height - 1)
                                        ch = "↓";
                                    else
                                        ch = "│";
                                }
                                else
                                {
                                    if (Yn == 0)
                                    {
                                        if (Xn < titleRow.Length)
                                            ch = titleRow.Substring(Xn, 1);
                                    }else if (Yn == 1)
                                    {
                                        ch = "─";
                                    }
                                    else
                                    {
                                        if (Xn < sbRow.Length)
                                            ch = sbRow[Xn].ToString();

                                        if (Yn + scrollY == cursorY)
                                        {
                                            foreColorTmp = backgroundColor;
                                            backgroundColorTmp = foreColor;
                                        }
                                    }
                                }

                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + Xn].foreColor = foreColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + Xn].backgroundColor = backgroundColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + Xn].character = ch;
                            }
                        }
                    }
                }
            }
        }
    }
}
