using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ValidationTests
{
    [Fact]
    public void RequiredTextBoxMarksInvalidRendersInlineMessageAndFocusesFirstInvalid()
    {
        var screen = new Screen(50, 5);
        var first = new TextBox("firstName", "", 1, 1, 10, Color.Yellow, Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "First name required"
        };
        var second = new TextBox("lastName", "", 1, 2, 10, Color.Yellow, Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Last name required"
        };
        screen.TopContainer.AddControl(first);
        screen.TopContainer.AddControl(second);
        screen.SetFocus("lastName");

        bool isValid = screen.Validate();
        screen.Render();

        Assert.False(isValid);
        Assert.False(first.IsValid);
        Assert.False(second.IsValid);
        Assert.Equal("First name required", first.ValidationMessage);
        Assert.True(first.Focus);
        Assert.False(second.Focus);
        Assert.Contains("First name required", RowText(screen, 1));
        Assert.Equal(Color.White, screen.Rows[1].Cells[12].ForeColor);
        Assert.Equal(Color.DarkRed, screen.Rows[1].Cells[12].BackgroundColor);
    }

    [Fact]
    public void CustomValidationRulesReportAndClearErrors()
    {
        var input = new TextBox("code", "ab", 1, 1, 8, Color.Yellow, Color.Black);
        input.ValidationRules.Add(value => value is string text && text.Length >= 3, "Use at least 3 chars");
        List<TuiValidationChangedEventArgs> changes = new();
        input.ValidationChanged += (_, args) => changes.Add(args);

        Assert.False(input.Validate());
        Assert.False(input.IsValid);
        Assert.Equal("Use at least 3 chars", input.ValidationMessage);
        Assert.Equal(Color.White, input.ForeColor);
        Assert.Equal(Color.DarkRed, input.BackgroundColor);

        input.Value = "abcd";
        Assert.True(input.Validate());

        Assert.True(input.IsValid);
        Assert.Empty(input.ValidationMessage);
        Assert.Equal(Color.Yellow, input.ForeColor);
        Assert.Equal(Color.Black, input.BackgroundColor);
        Assert.Equal(new[] { false, true }, changes.Select(change => change.IsValid));
    }

    [Fact]
    public void RequiredBooleanAndSelectionControlsUseTheirCurrentValue()
    {
        var checkBox = new CheckBox(
            "accept", "Accept terms", 1, 1, 18,
            Color.Yellow, Color.Black)
        {
            IsRequired = true
        };
        var comboBox = new ComboBox(
            "priority", Array.Empty<string>(), 1, 2, 12,
            Color.Yellow, Color.Black)
        {
            IsRequired = true
        };

        Assert.False(checkBox.Validate());
        Assert.False(comboBox.Validate());

        checkBox.Value = true;
        comboBox.AddItem("Normal");

        Assert.True(checkBox.Validate());
        Assert.True(comboBox.Validate());
    }

    [Fact]
    public void DateTimeAndNumericControlsValidateParsedValue()
    {
        var date = new DateBox(
            "date", null, DateBox.DateFormat.YYYYMMDD, 1, 1,
            Color.Yellow, Color.Black)
        {
            IsRequired = true
        };
        var time = new TimeBox(
            "time", null, 1, 2,
            Color.Yellow, Color.Black)
        {
            IsRequired = true
        };
        var numeric = new NumericBox(
            "amount", null, 2, 0, '.', 1, 3,
            Color.Yellow, Color.Black)
        {
            IsRequired = true
        };

        Assert.False(date.Validate());
        Assert.False(time.Validate());
        Assert.False(numeric.Validate());

        date.Value = new DateOnly(2026, 6, 27);
        time.Value = new TimeOnly(8, 30);
        numeric.Value = 42;

        Assert.True(date.Validate());
        Assert.True(time.Validate());
        Assert.True(numeric.Validate());
    }

    [Fact]
    public void ScreenValidationUsesTopDialogWhenDialogIsOpen()
    {
        var screen = new Screen(40, 12);
        var rootInput = new TextBox("rootName", "", 1, 1, 10, Color.Yellow, Color.Black)
        {
            IsRequired = true
        };
        screen.TopContainer.AddControl(rootInput);

        var dialog = new Dialog(
            "dialog", "Dialog", 24, 6, BorderStyle.Line,
            Color.White, Color.DarkBlue, screen);
        var dialogInput = new TextBox("dialogName", "ready", 2, 2, 10, Color.Yellow, Color.Black)
        {
            IsRequired = true
        };
        dialog.AddControl(dialogInput);
        dialog.Show();

        Assert.True(screen.Validate());
        Assert.True(rootInput.IsValid);
        Assert.True(dialogInput.IsValid);
    }

    [Fact]
    public void ValidationMessageMovesBelowControlWhenRightSideHasNoRoom()
    {
        var screen = new Screen(16, 4);
        var input = new TextBox("name", "", 1, 1, 14, Color.Yellow, Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Required"
        };
        screen.TopContainer.AddControl(input);

        Assert.False(screen.Validate());
        screen.Render();

        Assert.Contains("Required", RowText(screen, 2));
    }

    private static string RowText(Screen screen, int row)
        => string.Concat(screen.Rows[row].Cells.Select(cell => cell.Character));
}
