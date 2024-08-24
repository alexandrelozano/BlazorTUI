using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class MenuItem
    {
        public string text;

        public enum MenuItemType
        {
            Item,
            Separator
        }

        public MenuItemType menuItemType;

        public Action OnClick;

        public MenuItem(string text, MenuItemType menuItemType)
        {
            this.text = text;
            this.menuItemType = menuItemType;
        }
    }
}
