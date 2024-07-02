using System.Drawing;
using System.Timers;

namespace BlazorTUI.TUI
{
    public class Spinner : Control
    {
        public enum SpinnerType
        {
            ArrowSpinner,
            GrowVerticalSpinner,
            Star
        }

        SpinnerType spinnerType;

        private System.Timers.Timer tt;
        private string elements;
        private short n;

        public Spinner(string name, SpinnerType spinnerType, short X, short Y, Color forecolor, Color backgroundcolor)
        {
            this.name = name;
            this.X = X;
            this.Y = Y;
            this.spinnerType = spinnerType;
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            n = 0;

            switch (spinnerType)
            {
                case SpinnerType.ArrowSpinner:
                    elements = "←↖↑↗→↘↓↙";
                    break;
                case SpinnerType.GrowVerticalSpinner:
                    elements = "▁▃▄▅▆▇▆▅▄▃";
                    break;
                case SpinnerType.Star:
                    elements = "|/-\\";
                    break;
            }

            tt = new System.Timers.Timer(250);
            tt.Elapsed += new System.Timers.ElapsedEventHandler(TimerElapsed);
            tt.Start();
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            n++;

            if (n >= elements.Length)
                n = 0;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                if (container.YOffset() + Y < container.YOffset() + container.height && container.YOffset() + Y < rows.Count)
                {
                    if (container.XOffset() + X < container.XOffset() + container.width && container.XOffset() + X < rows[Y].Cells.Count)
                    {
                        rows[container.YOffset() + Y].Cells[container.XOffset() + X].foreColor = foreColor;
                        rows[container.YOffset() + Y].Cells[container.XOffset() + X].backgroundColor = backgroundColor;
                        rows[container.YOffset() + Y].Cells[container.XOffset() + X].character = elements[n].ToString();
                    }
                }
            }
        }
    }
}
