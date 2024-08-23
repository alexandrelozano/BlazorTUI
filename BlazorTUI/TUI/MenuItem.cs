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

        public Action OnClick;

        public MenuItem(string text)
        {
            this.text = text;
        }
    }
}
