using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class AdditionalInputControlsTests
{
    [Fact]
    public void SearchBoxRaisesSearchAndClearEvents()
    {
        var search = new SearchBox(
            "search", "pizza", 0, 0, 12,
            Color.Yellow, Color.Black);
        string searched = "";
        bool cleared = false;
        search.SearchRequested += (_, args) => searched = args.Value;
        search.Cleared += (_, _) => cleared = true;

        Assert.True(search.KeyDown("Enter", false));
        Assert.Equal("pizza", searched);

        Assert.True(search.KeyDown("Escape", false));
        Assert.True(cleared);
        Assert.Equal("", search.Value);
    }

    [Fact]
    public void AutoCompleteBoxFiltersAndCommitsHighlightedSuggestion()
    {
        var screen = new Screen(40, 10);
        var autocomplete = new AutoCompleteBox(
            "city",
            "",
            new[] { "Barcelona", "Berlin", "Madrid" },
            2, 2, 14,
            Color.Yellow, Color.Black);
        string selected = "";
        autocomplete.SuggestionSelected += (_, args) => selected = args.Item;
        screen.TopContainer.AddControl(autocomplete);

        autocomplete.KeyDown("B", false);
        autocomplete.KeyDown("ArrowDown", false);
        autocomplete.KeyDown("Enter", false);

        Assert.Equal("Berlin", autocomplete.Value);
        Assert.Equal("Berlin", autocomplete.SelectedItem);
        Assert.Equal("Berlin", selected);
        Assert.False(autocomplete.IsDropDownOpen);
    }

    [Fact]
    public void MaskedTextBoxAcceptsOnlyCharactersAllowedByMask()
    {
        var screen = new Screen(30, 6);
        var mask = new MaskedTextBox(
            "phone",
            "000-AAA",
            "",
            2, 2,
            Color.Yellow, Color.Black)
        {
            IsRequired = true,
            RequiredMessage = "Complete phone"
        };
        screen.TopContainer.AddControl(mask);

        mask.KeyDown("1", false);
        mask.KeyDown("x", false);
        mask.KeyDown("2", false);
        mask.KeyDown("3", false);
        mask.KeyDown("A", false);
        mask.KeyDown("B", false);
        mask.KeyDown("C", false);
        screen.Render();

        Assert.Equal("123-ABC", mask.Value);
        Assert.True(mask.Validate());
        Assert.Contains("123-ABC", Read(screen, 2, 2, 7));

        mask.RawValue = "12";
        Assert.False(mask.Validate());
        Assert.Equal("Complete phone", mask.ValidationMessage);
    }

    [Fact]
    public void ToggleSwitchTogglesAndRaisesValueChanged()
    {
        var toggle = new ToggleSwitch(
            "alerts", "Alerts", false,
            0, 0, 16,
            Color.Yellow, Color.Black);
        bool? value = null;
        toggle.ValueChanged += (_, args) => value = args.Value;

        Assert.True(toggle.KeyDown("Space", false));

        Assert.True(toggle.Value);
        Assert.True(value);
        Assert.True(toggle.Validate());
    }

    [Fact]
    public void MultiSelectComboBoxTogglesHighlightedItems()
    {
        var combo = new MultiSelectComboBox(
            "tags",
            new[] { "Online", "VIP", "Late" },
            0, 0, 14,
            Color.Yellow, Color.Black);
        List<string> changed = new();
        combo.SelectionChanged += (_, args) => changed.Add($"{args.Item}:{args.IsSelected}");

        combo.KeyDown("Enter", false);
        combo.KeyDown("Space", false);
        combo.KeyDown("ArrowDown", false);
        combo.KeyDown("Space", false);

        Assert.Equal(new[] { "Online", "VIP" }, combo.SelectedItems);
        Assert.Contains("Online:True", changed);
        Assert.Contains("VIP:True", changed);
        Assert.True(combo.IsDropDownOpen);
    }

    [Fact]
    public void StatePersistenceRestoresAdditionalInputControls()
    {
        var screen = CreateStateScreen();
        ((SearchBox)screen.TopContainer.GetControl("search")!).Value = "order";
        AutoCompleteBox autocomplete = (AutoCompleteBox)screen.TopContainer.GetControl("city")!;
        autocomplete.KeyDown("B", false);
        autocomplete.KeyDown("Enter", false);
        ((MaskedTextBox)screen.TopContainer.GetControl("phone")!).RawValue = "555123456";
        ((ToggleSwitch)screen.TopContainer.GetControl("toggle")!).Value = true;
        ((MultiSelectComboBox)screen.TopContainer.GetControl("tags")!).SelectItem("VIP");

        TuiScreenState state = screen.ExportState();

        var restored = CreateStateScreen();
        restored.RestoreState(state);

        Assert.Equal("order", ((SearchBox)restored.TopContainer.GetControl("search")!).Value);
        Assert.Equal("Barcelona", ((AutoCompleteBox)restored.TopContainer.GetControl("city")!).SelectedItem);
        Assert.Equal("555-123-456", ((MaskedTextBox)restored.TopContainer.GetControl("phone")!).Value);
        Assert.True(((ToggleSwitch)restored.TopContainer.GetControl("toggle")!).Value);
        Assert.Equal(new[] { "VIP" }, ((MultiSelectComboBox)restored.TopContainer.GetControl("tags")!).SelectedItems);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SearchBox("search", "", 0, 0, 4, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new AutoCompleteBox("auto", "", new[] { "line\nbreak" }, 0, 0, 8, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() =>
            new MaskedTextBox("mask", "", "", 0, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ToggleSwitch("toggle", "", false, 0, 0, 7, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MultiSelectComboBox("multi", Array.Empty<string>(), 0, 0, 5, Color.White, Color.Black));
    }

    private static Screen CreateStateScreen()
    {
        var screen = new Screen(50, 12);
        screen.TopContainer.AddControl(new SearchBox("search", "", 0, 0, 12, Color.Yellow, Color.Black));
        screen.TopContainer.AddControl(new AutoCompleteBox(
            "city", "", new[] { "Barcelona", "Berlin" },
            0, 1, 14, Color.Yellow, Color.Black));
        screen.TopContainer.AddControl(new MaskedTextBox(
            "phone", "000-000-000", "",
            0, 2, Color.Yellow, Color.Black));
        screen.TopContainer.AddControl(new ToggleSwitch(
            "toggle", "Enabled", false,
            0, 3, 16, Color.Yellow, Color.Black));
        screen.TopContainer.AddControl(new MultiSelectComboBox(
            "tags", new[] { "Online", "VIP" },
            0, 4, 16, Color.Yellow, Color.Black));
        return screen;
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
