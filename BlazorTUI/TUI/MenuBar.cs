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
        public IReadOnlyList<Menu> Menus => menus;
        public bool visible;
        public bool IsVisible { get => visible; set => visible = value; }
        public Color foreColor;
        public Color ForeColor { get => foreColor; set => foreColor = value; }
        public Color backgroundColor;
        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        private Screen screen;
        public bool showShortCutkeys;
        public bool ShowShortcutKeys { get => showShortCutkeys; set => showShortCutkeys = value; }

        public MenuBar(Color foreColor, Color backgroundColor, Screen screen) {

            ArgumentNullException.ThrowIfNull(screen);

            menus = new List<Menu>();
            visible = true;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
            this.screen = screen;
            this.showShortCutkeys = false;
        }

        public void AddMenu(Menu menu)
        {
            ArgumentNullException.ThrowIfNull(menu);
            menus.Add(menu);
        }

        public bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            bool handled = true;

            Menu? mnuOpen = OpenedMenu();

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
                            mnuOpen.menuItems[mnuOpen.selectedItem - 1].Invoke();

                            mnuOpen.selectedItem = 0;
                            mnuOpen.opended = false;
                            showShortCutkeys = false;
                        }
                        break;
                    default:
                        foreach (MenuItem menuItem in mnuOpen.menuItems)
                            if (menuItem.shortCutKey != null && char.ToUpperInvariant(key[0]) == char.ToUpperInvariant(menuItem.shortCutKey.Value))
                            {
                                menuItem.Invoke();

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
            if (!visible)
                return false;

            if (Y == 0)
            {
                int menuStartX = 0;
                foreach (Menu menu in menus)
                {
                    if (X >= menuStartX && X < menuStartX + menu.text.Length)
                    {
                        CloseMenus();
                        menu.opended = true;
                        showShortCutkeys = true;
                        return true;
                    }

                    menuStartX += menu.text.Length;
                }

                CloseMenus();
                return false;
            }

            Menu? openMenu = OpenedMenu();
            if (openMenu is null)
                return false;

            int openMenuStartX = 0;
            foreach (Menu menu in menus)
            {
                if (ReferenceEquals(menu, openMenu))
                    break;
                openMenuStartX += menu.text.Length;
            }

            int itemIndex = Y - 1;
            int dropDownWidth = openMenu.menuItems.Count == 0
                ? 0
                : openMenu.menuItems.Max(item => item.text.Length);
            bool itemClicked = itemIndex >= 0 && itemIndex < openMenu.menuItems.Count &&
                X >= openMenuStartX && X < openMenuStartX + dropDownWidth;

            CloseMenus();
            if (!itemClicked)
                return false;

            MenuItem item = openMenu.menuItems[itemIndex];
            if (item.menuItemType != MenuItem.MenuItemType.Separator)
                item.Invoke();

            return true;
        }

        private void CloseMenus()
        {
            foreach (Menu menu in menus)
            {
                menu.opended = false;
                menu.selectedItem = 0;
            }
            showShortCutkeys = false;
        }

        public void Render(IList<Row> rows)
        {
            if (visible == true && rows != null && rows.Count > 0)
            {
                Menu? mnuOpened = OpenedMenu();

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

        public Menu? OpenedMenu()
        {
            Menu? mnuOpen = null;
            foreach (Menu menu in menus)
                if (menu.opended)
                    mnuOpen = menu;

            return mnuOpen;
        }

        public Menu? OpenMenu => OpenedMenu();
    }
}
