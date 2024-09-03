using BlazorTUI.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public class ColorPicker : Control
    {
        public Color color;
        public bool showColorName;

        public Color foreColor;
        public Color backgroundColor;

        private Screen screen;

        private Dialog dlgColors;

        public ColorPicker(string name, Color color, bool showColorName, short X, short Y, short width, Color foreColor, Color backgroundColor, Screen screen)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.color = color;
            this.showColorName = showColorName;
            this.width = width;
            this.height = 1;
            this.foreColor = foreColor;
            this.backgroundColor = backgroundColor;
            this.screen = screen;   
        }

        private void PickColor()
        {
            short widthDlg = (short)(container.TopContainer().width / 2);
            short heigthDlg = (short)(container.TopContainer().height / 2);

            dlgColors = new Dialog("dlgColors", "Pick a color", widthDlg, heigthDlg, BorderStyle.line, foreColor, backgroundColor, screen);

            Button bttCancel = new Button("bttCancel", "Cancel", 2, (short)(dlgColors.height - 2), 10, foreColor, backgroundColor);
            bttCancel.OnClick = bttCancel_OnClick;
            dlgColors.AddControl(bttCancel);

            var colorProperties = color.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public);
            var colors = colorProperties.Select(prop => (Color)prop.GetValue(null, null));
            short xc = 2;
            short yc = 2;
            foreach (Color myColor in colors)
            {
                Button bttColor = new Button($"{myColor.Name}", " ", xc, yc, 3, foreColor, myColor);
                bttColor.OnClick = bttDlgColor_OnClick;
                dlgColors.AddControl(bttColor);

                xc += 3;

                if (xc > widthDlg - 5){
                    xc = 2;
                    yc++;
                }
            }

            dlgColors.Show();
        }

        public void bttDlgColor_OnClick(Control sender)
        {
            color = Color.FromName(sender.name);
            dlgColors.Close();
        }

        public void bttCancel_OnClick(Control sender)
        {
            dlgColors.Close();
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                container.TopContainer().SetFocus(name);

                PickColor();

                if (OnClick != null)
                    OnClick.Invoke(this);

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
                        PickColor();

                        if (OnClick != null)
                            OnClick.Invoke(this);
                        handled = true;
                        break;
                    case "Enter":
                        PickColor();

                        if (OnClick != null)
                            OnClick.Invoke(this);
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
                string colorName = showColorName ? color.Name.CenterString(width - 2) : "";
                
                for (short x = 0; x < width; x++)
                {
                    if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                    {
                        if (container.XOffset() + X + x < container.XOffset() + container.width && container.XOffset() + X + x < rows[Y].Cells.Count)
                        {
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].visible = true;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].backgroundImage = "";
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].scaleX = 1;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].scaleY = 1;

                            if (x == 0)
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].foreColor = foreColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].backgroundColor = backgroundColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].character = "[";
                            }
                            else if (x < width - 1)
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].foreColor = foreColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].backgroundColor = color;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].character = colorName.Substring(x - 1,1);
                            }
                            else
                            {
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].foreColor = foreColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].backgroundColor = backgroundColor;
                                rows[container.YOffset() + Y].Cells[container.XOffset() + X + x].character = "]";
                            }
                        }
                    }
                }
            }
        }
    }
}
