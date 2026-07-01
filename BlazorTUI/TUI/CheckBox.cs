using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class CheckBox : Control
    {
        string text;
        public string Text { get => text[4..]; set => text = $"    {value ?? ""}"; }
        internal bool value { get; set; }

        public bool Value { get => value; set => this.value = value; }

        protected override object? GetValidationValue() => value;

        public CheckBox(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor, bool value = false)
        {
            this.Name = name;
            this.X = X;
            this.Y = Y;
            this.height = 1;

            if (width < 3)
            {
                this.width = 3;
            }
            else
            {
                this.width = width;
            }

            this.text = $"    {text}";
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;
            this.Focus = false;
            this.TabStop = true;
            this.value = value; 
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                value = !value;
                container.TopContainer().SetFocus(name);
                handled = true; 
            }

            if (handled)
                NotifyClicked();

            return handled; 
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            bool handled = false;

            if (Visible)
            {
                switch (key)
                {
                    case "Tab":
                        break;
                    case "Enter":
                        value = !value;
                        container.TopContainer().SetFocus(name);
                        NotifyClicked();
                        handled = true;
                        break;
                    case "Space":
                    case " ":
                        value = !value;
                        container.TopContainer().SetFocus(name);
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
            if (Visible)
            {
                for (short n = 0; n < width; n++)
                {
                    if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                    {
                        if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                        {
                            string ch = TuiText.CellAt(text, n);

                            if (Focus)
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = backgroundColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = foreColor;
                            }
                            else
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = foreColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;
                            }

                            if (n == 0)
                            {
                                ch = "[";
                            }
                            else if (n == 1)
                            {
                                if (value)
                                {
                                    ch = "X";
                                }
                                else
                                {
                                    ch = " ";
                                }
                            }
                            else if (n == 2)
                            {
                                ch = "]";
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }
    }
}
