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

        public Menu(string text)
        {
            this.text = $"{text}│";

            opended = false;
            menuItems = new List<MenuItem>();
        }
    }
}
