namespace BlazorTUI.TUI
{
    public sealed class TuiWizardStep
    {
        private string validationGroup = "";

        internal TuiWizardStep(string name, Container container, string? validationGroup)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(container);

            Name = name.Trim();
            Container = container;
            ValidationGroup = validationGroup ?? "";
        }

        public string Name { get; }

        public Container Container { get; }

        public string ValidationGroup
        {
            get => validationGroup;
            set => validationGroup = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }

        public bool AutoFocusFirstControl { get; set; } = true;
    }
}
