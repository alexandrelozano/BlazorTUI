using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorTUI.TUI
{
    public class ListBox : Control
    {
        List<string> items = new List<string>();
        List<string> itemsSelected = new List<string>();

        private bool multipleSelection;

        private short cursorY;
        private short scrollY;

        public ListBox(string name, List<string> items, bool multipleSelection, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;

            this.width = width;
            this.height = height;

            this.items = items;
            this.multipleSelection = multipleSelection;
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            scrollY = 0;
            cursorY = 0;

            this.Focus = false;
            this.TabStop = true;
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
                        container.TopContainer().SetFocus(name);
                        handled = true;
                        break;
                    case " ":
                        if (multipleSelection)
                            SelectItem(items[cursorY]);
                        container.TopContainer().SetFocus(name);
                        handled = true;
                        break;
                    case "Backspace":
                        break;
                    case "ArrowUp":
                        if (cursorY > 0)
                        {
                            cursorY--;

                            if (cursorY < scrollY)
                                Click((short)(width - 1), 0);

                            if (!multipleSelection)
                                SelectItem(items[cursorY]);
                        }
                        break;
                    case "ArrowDown":
                        if (cursorY < items.Count() - 1)
                        {
                            cursorY++;

                            if (cursorY >= height)
                                Click((short)(width - 1), (short)(height - 1));

                            if (!multipleSelection)
                                SelectItem(items[cursorY]);
                        }
                        break;
                    default:
                        break;
                }
            }

            return handled;
        }

        private void SelectItem(string item)
        {
            if (multipleSelection)
            {
                if (itemsSelected.Contains(item))
                    itemsSelected.Remove(item);
                else
                    itemsSelected.Add(item);
            }
            else
            {
                itemsSelected.Clear();
                itemsSelected.Add(item);
            }
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
                    else if (Y == height -1)
                    {
                        if (scrollY < items.Count - height)
                            scrollY++;
                    }
                }
                else
                {
                    string item = items[Y + scrollY];

                    cursorY = (short)(Y + scrollY);
                    SelectItem(item);
                }       
                
                container.TopContainer().SetFocus(name);
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
                    for (short n = 0; n < width; n++)
                    {
                        if (container.YOffset() + Y + Yn < container.YOffset() + container.height && container.YOffset() + Y + Yn < rows.Count)
                        {
                            if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                            {
                                string item = "";
                                if ((Yn + scrollY) < items.Count) 
                                    item = items[Yn + scrollY];

                                string ch = " ";

                                Color foreColorTmp = foreColor;
                                Color backgroundColorTmp = backgroundColor;

                                if (n == 0)
                                {
                                    if (itemsSelected.Contains(item))
                                    {
                                        ch = "●";
                                    }

                                    if (Focus && (Yn + scrollY) == cursorY)
                                    {
                                        foreColorTmp = backgroundColor;
                                        backgroundColorTmp = foreColor;
                                    }
                                }
                                else if (n == width - 1)
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
                                    if ((n - 1) < item.Length)
                                        ch = item.Substring((n - 1), 1);

                                    if (Focus && (Yn + scrollY) == cursorY)
                                    {
                                        foreColorTmp = backgroundColor;
                                        backgroundColorTmp = foreColor;
                                    }
                                }

                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].foreColor = foreColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].backgroundColor = backgroundColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].character = ch;
                            }
                        }
                    }
                }
            }
        }
    }
}
