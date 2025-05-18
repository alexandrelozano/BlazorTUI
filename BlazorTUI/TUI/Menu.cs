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

        public List<MenuItem> menuItems;

        public bool opended;

        public char? shortCutKey = null;

        public int selectedItem;

        public Menu(string text, char? shortCutKey = null)
        {
            this.text = $"{text}│";
            this.shortCutKey = shortCutKey;

            opended = false;
            menuItems = new List<MenuItem>();

            selectedItem = 0;
        }
    }
}
