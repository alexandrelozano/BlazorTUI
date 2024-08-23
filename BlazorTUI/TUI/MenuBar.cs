using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorTUI.TUI
{
    public class MenuBar
    {
        public List<Menu> menus;
        public bool visible;
        public Color foreColor;
        public Color backgroundColor;

        private Screen screen;

        public MenuBar(Color foreColor, Color backgroundColor, Screen screen) {

            menus = new List<Menu>();
            visible = true;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
            this.screen = screen;
        }

        public bool Click(short X, short Y)
        {
            bool handled = false;

            if (visible)
            {
                if (Y == 0)
                {
                    int c = 0;
                    for (int i = 0; i < menus.Count; i++) {
                        if (X >= c && X < (c + menus[i].text.Length)) {
                            menus[i].opended = true;

                            for (int j = 0; j < menus.Count; j++)
                                if (i != j)
                                    menus[j].opended = false;

                            handled = true;
                            break;
                        }
                        else
                            c += menus[i].text.Length;
                    }
                }
                else
                {
                    int c = 0;
                    int m = 0;
                    for (int x = 0; x < screen.rows[0].Cells.Count; x++)
                    {
                        if (m < menus.Count)
                        {
                            if (c < menus[m].text.Length)
                            {
                                if (menus[m].opended == true)
                                {
                                    if (c == 0)
                                    {
                                        int maxLenght = (from p in menus[m].menuItems select p.text.Length).Max();
                                        for (int y = 1; y <= menus[m].menuItems.Count; y++)
                                        {
                                            if (Y == (y + 1))
                                            {
                                                if (menus[m].menuItems[y].OnClick != null)
                                                {
                                                    menus[m].menuItems[y].OnClick.Invoke();
                                                    handled = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            c++;

                            if (c >= menus[m].text.Length)
                            {
                                c = 0;
                                m++;
                            }
                        }
                    }
                }
            }

            if (handled == false)
                for (int i = 0; i < menus.Count;i++)
                    menus[i].opended = false;

            return handled;
        }

        public void Render(IList<Row> rows)
        {
            if (visible == true && rows != null && rows.Count > 0)
            {
                int c = 0;
                int m = 0;
                for (int x = 0; x < rows[0].Cells.Count; x++)
                {
                    Color fore = foreColor;
                    Color background = backgroundColor;
                    
                    string ch = " ";

                    if (m < menus.Count)
                    {
                        if (c < menus[m].text.Length)
                        {
                            ch = menus[m].text.Substring(c,1);  
                            
                            if (menus[m].opended == true)
                            {
                                fore = backgroundColor;
                                background = foreColor;

                                if (c == 0)
                                {
                                    int maxLenght = (from p in menus[m].menuItems select p.text.Length).Max();
                                    for (int y = 1; y <= menus[m].menuItems.Count; y++)
                                    {
                                        for (int x2 = 0; x2 < maxLenght; x2++)
                                        {
                                            rows[y].Cells[x2 + x].foreColor = fore;
                                            rows[y].Cells[x2 + x].backgroundColor = background;
                                            if (x2 < menus[m].menuItems[y - 1].text.Length)
                                                rows[y].Cells[x2 + x].character = menus[m].menuItems[y - 1].text.Substring(x2, 1);
                                            else
                                                rows[y].Cells[x2 + x].character = " ";
                                        }
                                    }
                                }
                            }
                        }

                        c++;

                        if (c >= menus[m].text.Length)
                        {
                            c = 0;
                            m++;
                        }
                    }

                    rows[0].Cells[x].foreColor = fore;
                    rows[0].Cells[x].backgroundColor = background;
                    rows[0].Cells[x].character = ch;
                }
            }
        }
    }
}