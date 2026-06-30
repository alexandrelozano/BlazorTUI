using System.Drawing;

namespace BlazorTUI.TUI
{
    public class DataForm<TModel> : Frame
        where TModel : class
    {
        private readonly List<FormField<TModel>> fields = new();

        public DataForm(
            string name,
            string title,
            TModel model,
            short X,
            short Y,
            short width,
            short height,
            BorderStyle borderStyle,
            Color foreColor,
            Color backgroundColor)
            : base(name, title, X, Y, width, height, borderStyle, foreColor, backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(model);
            Model = model;
        }

        public TModel Model { get; }

        public IReadOnlyList<FormField<TModel>> Fields => fields;

        public short ContentX { get; set; } = 2;

        public short FirstFieldY { get; set; } = 2;

        public short FieldSpacing { get; set; } = 2;

        public short LabelWidth { get; set; } = 12;

        public short EditorWidth { get; set; } = 20;

        public Color LabelForeColor { get; set; } = Color.White;

        public Color InputForeColor { get; set; } = Color.Yellow;

        public Color InputBackgroundColor { get; set; } = Color.Black;

        public ValidationSummary? Summary { get; private set; }

        public event EventHandler<DataFormModelUpdatedEventArgs<TModel>>? ModelUpdated;

        public void AddField(FormField<TModel> field)
        {
            ArgumentNullException.ThrowIfNull(field);
            Control editor = CreateEditor(field);
            AddField(field, editor);
        }

        public void AddField(FormField<TModel> field, Control editor)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(editor);

            if (fields.Any(existing => existing.Name == field.Name))
                throw new InvalidOperationException($"A form field named '{field.Name}' already exists.");

            short row = GetFieldRow();
            var label = new Label(
                $"{Name}_{field.Name}_label",
                field.Label,
                ContentX,
                row,
                LabelWidth,
                LabelForeColor,
                BackgroundColor);

            editor.X = (short)(ContentX + LabelWidth + 1);
            editor.Y = row;
            field.ApplyValidationTo(editor);
            field.Control = editor;
            field.LabelControl = label;
            fields.Add(field);

            base.AddControl(label);
            base.AddControl(editor);
            WireModelUpdates(field, editor);
        }

        public void SetValidationSummary(ValidationSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);
            Summary = summary;
            base.AddControl(summary);
        }

        public bool Submit(bool focusFirstInvalid = true)
        {
            if (!Validate(focusFirstInvalid))
                return false;

            UpdateModelFromControls();
            return true;
        }

        public void UpdateModelFromControls()
        {
            foreach (FormField<TModel> field in fields)
                UpdateModelFromField(field);
        }

        public bool UpdateModelFromField(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            FormField<TModel>? field = fields.FirstOrDefault(candidate => candidate.Name == fieldName);
            if (field is null)
                return false;

            UpdateModelFromField(field);
            return true;
        }

        public override bool Validate(bool focusFirstInvalid = true)
        {
            bool valid = base.Validate(focusFirstInvalid);
            UpdateSummary();
            return valid;
        }

        private Control CreateEditor(FormField<TModel> field)
        {
            short row = GetFieldRow();
            short editorX = (short)(ContentX + LabelWidth + 1);
            short width = field.ResolveEditorWidth(EditorWidth);
            string controlName = $"{Name}_{field.Name}_editor";
            var context = new FormFieldEditorContext<TModel>(
                this,
                field,
                controlName,
                editorX,
                row,
                width,
                InputForeColor,
                InputBackgroundColor);

            return field.EditorFactory is not null
                ? field.EditorFactory(context)
                : CreateDefaultEditor(context);
        }

        private Control CreateDefaultEditor(FormFieldEditorContext<TModel> context)
        {
            object? value = context.Field.GetModelValue(Model);
            return context.Field.EditorKind switch
            {
                FormFieldEditorKind.Password => new PasswordBox(
                    context.ControlName,
                    Convert.ToString(value) ?? "",
                    context.X,
                    context.Y,
                    context.Width,
                    context.ForeColor,
                    context.BackgroundColor),
                FormFieldEditorKind.Number => new NumericBox(
                    context.ControlName,
                    ConvertToNullableDouble(value),
                    context.Field.NumericIntegerPlaces,
                    context.Field.NumericDecimalPlaces,
                    context.Field.NumericSeparator,
                    context.X,
                    context.Y,
                    context.ForeColor,
                    context.BackgroundColor),
                FormFieldEditorKind.CheckBox => new CheckBox(
                    context.ControlName,
                    "",
                    context.X,
                    context.Y,
                    Math.Max((short)4, context.Width),
                    context.ForeColor,
                    context.BackgroundColor,
                    value is bool boolValue && boolValue),
                FormFieldEditorKind.Date => new DateBox(
                    context.ControlName,
                    ConvertToNullableDateOnly(value),
                    context.Field.DateFormat,
                    context.X,
                    context.Y,
                    context.ForeColor,
                    context.BackgroundColor),
                FormFieldEditorKind.Time => new TimeBox(
                    context.ControlName,
                    ConvertToNullableTimeOnly(value),
                    context.X,
                    context.Y,
                    context.ForeColor,
                    context.BackgroundColor),
                FormFieldEditorKind.ComboBox => new ComboBox(
                    context.ControlName,
                    context.Field.Options,
                    context.X,
                    context.Y,
                    Math.Max((short)4, context.Width),
                    context.ForeColor,
                    context.BackgroundColor,
                    GetSelectedOptionIndex(context.Field.Options, Convert.ToString(value))),
                FormFieldEditorKind.Custom => throw new InvalidOperationException("Custom form fields require an EditorFactory or an explicit editor control."),
                _ => new TextBox(
                    context.ControlName,
                    Convert.ToString(value) ?? "",
                    context.X,
                    context.Y,
                    context.Width,
                    context.ForeColor,
                    context.BackgroundColor)
            };
        }

        private short GetFieldRow()
            => (short)(FirstFieldY + fields.Count * FieldSpacing);

        private void WireModelUpdates(FormField<TModel> field, Control editor)
        {
            editor.LostFocus += (_, _) => UpdateModelFromField(field);

            switch (editor)
            {
                case ComboBox comboBox:
                    comboBox.SelectedIndexChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case RadioGroup radioGroup:
                    radioGroup.SelectionChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case Slider slider:
                    slider.ValueChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case DatePicker datePicker:
                    datePicker.ValueChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case DateRangePicker dateRangePicker:
                    dateRangePicker.ValueChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case MonthPicker monthPicker:
                    monthPicker.ValueChanged += (_, _) => UpdateModelFromField(field);
                    break;
                case Calendar calendar:
                    calendar.DateSelected += (_, _) => UpdateModelFromField(field);
                    break;
                default:
                    editor.Clicked += (_, _) => UpdateModelFromField(field);
                    break;
            }
        }

        private void UpdateModelFromField(FormField<TModel> field)
        {
            if (field.Control is null)
                return;

            object? previousValue = field.GetModelValue(Model);
            object? editorValue = field.ReadControlValue();
            if (Equals(previousValue, editorValue))
                return;

            field.SetModelValue(Model, editorValue);
            object? currentValue = field.GetModelValue(Model);
            ModelUpdated?.Invoke(this, new DataFormModelUpdatedEventArgs<TModel>(
                Model,
                field,
                field.Control,
                previousValue,
                currentValue));
        }

        private void UpdateSummary()
        {
            if (Summary is null)
                return;

            Summary.SetMessages(fields
                .Where(field => field.Control is { IsValid: false })
                .Select(field => $"{field.Label.TrimEnd(':')}: {field.Control!.ValidationMessage}"));
        }

        private static double? ConvertToNullableDouble(object? value)
        {
            if (value is null)
                return null;

            if (value is double doubleValue)
                return doubleValue;

            if (value is IConvertible convertible)
                return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);

            return null;
        }

        private static DateOnly? ConvertToNullableDateOnly(object? value)
            => value is DateOnly date ? date : null;

        private static TimeOnly? ConvertToNullableTimeOnly(object? value)
            => value is TimeOnly time ? time : null;

        private static int GetSelectedOptionIndex(IReadOnlyList<string> options, string? value)
        {
            if (options.Count == 0)
                return -1;

            if (value is null)
                return -1;

            int index = options.ToList().IndexOf(value);
            return index >= 0 ? index : -1;
        }
    }
}
