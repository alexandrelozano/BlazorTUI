using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Button : Control
    {
        private string text;
        private TuiCommand? command;
        private bool enabled = true;

        public string Text
        {
            get => command?.Label ?? text.Trim();
            set
            {
                if (command is not null)
                    command.Label = value;

                text = (value ?? "").CenterString(width);
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

        public Button(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor)
        {
            this.Name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = 1;
            this.text = text.CenterString(width);
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            this.Focus = false;
            this.TabStop = true;
        }

        public Button(string name, TuiCommand command, short X, short Y, short width, Color forecolor, Color backgroundcolor)
            : this(name, command?.Label ?? "", X, Y, width, forecolor, backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(command);
            BindCommand(command);
        }

        public void BindCommand(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            this.command = command;
            text = command.Label.CenterString(width);
            if (string.IsNullOrWhiteSpace(ScreenReaderDescription) &&
                !string.IsNullOrWhiteSpace(command.Description))
            {
                ScreenReaderDescription = command.Description;
            }
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (IsVisibleByCommand && Enabled)
            {
                container.TopContainer().SetFocus(name);
                command?.Execute();
                handled = true;
            }

            if (handled)
                NotifyClicked();

            return handled;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            bool handled = false;

            if (IsVisibleByCommand && Enabled)
            {
                switch (key)
                {
                    case "Tab":
                        break;
                    case "Space":
                    case " ":
                        command?.Execute();
                        NotifyClicked();
                        handled = true;
                        break;
                    case "Enter":
                        command?.Execute();
                        NotifyClicked();
                        handled = true; 
                        break;
                    case "Backspace":
                        break;
                    case "ArrowRight":
                        break;
                    case "ArrowLeft":
                        break;
                    default:
                        break;
                }
            }

            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (IsVisibleByCommand)
            {
                string renderedText = Text.CenterString(width);
                for (short n = 0; n < width; n++)
                {
                    if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                    {
                        if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                        {
                            string ch = TuiText.CellAt(renderedText, n);
                            Color foreground = Enabled ? foreColor : Color.Gray;

                            if (Focus)
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = backgroundColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = foreground;
                            }
                            else
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = foreground;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;
                            }

                            if (n == 0)
                            {
                                ch = "[";
                            }
                            else if (n == width - 1)
                            {
                                ch = "]";
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }

        private bool IsVisibleByCommand => Visible && (command?.Visible ?? true);
    }
}
