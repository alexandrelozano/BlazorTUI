﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class TextArea : Control
    {
        private List<string> text;

        public short maxLines;
        public short maxTextWidth;

        private bool blinkCursor;
        private short cursorX;
        private short cursorY;
        private short scrollY;
        private short scrollX;

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

        public TextArea(string name, string text, short X, short Y, short width, short height, short maxTextWidth, short maxLines, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            this.maxLines = maxLines;
            this.maxTextWidth = maxTextWidth;

            if (text != null)
                this.text = new List<string>(text.Split(Environment.NewLine));
            else
                this.text = new List<string>();

            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            Focus = false;
            TabStop = true;
            cursorX = 0;
            cursorY = 0;
            scrollX = 0;
            scrollY = 0;
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                if (X == width - 1)
                {
                    if (Y == 0)
                    {
                        if (scrollY > 0)
                        {
                            scrollY--;

                            if (cursorY > scrollY + height)
                                cursorY--;

                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;
                        }
                    }
                    else if (Y == height - 1)
                    {
                        if (text.Count - scrollY > 1)
                        {
                            scrollY++;

                            if (cursorY < scrollY)
                                cursorY++;

                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;
                        }
                    }
                }
                else
                {
                    if (Y == height - 1)
                    {
                        if (X == 0)
                        {
                            if (scrollX > 0)
                                scrollX--;
                        }else if (X == width - 2)
                        {
                            if (scrollX < maxTextWidth)
                                scrollX++;
                        }
                    }
                    else
                    {
                        if (Y + scrollY < text.Count)
                        {
                            cursorY = (short)(Y + scrollY);
                            if (X < text[Y + scrollY].Length)
                                cursorX = X;
                            else
                                cursorX = (short)text[Y + scrollY].Length;
                        }
                        else
                        {
                            cursorX = 0;
                            cursorY = 0;
                        }
                    }
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
                    case "Home":
                        cursorX = 0;
                        scrollX = 0;
                        break;
                    case "End":
                        cursorX = (short)(text[cursorY].Length);
                        scrollX = (short)(cursorX - width + 2);
                        if (scrollX < 0)
                            scrollX = 0;
                        break;
                    case "Tab":
                        break;
                    case "Enter":
                        if (text.Count < maxLines)
                        {
                            if (cursorY == text.Count - 1)
                            {
                                text.Add("");
                                cursorY++;
                                cursorX = 0;

                                scrollX = 0;
                                if (cursorY - scrollY > height - 2)
                                    scrollY++;
                            }
                            else
                            {
                                text.Insert(cursorY + 1, "");
                                cursorY++;
                                cursorX = 0;
                                scrollX = 0;
                            }
                        }
                        handled = true;
                        break;
                    case "Backspace":
                        if (cursorX > 0)
                        {
                            text[cursorY] = text[cursorY].Remove(cursorX - 1, 1);
                            cursorX--;
                            if (scrollX > cursorX)
                                scrollX--;
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

                                if (scrollX + width - 3 < cursorX)
                                    scrollX = (short)(cursorX - width + 2);

                                if (cursorY < scrollY)
                                    scrollY = cursorY;
                            }
                        }
                        handled = true;
                        break;
                    case "ArrowRight":
                        if (cursorX < (short)text[cursorY].Length)
                            cursorX++;

                        if (scrollX + width - 3 < cursorX)
                            scrollX = (short)(cursorX - width + 2);
                        handled = true;
                        break;
                    case "ArrowLeft":
                        if (cursorX > 0)
                            cursorX--;

                        if (cursorX < scrollX)
                            scrollX = cursorX;
                        handled = true;
                        break;
                    case "ArrowUp":
                        if (cursorY > 0)
                        {
                            cursorY--;
                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;

                            if (cursorX < scrollX)
                                scrollX = (short)(cursorX - width - 2);

                            if (scrollX < 0)
                                scrollX = 0;

                            if (cursorY < scrollY)
                                scrollY = cursorY;
                        }
                        handled = true;
                        break;
                    case "ArrowDown":
                        if (cursorY < text.Count - 1)
                        {
                            cursorY++;
                            if (text[cursorY].Length < cursorX)
                                cursorX = (short)text[cursorY].Length;

                            if (cursorX < scrollX)
                                scrollX = (short)(cursorX - width - 2);

                            if (scrollX < 0)
                                scrollX = 0;

                            if (cursorY - scrollY > height - 2)
                                scrollY++;
                        }
                        handled = true;
                        break;
                    default:
                        if (key.Length == 1 && text[cursorY].Length < maxTextWidth)
                        {
                            if (cursorX != text[cursorY].Length)
                                text[cursorY] = text[cursorY].Insert(cursorX, key);
                            else
                                text[cursorY] += key;

                            cursorX++;

                            if (scrollX + width - 3 < cursorX)
                                scrollX = (short)(cursorX - width + 2);
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

                    if ((h + scrollY) < text.Count)
                        line = text[h + scrollY];

                    for (short n = 0; n < width; n++)
                    {
                        if (container.YOffset() + Y + h < container.YOffset() + container.height && container.YOffset() + Y + h < rows.Count)
                        {
                            if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                            {
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].foreColor = foreColor;
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].backgroundColor = backgroundColor;
                                rows[container.YOffset() + Y + h].Cells[container.XOffset() + X + n].textDecoration = Cell.TextDecoration.None;

                                string ch = " ";

                                if (h == height - 1)
                                {
                                    if (n == 0)
                                        ch = "←";
                                    else if (n == width - 2)
                                        ch = "→";
                                    else if (n == width - 1)
                                        ch = "↓";
                                    else
                                        ch = "─";
                                }
                                else
                                {
                                    if (n == width - 1)
                                    {
                                        if (h == 0)
                                            ch = "↑";
                                        else
                                            ch = "│";
                                    }
                                    else if (n + scrollX < line.Length)
                                        ch = line.Substring(n + scrollX, 1);
                                }
                                
                                if (Focus)
                                {
                                    if (h == cursorY - scrollY && n == cursorX - scrollX)
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
