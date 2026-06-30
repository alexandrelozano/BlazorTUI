using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class FormField<TModel>
        where TModel : class
    {
        private readonly Func<TModel, object?> getValue;
        private readonly Action<TModel, object?> setValue;
        private string name;
        private string label;
        private IReadOnlyList<string> options = Array.Empty<string>();

        public FormField(
            string name,
            string label,
            Func<TModel, object?> getValue,
            Action<TModel, object?> setValue,
            FormFieldEditorKind editorKind = FormFieldEditorKind.Text)
        {
            ArgumentNullException.ThrowIfNull(getValue);
            ArgumentNullException.ThrowIfNull(setValue);

            this.name = ValidateName(name);
            this.label = label ?? "";
            this.getValue = getValue;
            this.setValue = setValue;
            EditorKind = editorKind;
        }

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public string Label
        {
            get => label;
            set => label = value ?? "";
        }

        public FormFieldEditorKind EditorKind { get; set; }

        public short? EditorWidth { get; set; }

        public bool IsRequired { get; set; }

        public string RequiredMessage { get; set; } = "This field is required.";

        public TuiValidationRuleCollection ValidationRules { get; } = new();

        public IReadOnlyList<string> Options
        {
            get => options;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                options = value.Select(ValidateOption).ToArray();
            }
        }

        public short NumericIntegerPlaces { get; set; } = 5;

        public short NumericDecimalPlaces { get; set; }

        public char NumericSeparator { get; set; } = '.';

        public DateBox.DateFormat DateFormat { get; set; } = DateBox.DateFormat.YYYYMMDD;

        public FormFieldEditorFactory<TModel>? EditorFactory { get; set; }

        public Func<Control, object?>? ValueReader { get; set; }

        public Control? Control { get; internal set; }

        public Label? LabelControl { get; internal set; }

        public object? GetModelValue(TModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return getValue(model);
        }

        public void SetModelValue(TModel model, object? value)
        {
            ArgumentNullException.ThrowIfNull(model);
            setValue(model, value);
        }

        internal object? ReadControlValue()
        {
            if (Control is null)
                return null;

            return ValueReader is not null
                ? ValueReader(Control)
                : DataFormValueAccessor.Read(Control);
        }

        internal void ApplyValidationTo(Control control)
        {
            control.IsRequired = IsRequired;
            control.RequiredMessage = RequiredMessage;
            foreach (TuiValidationRule rule in ValidationRules)
                control.ValidationRules.Add(rule);
        }

        internal short ResolveEditorWidth(short fallback)
            => EditorWidth ?? fallback;

        private static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }

        private static string ValidateOption(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value;
        }
    }
}
