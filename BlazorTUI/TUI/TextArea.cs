using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class TextArea : Control
    {
        internal List<string> text;

        internal bool blinkCursor;
        internal short cursorX;
        internal short cursorY;

        public string value
        {
            get { 
                return String.Join(Environment.NewLine, text);
            }
            set {
                text = new List<string>(value.Split(Environment.NewLine));
                cursorX = 0;
                cursorY = 0;
            }
        }

        public TextArea(string name, string text, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            
            if (text != null)
                this.text = new List<string>(text.Split(Environment.NewLine));
            else
                this.text = new List<string>();

            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            this.Focus = false;
            this.TabStop = true;
            this.cursorX = 0;
            this.cursorY = 0;
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                if (Y < text.Count)
                {
                    cursorY = Y;
                    if (X < text[Y].Length)
                        cursorX = X;
                    else
                        cursorX = (short)text[Y].Length;
                }
                else
                {
                    cursorX = 0;
                    cursorY = 0;
                }

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
                        if (text.Count < height)
                        {
                            if (cursorY == text.Count - 1)
                            {
                                text.Add("");
                                cursorY++;
                                cursorX = 0;
                            }
                            else
                            {
                                text.Insert(cursorY + 1, "");
                                cursorY++;
                                cursorX = 0;
                            }
                        }
                        handled = true;
                        break;
                    case "Backspace":
                        if (cursorX > 0)
                        {
                            text[cursorY] = text[cursorY].Remove(cursorX - 1, 1);
                            cursorX--;
                        }
                        else
                        {
                            if (cursorY > 0)
                            {
                                cursorY--;
                                cursorX = (short)text[cursorY].Length;
                                if (string.IsNullOrEmpty(text[cursorY + 1]))
                                {
                                    text.RemoveAt(cursorY + 1);
                                }
                            }
                        }
                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursorX < (short)text[cursorY].Length)
                            cursorX++;
                        handled = true;
                        break;
                    case "ArrowLeft":
                        if (cursorX > 0)
                            cursorX--;
                        handled = true;
                        break;
                    case "ArrowUp":
                        if (cursorY > 0)
                        {
                            cursorY--;
                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;
                        }
                        handled = true;
                        break;
                    case "ArrowDown":
                        if (cursorY < text.Count - 1)
                        {
                            cursorY++;
                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;
                        }
                        handled = true;
                        break;
                    default:
                        if (key.Length == 1 && text[cursorY].Length < width - 1)
                        {
                            if (cursorX != text[cursorY].Length)
                                text[cursorY] = text[cursorY].Insert(cursorX, key);
                            else
                                text[cursorY] += key;

                            cursorX++;
                        }
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
                for (int h = 0; h < height; h++)
                {
                    string line = "";
                    if (text.Count > h)
                        line = text[h];

                    for (short n = 0; n < width; n++)
                    {
                        if (container.YOffset() + Y + h < container.YOffset() + container.height && container.YOffset() + Y + h < rows.Count)
                        {
                            if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                            {
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].foreColor = foreColor;
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.None;

                                string ch = (n < line.Length) ? line.Substring(n, 1) : " ";

                                if (Focus)
                                {
                                    if (h == cursorY && n == cursorX)
                                    {
                                        if (blinkCursor)
                                            rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.UnderLine;

                                        blinkCursor = !blinkCursor;
                                    }
                                }

                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].character = ch;
                            }
                        }
                    }
                }
            }
        }
    }
}
