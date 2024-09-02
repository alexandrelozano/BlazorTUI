using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorTUI.TUI
{
    public class PictureBox : Control
    {
        string pngB64;

        public PictureBox(string name, System.Drawing.Image image, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;

            System.IO.MemoryStream ms = new MemoryStream();
            Bitmap bImage = new Bitmap(image);
            bImage.Save(ms, ImageFormat.Png);
            byte[] byteImage = ms.ToArray();
            this.pngB64 = Convert.ToBase64String(byteImage);

            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                for (short x = 0; x < width; x++)
                {
                    for (short y = 0; y < height; y++)
                    {
                        if (container.YOffset() + Y + y < container.YOffset() + container.height && container.YOffset() + Y + y < rows.Count)
                        {
                            if (container.XOffset() + X + x < container.XOffset() + container.width && container.XOffset() + X + x < rows[Y].Cells.Count)
                            {
                                rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].visible = true;    

                                if (x==0 && y == 0)
                                {
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].character = "";
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].foreColor = foreColor;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].backgroundColor = backgroundColor;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].backgroundImage = pngB64;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].scaleX = width;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].scaleY = height;
                                }
                                else
                                {
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].backgroundImage = "";
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
