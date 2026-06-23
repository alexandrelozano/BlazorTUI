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
        public string Text { get => text; set => text = value ?? ""; }
        public char? shortCutKey = null;
        public char? ShortcutKey { get => shortCutKey; set => shortCutKey = value; }

        public enum MenuItemType
        {
            Item,
            Separator
        }

        public MenuItemType menuItemType;
        public MenuItemType Type { get => menuItemType; set => menuItemType = value; }

        public Action? OnClick;

        public event EventHandler? Clicked;

        public MenuItem(string text, MenuItemType menuItemType, char? shortCutKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            this.text = text;
            this.menuItemType = menuItemType;
            this.shortCutKey = shortCutKey;
        }

        internal void Invoke()
        {
            OnClick?.Invoke();
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
