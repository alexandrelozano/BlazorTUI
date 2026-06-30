namespace BlazorTUI.TUI
{
    public sealed class DataFormModelUpdatedEventArgs<TModel> : EventArgs
        where TModel : class
    {
        public DataFormModelUpdatedEventArgs(
            TModel model,
            FormField<TModel> field,
            Control control,
            object? previousValue,
            object? currentValue)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(control);

            Model = model;
            Field = field;
            FieldName = field.Name;
            Control = control;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }

        public TModel Model { get; }

        public FormField<TModel> Field { get; }

        public string FieldName { get; }

        public Control Control { get; }

        public object? PreviousValue { get; }

        public object? CurrentValue { get; }
    }
}
