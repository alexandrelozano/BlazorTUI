using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class Menu
    {
        public string text;
        public string Text { get => text.TrimEnd('│'); set => text = $"{value ?? ""}│"; }

        public List<MenuItem> menuItems;
        public IReadOnlyList<MenuItem> Items => menuItems;

        public bool opended;
        public bool IsOpen { get => opended; set => opended = value; }

        public char? shortCutKey = null;
        public char? ShortcutKey { get => shortCutKey; set => shortCutKey = value; }

        public int selectedItem;
        public int SelectedIndex { get => selectedItem; set => selectedItem = value; }

        public Menu(string text, char? shortCutKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            this.text = $"{text}│";
            this.shortCutKey = shortCutKey;

            opended = false;
            menuItems = new List<MenuItem>();

            selectedItem = 0;
        }

        public void AddItem(MenuItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            menuItems.Add(item);
        }

        public MenuItem AddCommand(TuiCommand command, char? shortcutKey = null)
        {
            ArgumentNullException.ThrowIfNull(command);
            var item = new MenuItem(command, MenuItem.MenuItemType.Item, shortcutKey);
            AddItem(item);
            return item;
        }

        internal IReadOnlyList<MenuItem> VisibleItems()
            => menuItems.Where(item => item.Visible).ToList();
    }
}
