using System.Drawing;

namespace BlazorTUI.TUI
{
    public class TextBox : Control
    {
        internal string text;

        internal bool blinkCursor;

        public TextBox(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = 1;
            this.text = text;
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
                        break;
                    case "Backspace":
                        if (!string.IsNullOrEmpty(text) && text.Length > 0)
                            text = text.Remove(text.Length - 1, 1);
                        handled = true;
                        break;
                    case "ArrowRight":
                        break;
                    case "ArrowLeft":
                        break;
                    default:
                        if (key.Length == 1 && text.Length < width - 1)
                            text += key;
                        handled = true;
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
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = foreColor;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.None;

                            string ch = (n < text.Length) ? text.Substring(n, 1) : " ";

                            if (Focus)
                            {
                                if (n == text.Length)
                                {
                                    if (blinkCursor)
                                        rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.UnderLine;

                                    blinkCursor = !blinkCursor;
                                }
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }
    }
}