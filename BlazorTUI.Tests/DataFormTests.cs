using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class DataFormTests
{
    [Fact]
    public void SubmitValidatesUpdatesModelAndRaisesModelUpdated()
    {
        var model = new TestModel { Name = "", Priority = "Normal" };
        var screen = new Screen(60, 20);
        var form = CreateForm(model);
        screen.TopContainer.AddContainer(form);
        var field = new FormField<TestModel>(
            "name",
            "Name:",
            candidate => candidate.Name,
            (candidate, value) => candidate.Name = Convert.ToString(value) ?? "")
        {
            IsRequired = true,
            RequiredMessage = "Name required"
        };
        form.AddField(field);
        form.SetValidationSummary(new ValidationSummary(
            "summary", 2, 5, 30, 2,
            Color.White, Color.DarkRed));
        List<string> updatedFields = new();
        form.ModelUpdated += (_, args) => updatedFields.Add(args.FieldName);

        Assert.False(form.Submit());
        Assert.Equal("Name: Name required", Assert.Single(form.Summary!.Messages));
        Assert.Equal("orderForm_name_editor", screen.TopContainer.GetCurrentFocusControl()?.Name);

        TextBox editor = Assert.IsType<TextBox>(field.Control);
        editor.Value = "Alex";

        Assert.True(form.Submit());
        Assert.Equal("Alex", model.Name);
        Assert.Contains("name", updatedFields);
        Assert.Empty(form.Summary.Messages);
    }

    [Fact]
    public void ComboBoxGeneratedEditorUpdatesModelOnSelectionChange()
    {
        var model = new TestModel { Priority = "Normal" };
        var form = CreateForm(model);
        var field = new FormField<TestModel>(
            "priority",
            "Priority:",
            candidate => candidate.Priority,
            (candidate, value) => candidate.Priority = Convert.ToString(value) ?? "",
            FormFieldEditorKind.ComboBox)
        {
            Options = new[] { "Low", "Normal", "High" }
        };
        form.AddField(field);

        ComboBox editor = Assert.IsType<ComboBox>(field.Control);
        editor.SelectItem("High");

        Assert.Equal("High", model.Priority);
    }

    [Fact]
    public void ExplicitEditorFactoryCanCreateCustomControl()
    {
        var model = new TestModel { Password = "secret" };
        var form = CreateForm(model);
        var field = new FormField<TestModel>(
            "password",
            "Password:",
            candidate => candidate.Password,
            (candidate, value) => candidate.Password = Convert.ToString(value) ?? "",
            FormFieldEditorKind.Custom)
        {
            EditorFactory = context => new PasswordBox(
                context.ControlName,
                Convert.ToString(context.Field.GetModelValue(context.Model)) ?? "",
                context.X,
                context.Y,
                context.Width,
                context.ForeColor,
                context.BackgroundColor)
        };
        form.AddField(field);

        PasswordBox editor = Assert.IsType<PasswordBox>(field.Control);
        editor.Value = "changed";
        form.UpdateModelFromControls();

        Assert.Equal("changed", model.Password);
    }

    [Fact]
    public void ExplicitControlCanBeBoundToField()
    {
        var model = new TestModel();
        var form = CreateForm(model);
        var field = new FormField<TestModel>(
            "notify",
            "Notify:",
            candidate => candidate.Notify,
            (candidate, value) => candidate.Notify = value is bool enabled && enabled,
            FormFieldEditorKind.CheckBox);
        var editor = new CheckBox(
            "notifyEditor", "", 0, 0, 6,
            Color.Yellow, Color.Black);
        form.AddField(field, editor);

        editor.Click(0, 0);

        Assert.True(model.Notify);
    }

    [Fact]
    public void ValidationSummaryStateCanBeExportedAndRestored()
    {
        var screen = new Screen(40, 10);
        var summary = new ValidationSummary(
            "summary", 1, 1, 20, 2,
            Color.White, Color.DarkRed);
        summary.SetMessages(new[] { "Name required" });
        screen.TopContainer.AddControl(summary);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(40, 10);
        var restoredSummary = new ValidationSummary(
            "summary", 1, 1, 20, 2,
            Color.White, Color.DarkRed);
        restoredScreen.TopContainer.AddControl(restoredSummary);
        restoredScreen.RestoreState(state);

        Assert.Equal("Name required", Assert.Single(restoredSummary.Messages));
    }

    [Fact]
    public void ValidatesArgumentsAndDuplicateFields()
    {
        var model = new TestModel();
        Assert.Throws<ArgumentNullException>(() =>
            new DataForm<TestModel>("form", "FORM", null!, 0, 0, 20, 8, Frame.BorderStyle.Line, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new FormField<TestModel>("", "Name", candidate => candidate.Name, (_, _) => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ValidationSummary("summary", 0, 0, 0, 1, Color.White, Color.Black));

        var form = CreateForm(model);
        form.AddField(new FormField<TestModel>("name", "Name", candidate => candidate.Name, (_, _) => { }));
        Assert.Throws<InvalidOperationException>(() =>
            form.AddField(new FormField<TestModel>("name", "Name", candidate => candidate.Name, (_, _) => { })));
    }

    private static DataForm<TestModel> CreateForm(TestModel model)
        => new(
            "orderForm",
            "ORDER",
            model,
            1, 1, 40, 12,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);

    private sealed class TestModel
    {
        public string Name { get; set; } = "";

        public string Priority { get; set; } = "";

        public string Password { get; set; } = "";

        public bool Notify { get; set; }
    }
}
