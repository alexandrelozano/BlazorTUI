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
        public bool showShortCutkeys;

        public MenuBar(Color foreColor, Color backgroundColor, Screen screen) {

            menus = new List<Menu>();
            visible = true;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
            this.screen = screen;
            this.showShortCutkeys = false;
        }

        public bool KeyDown(string key, bool shiftKey)
        {
            bool handled = true;

            Menu mnuOpen = OpenedMenu();

            if (mnuOpen == null)
            {
                switch (key)
                {
                    case "ArrowLeft":
                    case "ArrowRight":
                    case "ArrowDown":
                    case "ArrowUp":
                        mnuOpen = this.menus[0];
                        mnuOpen.opended = true;
                        showShortCutkeys = true;
                        break;
                }
            }

            if (mnuOpen == null)
            {
                foreach (Menu menu in menus)
                    if (menu.shortCutKey!=null && char.ToUpperInvariant(key[0]) == char.ToUpperInvariant(menu.shortCutKey.Value))
                        menu.opended = true;
            }
            else if (mnuOpen.menuItems != null)
            {
                showShortCutkeys = true;

                switch (key)
                {
                    case "ArrowLeft":
                        for (int i = 1; i < menus.Count;i++)
                        {
                            if (menus[i].text==mnuOpen.text)
                            {
                                menus[i].opended = false;
                                menus[i].selectedItem = 0;
                                menus[i - 1].opended = true;
                                break;
                            }
                            else
                                menus[i].opended = false;
                        }
                        break;
                    case "ArrowRight":
                        for (int i = 0; i < menus.Count; i++)
                        {
                            if (menus[i].text == mnuOpen.text && i < menus.Count - 1)
                            {
                                menus[i].opended = false;
                                menus[i].selectedItem = 0;
                                menus[i + 1].opended = true;
                                break;
                            }
                        }
                        break;
                    case "ArrowDown":
                        if (mnuOpen.selectedItem < mnuOpen.menuItems.Count)
                            mnuOpen.selectedItem++;
                        break;
                    case "ArrowUp":
                        if (mnuOpen.selectedItem > 0)
                            mnuOpen.selectedItem--;
                        break;
                    case "Enter":
                        if (mnuOpen.selectedItem > 0)
                        {
                            if (mnuOpen.menuItems[mnuOpen.selectedItem - 1].OnClick != null)
                                mnuOpen.menuItems[mnuOpen.selectedItem - 1].OnClick.Invoke();

                            mnuOpen.selectedItem = 0;
                            mnuOpen.opended = false;
                            showShortCutkeys = false;
                        }
                        break;
                    default:
                        foreach (MenuItem menuItem in mnuOpen.menuItems)
                            if (menuItem.shortCutKey != null && char.ToUpperInvariant(key[0]) == char.ToUpperInvariant(menuItem.shortCutKey.Value))
                            {
                                if (menuItem.OnClick != null)
                                    menuItem.OnClick.Invoke();

                                mnuOpen.opended = false;
                                showShortCutkeys = false;
                            }
                        break;
                }
            }

            return handled;
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
                            showShortCutkeys = true;

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
                                    menus[m].opended = false;
                                    if (c == 0)
                                    {
                                        int maxLenght = (from p in menus[m].menuItems select p.text.Length).Max();
                                        for (int y = 1; y <= menus[m].menuItems.Count; y++)
                                        {
                                            if (Y == (y + 1))
                                            {
                                                if (menus[m].menuItems[y].OnClick != null)
                                                    menus[m].menuItems[y].OnClick.Invoke();

                                                handled = true;
                                                break;
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
                Menu mnuOpened = OpenedMenu();

                int c = 0;
                int m = 0;
                for (int x = 0; x < rows[0].Cells.Count; x++)
                {
                    Color fore = foreColor;
                    Color background = backgroundColor;
                    Cell.TextDecoration td = Cell.TextDecoration.None;

                    string ch = " ";

                    if (m < menus.Count)
                    {
                        if (c < menus[m].text.Length)
                        {
                            ch = menus[m].text.Substring(c,1);  
                            
                            if (showShortCutkeys && mnuOpened == null && menus[m].shortCutKey != null && char.ToUpperInvariant(menus[m].shortCutKey!.Value) == char.ToUpperInvariant(ch[0]))
                                    td = Cell.TextDecoration.UnderLine;

                            if (menus[m].opended == true)
                            {
                                fore = backgroundColor;
                                background = foreColor;

                                if (c == 0)
                                {
                                    int maxLenght = (from p in menus[m].menuItems select p.text.Length).Max();
                                    for (int y = 1; y <= menus[m].menuItems.Count; y++)
                                    {
                                        bool shortCutKeyShowed = false;
                                        for (int x2 = 0; x2 < maxLenght; x2++)
                                        {
                                            if (menus[m].selectedItem == y)
                                            {
                                                rows[y].Cells[x2 + x].foreColor = fore;
                                                rows[y].Cells[x2 + x].backgroundColor = background;
                                            }
                                            else
                                            {
                                                rows[y].Cells[x2 + x].foreColor = background;
                                                rows[y].Cells[x2 + x].backgroundColor = fore;
                                            }

                                            rows[y].Cells[x2 + x].textDecoration = Cell.TextDecoration.None;

                                            if (menus[m].menuItems[y - 1].menuItemType == MenuItem.MenuItemType.Separator)
                                                rows[y].Cells[x2 + x].character = "─";
                                            else if (x2 < menus[m].menuItems[y - 1].text.Length)
                                                rows[y].Cells[x2 + x].character = menus[m].menuItems[y - 1].text.Substring(x2, 1);
                                            else
                                                rows[y].Cells[x2 + x].character = " ";

                                            if (!shortCutKeyShowed && showShortCutkeys && menus[m].menuItems[y - 1].shortCutKey != null && char.ToUpperInvariant(menus[m].menuItems[y - 1].shortCutKey!.Value) == char.ToUpperInvariant(rows[y].Cells[x2 + x].character[0]))
                                            {
                                                rows[y].Cells[x2 + x].textDecoration = Cell.TextDecoration.UnderLine;
                                                shortCutKeyShowed = true;
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

                    rows[0].Cells[x].foreColor = fore;
                    rows[0].Cells[x].backgroundColor = background;
                    rows[0].Cells[x].character = ch;
                    rows[0].Cells[x].textDecoration = td;
                }
            }
        }

        public Menu OpenedMenu()
        {
            Menu mnuOpen = null;
            foreach (Menu menu in menus)
                if (menu.opended)
                    mnuOpen = menu;

            return mnuOpen;
        }
    }
}