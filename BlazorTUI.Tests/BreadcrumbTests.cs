using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class BreadcrumbTests
{
    [Fact]
    public void RendersPathAndHighlightsSelectedItem()
    {
        var screen = new Screen(40, 5);
        var breadcrumb = new Breadcrumb(
            "path",
            new[]
            {
                new BreadcrumbItem("home", "Home", "/"),
                new BreadcrumbItem("docs", "Docs", "/docs"),
                new BreadcrumbItem("api", "API", "/docs/api")
            },
            1, 1, 24,
            Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(breadcrumb);

        screen.Render();

        Assert.StartsWith("Home / Docs / API", Read(screen, 1, 1, 24));
        Assert.Equal(2, breadcrumb.SelectedIndex);
        Assert.Equal("/docs/api", breadcrumb.SelectedValue);
        Assert.Equal(Cell.TextDecoration.UnderLine, screen.Rows[1].Cells[15].Decoration);

        screen.SetFocus("path");
        screen.Render();

        Assert.Equal(Color.Black, screen.Rows[1].Cells[15].ForeColor);
        Assert.Equal(Color.Yellow, screen.Rows[1].Cells[15].BackgroundColor);
        Assert.Equal(Cell.TextDecoration.None, screen.Rows[1].Cells[15].Decoration);
    }

    [Fact]
    public void KeyboardNavigationSelectsItemsAndActivatesCurrentItem()
    {
        var breadcrumb = new Breadcrumb(
            "path",
            new[] { "Home", "Docs", "API" },
            0, 0, 24,
            Color.White, Color.Black);
        var changes = new List<(int Previous, int Current, string? PreviousText, string? CurrentText)>();
        var activated = new List<string>();
        breadcrumb.SelectionChanged += (_, args) =>
            changes.Add((
                args.PreviousSelectedIndex,
                args.SelectedIndex,
                args.PreviousSelectedItem?.Text,
                args.SelectedItem?.Text));
        breadcrumb.ItemActivated += (_, args) => activated.Add(args.ItemName);

        breadcrumb.KeyDown("ArrowLeft", false);
        breadcrumb.KeyDown("Home", false);
        breadcrumb.KeyDown("ArrowRight", false);
        breadcrumb.KeyDown("Enter", false);

        Assert.Equal(1, breadcrumb.SelectedIndex);
        var expectedChanges = new List<(int Previous, int Current, string? PreviousText, string? CurrentText)>
        {
            (2, 1, "API", "Docs"),
            (1, 0, "Docs", "Home"),
            (0, 1, "Home", "Docs")
        };
        Assert.Equal(expectedChanges, changes);
        Assert.Equal(new[] { "item1" }, activated);
    }

    [Fact]
    public void MouseClickSelectsAndActivatesVisibleItem()
    {
        var screen = new Screen(36, 5);
        var breadcrumb = new Breadcrumb(
            "path",
            new[]
            {
                new BreadcrumbItem("home", "Home"),
                new BreadcrumbItem("orders", "Orders"),
                new BreadcrumbItem("details", "Details")
            },
            1, 1, 28,
            Color.White, Color.Black);
        string activated = "";
        int clicks = 0;
        breadcrumb.ItemActivated += (_, args) => activated = args.ItemName;
        breadcrumb.Clicked += (_, _) => clicks++;
        screen.TopContainer.AddControl(breadcrumb);

        screen.TopContainer.Click(8, 1);

        Assert.Equal("orders", activated);
        Assert.Equal(1, breadcrumb.SelectedIndex);
        Assert.True(breadcrumb.Focus);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void ClipsOverflowFromLeftAndKeepsLatestItemsVisible()
    {
        var screen = new Screen(24, 4);
        var breadcrumb = new Breadcrumb(
            "path",
            new[] { "Workspace", "Project", "Settings", "Users" },
            1, 1, 18,
            Color.White, Color.Black);
        screen.TopContainer.AddControl(breadcrumb);

        screen.Render();

        Assert.Equal("… Settings / Users", Read(screen, 1, 1, 18));
    }

    [Fact]
    public void SupportsItemMutationAndSelectionHelpers()
    {
        var breadcrumb = new Breadcrumb(
            "path",
            Array.Empty<BreadcrumbItem>(),
            0, 0, 20,
            Color.White, Color.Black);
        int changes = 0;
        breadcrumb.SelectedIndexChanged += (_, _) => changes++;

        breadcrumb.AddItem("home", "Home", "/");
        breadcrumb.AddItem("docs", "Docs", "/docs");
        breadcrumb.AddItem("api", "API", "/docs/api");
        breadcrumb.SelectValue("/docs");
        Assert.Equal("docs", breadcrumb.GetItem("docs")?.Name);
        breadcrumb.SelectItem("api");
        Assert.True(breadcrumb.ActivateItem("home"));
        Assert.True(breadcrumb.RemoveItem("docs"));
        breadcrumb.ClearItems();

        Assert.Empty(breadcrumb.Items);
        Assert.Equal(-1, breadcrumb.SelectedIndex);
        Assert.Null(breadcrumb.SelectedItem);
        Assert.Equal(5, changes);
    }

    [Fact]
    public void ValidatesArgumentsAndDuplicateItems()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Breadcrumb("path", new[] { "Home" }, 0, 0, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Breadcrumb("path", new[] { "Home" }, 0, 0, 10, Color.White, Color.Black, selectedIndex: 2));
        Assert.Throws<ArgumentException>(() =>
            new BreadcrumbItem("bad", "One\nTwo"));
        Assert.Throws<InvalidOperationException>(() =>
            new Breadcrumb(
                "path",
                new[]
                {
                    new BreadcrumbItem("duplicate", "One"),
                    new BreadcrumbItem("duplicate", "Two")
                },
                0, 0, 10,
                Color.White, Color.Black));

        var breadcrumb = new Breadcrumb("path", new[] { "Home" }, 0, 0, 10, Color.White, Color.Black);

        Assert.Throws<ArgumentOutOfRangeException>(() => breadcrumb.SelectedIndex = 3);
        Assert.Throws<ArgumentException>(() => breadcrumb.SelectItem("missing"));
        Assert.Throws<ArgumentException>(() => breadcrumb.SelectValue("missing"));
        Assert.Throws<ArgumentException>(() => breadcrumb.GetItem(" "));
        Assert.Throws<InvalidOperationException>(() => breadcrumb.AddItem("item0", "Duplicate"));
        Assert.Throws<ArgumentNullException>(() => breadcrumb.AddItem(null!));
        Assert.Throws<ArgumentNullException>(() => breadcrumb.RemoveItem((BreadcrumbItem)null!));
        Assert.Throws<ArgumentException>(() => breadcrumb.Separator = "\n");
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
