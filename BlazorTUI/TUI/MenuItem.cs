using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class MenuItem
    {
        private TuiCommand? command;
        private bool enabled = true;
        private bool visible = true;

        internal string text;
        public string Text
        {
            get => command?.Label ?? text;
            set
            {
                if (command is not null)
                    command.Label = value;
                else
                    text = value ?? "";
            }
        }

        public TuiCommand? Command => command;

        public bool Enabled
        {
            get => command?.Enabled ?? enabled;
            set
            {
                if (command is not null)
                    command.Enabled = value;
                else
                    enabled = value;
            }
        }

        public bool Visible
        {
            get => command?.Visible ?? visible;
            set
            {
                if (command is not null)
                    command.Visible = value;
                else
                    visible = value;
            }
        }

        internal char? shortCutKey = null;
        public char? ShortcutKey { get => shortCutKey; set => shortCutKey = value; }

        public enum MenuItemType
        {
            Item,
            Separator
        }

        internal MenuItemType menuItemType;
        public MenuItemType Type { get => menuItemType; set => menuItemType = value; }

        public event EventHandler? Clicked;

        public MenuItem(string text, MenuItemType menuItemType, char? shortCutKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            this.text = text;
            this.menuItemType = menuItemType;
            this.shortCutKey = shortCutKey;
        }

        public MenuItem(TuiCommand command, MenuItemType menuItemType = MenuItemType.Item, char? shortCutKey = null)
        {
            ArgumentNullException.ThrowIfNull(command);

            this.command = command;
            text = command.Label;
            this.menuItemType = menuItemType;
            this.shortCutKey = shortCutKey;
        }

        public void BindCommand(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            this.command = command;
            text = command.Label;
        }

        internal bool Invoke()
        {
            if (!Enabled || !Visible || Type == MenuItemType.Separator)
                return false;

            if (command is not null && !command.Execute())
                return false;

            Clicked?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
