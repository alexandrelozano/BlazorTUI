using System.Drawing;

namespace BlazorTUI.TUI
{
    public class PictureBox : Control
    {
        private readonly string imageDataUrl;

        public PictureBox(string name, byte[] pngData, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
            : this(name, pngData, "image/png", X, Y, width, height, forecolor, backgroundcolor)
        {
        }

        public PictureBox(string name, byte[] imageData, string mediaType, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(imageData);
            ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

            if (imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be empty.", nameof(imageData));
            }

            if (!mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                mediaType.IndexOfAny([';', ',', '\r', '\n', '\'', '"']) >= 0)
            {
                throw new ArgumentException("The media type must be a valid image MIME type.", nameof(mediaType));
            }

            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            this.imageDataUrl = $"data:{mediaType};base64,{Convert.ToBase64String(imageData)}";
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

                                if (x == 0 && y == 0)
                                {
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].character = "";
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].foreColor = foreColor;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].backgroundColor = backgroundColor;
                                    rows[container.YOffset() + Y + y].Cells[container.XOffset() + X + x].backgroundImage = imageDataUrl;
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
