using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class RadioButton : Control
    {
        string text;
        Action OnClick;
        bool value { get; set; }

        public RadioButton(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor, Action OnClick, bool value)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;

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
            this.OnClick = OnClick;

            this.Focus = false;
            this.TabStop = true;
            this.value = value;
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                Check();
                container.TopContainer().SetFocus(name);
                OnClick.Invoke();
                handled = true;
            }

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
                        Check();
                        container.TopContainer().SetFocus(name);
                        handled = true;
                        break;
                    case " ":
                        Check();
                        container.TopContainer().SetFocus(name);
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

        private void Check()
        {
            value = true;

            foreach (Control control in this.container.controls)
            {
                if (control is RadioButton && control.name != this.name)
                {
                    RadioButton rb = (RadioButton)control;
                    rb.value = false;
                }
            }
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
                            string ch = (n < text.Length) ? text.Substring(n, 1) : " ";

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
                                ch = "(";
                            }
                            else if (n == 1)
                            {
                                if (value)
                                {
                                    ch = "●";
                                }
                                else
                                {
                                    ch = " ";
                                }
                            }
                            else if (n == 2)
                            {
                                ch = ")";
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }
    }
}