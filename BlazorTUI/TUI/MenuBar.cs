using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class MenuBar
    {
        internal List<Menu> menus;
        public IReadOnlyList<Menu> Menus => menus;
        internal bool visible;
        public bool IsVisible { get => visible; set => visible = value; }
        internal Color foreColor;
        public Color ForeColor { get => foreColor; set => foreColor = value; }
        internal Color backgroundColor;
        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        private Screen screen;
        internal bool showShortCutkeys;
        public bool ShowShortcutKeys { get => showShortCutkeys; set => showShortCutkeys = value; }

        public MenuBar(Color foreColor, Color backgroundColor, Screen screen)
        {
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

            if (mnuOpen == null && menus.Count > 0)
            {
                switch (key)
                {
                    case "ArrowLeft":
                    case "ArrowRight":
                    case "ArrowDown":
                    case "ArrowUp":
                        mnuOpen = menus[0];
                        mnuOpen.opended = true;
                        showShortCutkeys = true;
                        break;
                }
            }

            if (mnuOpen == null)
            {
                foreach (Menu menu in menus)
                {
                    if (menu.shortCutKey != null && char.ToUpperInvariant(key[0]) == char.ToUpperInvariant(menu.shortCutKey.Value))
                        menu.opended = true;
                }
            }
            else if (mnuOpen.menuItems != null)
            {
                showShortCutkeys = true;
                IReadOnlyList<MenuItem> visibleItems = mnuOpen.VisibleItems();

                switch (key)
                {
                    case "ArrowLeft":
                        for (int i = 1; i < menus.Count; i++)
                        {
                            if (menus[i].text == mnuOpen.text)
                            {
                                menus[i].opended = false;
                                menus[i].selectedItem = 0;
                                menus[i - 1].opended = true;
                                break;
                            }
                            else
                            {
                                menus[i].opended = false;
                            }
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
                        if (mnuOpen.selectedItem < visibleItems.Count)
                            mnuOpen.selectedItem++;
                        break;
                    case "ArrowUp":
                        if (mnuOpen.selectedItem > 0)
                            mnuOpen.selectedItem--;
                        break;
                    case "Enter":
                        if (mnuOpen.selectedItem > 0)
                        {
                            visibleItems[mnuOpen.selectedItem - 1].Invoke();

                            mnuOpen.selectedItem = 0;
                            mnuOpen.opended = false;
                            showShortCutkeys = false;
                        }
                        break;
                    default:
                        foreach (MenuItem menuItem in visibleItems)
                        {
                            if (menuItem.Enabled &&
                                menuItem.shortCutKey != null &&
                                char.ToUpperInvariant(key[0]) == char.ToUpperInvariant(menuItem.shortCutKey.Value))
                            {
                                menuItem.Invoke();

                                mnuOpen.opended = false;
                                showShortCutkeys = false;
                            }
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
                    int menuWidth = TuiText.VisualWidth(menu.text);
                    if (X >= menuStartX && X < menuStartX + menuWidth)
                    {
                        CloseMenus();
                        menu.opended = true;
                        showShortCutkeys = true;
                        return true;
                    }

                    menuStartX += menuWidth;
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
                openMenuStartX += TuiText.VisualWidth(menu.text);
            }

            int itemIndex = Y - 1;
            IReadOnlyList<MenuItem> visibleItems = openMenu.VisibleItems();
            int dropDownWidth = visibleItems.Count == 0
                ? 0
                : visibleItems.Max(item => TuiText.VisualWidth(item.Text));
            bool itemClicked = itemIndex >= 0 && itemIndex < visibleItems.Count &&
                X >= openMenuStartX && X < openMenuStartX + dropDownWidth;

            CloseMenus();
            if (!itemClicked)
                return false;

            MenuItem item = visibleItems[itemIndex];
            if (item.Type != MenuItem.MenuItemType.Separator && item.Enabled)
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
            if (visible != true || rows == null || rows.Count == 0)
                return;

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
                    int menuWidth = TuiText.VisualWidth(menus[m].text);
                    if (c < menuWidth)
                    {
                        ch = TuiText.CellAt(menus[m].text, c);

                        if (showShortCutkeys &&
                            mnuOpened == null &&
                            menus[m].shortCutKey != null &&
                            ch.Length > 0 &&
                            char.ToUpperInvariant(menus[m].shortCutKey!.Value) == char.ToUpperInvariant(ch[0]))
                        {
                            td = Cell.TextDecoration.UnderLine;
                        }

                        if (menus[m].opended == true)
                        {
                            fore = backgroundColor;
                            background = foreColor;

                            if (c == 0)
                                RenderOpenMenu(rows, menus[m], x, fore, background);
                        }
                    }

                    c++;

                    if (c >= TuiText.VisualWidth(menus[m].text))
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

        private void RenderOpenMenu(IList<Row> rows, Menu menu, int startX, Color fore, Color background)
        {
            IReadOnlyList<MenuItem> visibleItems = menu.VisibleItems();
            if (visibleItems.Count == 0)
                return;

            int maxLength = visibleItems.Max(item => TuiText.VisualWidth(item.Text));
            for (int y = 1; y <= visibleItems.Count && y < rows.Count; y++)
            {
                bool shortCutKeyShowed = false;
                for (int x2 = 0; x2 < maxLength && x2 + startX < rows[y].Cells.Count; x2++)
                {
                    Cell cell = rows[y].Cells[x2 + startX];
                    if (menu.selectedItem == y)
                    {
                        cell.foreColor = fore;
                        cell.backgroundColor = background;
                    }
                    else
                    {
                        cell.foreColor = background;
                        cell.backgroundColor = fore;
                    }

                    cell.textDecoration = Cell.TextDecoration.None;
                    MenuItem menuItem = visibleItems[y - 1];
                    if (!menuItem.Enabled && menuItem.Type != MenuItem.MenuItemType.Separator)
                        cell.foreColor = Color.Gray;

                    if (menuItem.Type == MenuItem.MenuItemType.Separator)
                        cell.character = "─";
                    else
                        cell.character = TuiText.CellAt(menuItem.Text, x2);

                    if (!shortCutKeyShowed &&
                        showShortCutkeys &&
                        menuItem.shortCutKey != null &&
                        cell.character.Length > 0 &&
                        char.ToUpperInvariant(menuItem.shortCutKey!.Value) == char.ToUpperInvariant(cell.character[0]))
                    {
                        cell.textDecoration = Cell.TextDecoration.UnderLine;
                        shortCutKeyShowed = true;
                    }
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
