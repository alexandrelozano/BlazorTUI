using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Button : Control
    {
        string text;

        public Button(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
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

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
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
                    case " ":
                        OnClick.Invoke();
                        handled = true;
                        break;
                    case "Enter":
                        OnClick.Invoke();
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
                if (name == "bttSubmitDlg")
                {
                    string a = "a";
                }


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
    }
}