using System.Drawing;

namespace BlazorTUI.TUI
{
    public class ProgressBar : Control
    {
        public enum ProgressBarType
        {
            DoubleLine,
            Line,
            Solid
        }

        ProgressBarType progessBarType;

        public Double value;
        public Double MaxValue;

        private bool showPercent;

        public ProgressBar(string name, ProgressBarType progessBarType, short X, short Y, short width, Double value, Double maxValue, bool showPercent, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.progessBarType = progessBarType;
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;
            this.value = value;
            this.MaxValue = maxValue;
            this.showPercent = showPercent;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                {
                    for (short n = 0; n < width; n++)
                    {
                        if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[Y].Cells.Count)
                        {
                            Color fc = foreColor;
                            Color bc = backgroundColor;

                            string ch = "";

                            if ((value == 0 || n == 0) || (value > 0 && n > 0 && (MaxValue / value) < ((double)width / n)))
                            {
                                switch (progessBarType)
                                {
                                    case ProgressBarType.DoubleLine:
                                        ch = "═";
                                        break;
                                    case ProgressBarType.Line:
                                        ch = "─";
                                        break;
                                    case ProgressBarType.Solid:
                                        fc = backgroundColor;
                                        bc = foreColor;
                                        ch = "";
                                        break;
                                }
                            }

                            if (showPercent)
                            {
                                string percent = $"{((value / MaxValue) * 100.0).ToString("000")}%";
                                if (n == (width / 2) - 2)
                                {
                                    if (value == MaxValue)
                                    {
                                        ch = percent.Substring(0, 1);
                                    }
                                }
                                else if (n == (width / 2) - 1)
                                {
                                    ch = percent.Substring(1, 1);
                                }
                                else if (n == (width / 2))
                                {
                                    ch = percent.Substring(2, 1);
                                }
                                else if (n == (width / 2) + 1)
                                {
                                    ch = percent.Substring(3, 1);
                                }
                            }

                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].foreColor = fc;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].backgroundColor = bc;
                            rows[container.YOffset() + Y].Cells[container.XOffset() + X + n].character = ch;
                        }
                    }
                }
            }
        }
    }
}