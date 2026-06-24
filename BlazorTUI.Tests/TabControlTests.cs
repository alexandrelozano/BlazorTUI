using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class TabControlTests
{
    [Fact]
    public void RendersHeadersAndOnlyTheSelectedPage()
    {
        var screen = new Screen(30, 12);
        var tabs = new TabControl("tabs", 1, 1, 26, 9, Color.Yellow, Color.DarkBlue);
        screen.TopContainer.AddContainer(tabs);
        TabPage general = tabs.AddTab("generalTab", "General");
        TabPage advanced = tabs.AddTab("advancedTab", "Advanced");
        general.AddControl(new Label("generalLabel", "GENERAL", 1, 1, 8, Color.White, Color.DarkBlue));
        advanced.AddControl(new Label("advancedLabel", "ADVANCED", 1, 1, 8, Color.White, Color.DarkBlue));

        screen.Render();

        Assert.Equal("[General]", Read(screen, 2, 1, 9));
        Assert.Equal("GENERAL", Read(screen, 3, 4, 7));
        Assert.True(general.Visible);
        Assert.False(advanced.Visible);

        tabs.SelectTab(1);
        screen.Render();

        Assert.Equal("ADVANCED", Read(screen, 3, 4, 8));
        Assert.False(general.Visible);
        Assert.True(advanced.Visible);
    }

    [Fact]
    public void ClickingAHeaderSelectsTheTabAndRaisesTheEvent()
    {
        var tabs = new TabControl("tabs", 0, 0, 24, 8, Color.White, Color.Black);
        tabs.AddTab("firstTab", "First");
        tabs.AddTab("secondTab", "Second");
        int changes = 0;
        tabs.SelectedTabChanged += (_, _) => changes++;

        tabs.Click(10, 0);

        Assert.Equal(1, tabs.SelectedIndex);
        Assert.Equal("secondTab", tabs.SelectedTab?.Name);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void SettingFocusSelectsTheContainingTab()
    {
        var screen = new Screen(30, 12);
        var tabs = new TabControl("tabs", 1, 1, 26, 9, Color.White, Color.Black);
        screen.TopContainer.AddContainer(tabs);
        TabPage first = tabs.AddTab("firstTab", "First");
        TabPage second = tabs.AddTab("secondTab", "Second");
        var firstInput = new TextBox("firstInput", "", 1, 1, 8, Color.White, Color.Black);
        var secondInput = new TextBox("secondInput", "", 1, 1, 8, Color.White, Color.Black);
        first.AddControl(firstInput);
        second.AddControl(secondInput);

        screen.SetFocus("firstInput");
        screen.SetFocus("secondInput");

        Assert.Equal(1, tabs.SelectedIndex);
        Assert.False(firstInput.Focus);
        Assert.True(secondInput.Focus);

        screen.KeyDown("x", false);
        Assert.Equal("", firstInput.Value);
        Assert.Equal("x", secondInput.Value);
    }

    [Fact]
    public void NextAndPreviousTabWrapAndFocusTheFirstControl()
    {
        var screen = new Screen(30, 12);
        var tabs = new TabControl("tabs", 1, 1, 26, 9, Color.White, Color.Black);
        screen.TopContainer.AddContainer(tabs);
        TabPage first = tabs.AddTab("firstTab", "First");
        TabPage second = tabs.AddTab("secondTab", "Second");
        var firstButton = new Button("firstButton", "First", 1, 1, 8, Color.White, Color.Black);
        var secondButton = new Button("secondButton", "Second", 1, 1, 8, Color.White, Color.Black);
        first.AddControl(firstButton);
        second.AddControl(secondButton);

        tabs.SelectNextTab();
        Assert.Equal(1, tabs.SelectedIndex);
        Assert.True(secondButton.Focus);

        tabs.SelectNextTab();
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.True(firstButton.Focus);

        tabs.SelectPreviousTab();
        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void ValidatesDimensionsTabsAndSelectedIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TabControl("tabs", 0, 0, 3, 8, Color.White, Color.Black));

        var tabs = new TabControl("tabs", 0, 0, 20, 8, Color.White, Color.Black);
        tabs.AddTab("firstTab", "First");

        Assert.Throws<InvalidOperationException>(() => tabs.AddTab("firstTab", "Duplicate"));
        Assert.Throws<ArgumentOutOfRangeException>(() => tabs.SelectTab(2));

        var duplicatePage = new TabPage("duplicatePage", "Duplicate controls");
        duplicatePage.AddControl(new TextBox("duplicateControl", "", 0, 0, 5, Color.White, Color.Black));
        tabs.AddTab(duplicatePage);
        var rejectedPage = new TabPage("rejectedPage", "Rejected");
        rejectedPage.AddControl(new TextBox("duplicateControl", "", 0, 0, 5, Color.White, Color.Black));
        Assert.Throws<InvalidOperationException>(() => tabs.AddTab(rejectedPage));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
