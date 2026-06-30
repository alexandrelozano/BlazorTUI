namespace BlazorTUI.TUI
{
    public delegate Control FormFieldEditorFactory<TModel>(FormFieldEditorContext<TModel> context)
        where TModel : class;
}
