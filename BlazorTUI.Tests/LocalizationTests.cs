using System.Drawing;
using System.Globalization;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class LocalizationTests
{
    [Fact]
    public void CultureOptionsFormatDatesTimesNumbersCurrencyAndValidationMessages()
    {
        var culture = new TuiCultureOptions("ca-ES")
        {
            RequiredMessage = "Camp obligatori."
        };

        Assert.Equal("01/07/2026", culture.FormatDate(new DateOnly(2026, 7, 1), DateBox.DateFormat.CultureShortDate));
        Assert.Contains("2026", culture.FormatMonth(new DateOnly(2026, 7, 1), MonthPicker.MonthFormat.CultureMonthYear));
        Assert.Equal("14:30", culture.FormatTime(new TimeOnly(14, 30)));
        Assert.Equal("1,5", culture.FormatNumber(1.5, "0.0"));
        Assert.Contains("25,00", culture.FormatCurrency(25m));

        var input = new TextBox("localizedText", "", 0, 0, 10, Color.Yellow, Color.Black)
        {
            CultureOptions = culture,
            IsRequired = true
        };

        Assert.False(input.Validate());
        Assert.Equal("Camp obligatori.", input.ValidationMessage);
    }

    [Fact]
    public void DateAndMonthControlsUseConfiguredCulture()
    {
        var culture = new TuiCultureOptions("ca-ES");
        var screen = new Screen(70, 16);
        var datePicker = new DatePicker(
            "localizedDate",
            new DateOnly(2026, 7, 1),
            DateBox.DateFormat.CultureShortDate,
            1,
            1,
            Color.Yellow,
            Color.Black,
            width: 14,
            cultureOptions: culture);
        var monthPicker = new MonthPicker(
            "localizedMonth",
            new DateOnly(2026, 7, 1),
            MonthPicker.MonthFormat.CultureMonthYear,
            1,
            3,
            Color.Yellow,
            Color.Black,
            width: 24,
            cultureOptions: culture);
        var calendar = new global::BlazorTUI.TUI.Calendar(
            "localizedCalendar",
            new DateOnly(2026, 7, 1),
            1,
            5,
            Color.Yellow,
            Color.Black,
            cultureOptions: culture);
        screen.TopContainer.AddControl(datePicker);
        screen.TopContainer.AddControl(monthPicker);
        screen.TopContainer.AddControl(calendar);

        screen.Render();

        Assert.Contains("01/07/2026", Read(screen, 1, 1, 14));
        Assert.Contains("2026", Read(screen, 1, 3, 24));
        Assert.Contains("jul", Read(screen, 1, 5, 22), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NumericBoxAndTimeBoxUseConfiguredCulture()
    {
        var numberCulture = new TuiCultureOptions("ca-ES");
        var timeCulture = new TuiCultureOptions("fi-FI");
        var screen = new Screen(30, 5);
        var numeric = new NumericBox(
            "amount",
            1234.5,
            5,
            1,
            1,
            1,
            Color.Yellow,
            Color.Black,
            numberCulture);
        var time = new TimeBox(
            "time",
            new TimeOnly(14, 30),
            1,
            2,
            Color.Yellow,
            Color.Black,
            timeCulture);
        screen.TopContainer.AddControl(numeric);
        screen.TopContainer.AddControl(time);

        screen.Render();

        Assert.Contains(",", Read(screen, 1, 1, 8));
        Assert.Contains(".", Read(screen, 1, 2, 6));
    }

    [Fact]
    public void ValidationRuleMessageProviderReceivesCultureOptions()
    {
        var culture = new TuiCultureOptions("ca-ES");
        var input = new TextBox("amount", "10", 0, 0, 10, Color.Yellow, Color.Black)
        {
            CultureOptions = culture
        };
        input.ValidationRules.Add(
            _ => false,
            (_, options) => options.FormatValidationMessage("Use exactly {0}.", options.FormatCurrency(25m)));

        Assert.False(input.Validate());
        Assert.Contains("25,00", input.ValidationMessage);
    }

    [Fact]
    public void GridViewEditingUsesConfiguredCultureAndMessages()
    {
        var columns = new[]
        {
            new GridView.GridColumn
            {
                Title = "Amount",
                Width = 8,
                IsEditable = true,
                EditorKind = GridViewCellEditorKind.NumericBox
            }
        };
        var rows = new[]
        {
            new GridView.GridRow { Cells = new[] { "1,5" } }
        };
        var culture = new TuiCultureOptions("ca-ES")
        {
            InvalidNumberMessage = "El valor ha de ser numèric."
        };
        var grid = new GridView("grid", columns, rows, 0, 0, 20, 6, Color.Yellow, Color.Black)
        {
            CultureOptions = culture,
            IsReadOnly = false
        };

        Assert.True(grid.BeginEdit(0, 0));
        Assert.True(grid.CommitEdit());

        string[] invalidCells = rows[0].Cells.ToArray();
        invalidCells[0] = "abc";
        rows[0].Cells = invalidCells;
        Assert.True(grid.BeginEdit(0, 0));
        Assert.False(grid.CommitEdit());
        Assert.Equal("El valor ha de ser numèric.", grid.EditValidationMessage);
    }

    [Fact]
    public void DataFormPropagatesCultureToGeneratedEditors()
    {
        var model = new LocalizedModel { Amount = 12.5, DueDate = new DateOnly(2026, 7, 1) };
        var culture = new TuiCultureOptions("ca-ES") { RequiredMessage = "Camp obligatori." };
        var form = new DataForm<LocalizedModel>(
            "form",
            "Form",
            model,
            0,
            0,
            50,
            8,
            Frame.BorderStyle.Line,
            Color.White,
            Color.DarkBlue)
        {
            CultureOptions = culture
        };
        form.AddField(new FormField<LocalizedModel>(
            "Amount",
            "Amount:",
            candidate => candidate.Amount,
            (candidate, value) => candidate.Amount = Convert.ToDouble(value, CultureInfo.InvariantCulture),
            FormFieldEditorKind.Number)
        {
            NumericIntegerPlaces = 2,
            NumericDecimalPlaces = 1,
            UseCultureNumericSeparator = true
        });
        form.AddField(new FormField<LocalizedModel>(
            "DueDate",
            "Due:",
            candidate => candidate.DueDate,
            (candidate, value) => candidate.DueDate = value is DateOnly date ? date : null,
            FormFieldEditorKind.Date)
        {
            DateFormat = DateBox.DateFormat.CultureShortDate
        });

        var screen = new Screen(60, 12);
        screen.TopContainer.AddContainer(form);
        screen.Render();

        Assert.Contains(",", Read(screen, form.X + form.ContentX + form.LabelWidth + 1, form.Y + form.FirstFieldY, 6));
        Assert.Contains("01/07/2026", Read(screen, form.X + form.ContentX + form.LabelWidth + 1, form.Y + form.FirstFieldY + form.FieldSpacing, 11));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));

    private sealed class LocalizedModel
    {
        public double Amount { get; set; }

        public DateOnly? DueDate { get; set; }
    }
}
