namespace BlazorTUI.TUI
{
    public sealed class BarChartItem
    {
        private string name;
        private string label;
        private double value;

        public string Name
        {
            get => name;
            set => name = ControlName(value);
        }

        public string Label
        {
            get => label;
            set => label = value ?? "";
        }

        public double Value
        {
            get => value;
            set
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                this.value = value;
            }
        }

        public BarChartItem(string name, string label, double value)
        {
            this.name = ControlName(name);
            this.label = label ?? "";
            Value = value;
        }

        private static string ControlName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }
}
