using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class FormFieldEditorContext<TModel>
        where TModel : class
    {
        public FormFieldEditorContext(
            DataForm<TModel> form,
            FormField<TModel> field,
            string controlName,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(form);
            ArgumentNullException.ThrowIfNull(field);
            ArgumentException.ThrowIfNullOrWhiteSpace(controlName);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);

            Form = form;
            Field = field;
            ControlName = controlName;
            this.X = X;
            this.Y = Y;
            Width = width;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
        }

        public DataForm<TModel> Form { get; }

        public FormField<TModel> Field { get; }

        public TModel Model => Form.Model;

        public string ControlName { get; }

        public short X { get; }

        public short Y { get; }

        public short Width { get; }

        public Color ForeColor { get; }

        public Color BackgroundColor { get; }
    }
}
