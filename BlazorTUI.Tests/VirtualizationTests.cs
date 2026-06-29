using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class VirtualizationTests
{
    [Fact]
    public void ListBoxVirtualProviderRendersOnlyVisibleItemsAndSelectsByKey()
    {
        int getItemCalls = 0;
        var provider = new VirtualListBoxDataProvider(
            10_000,
            index =>
            {
                getItemCalls++;
                return $"Item {index:D4}";
            },
            index => $"item-{index}");
        var screen = new Screen(30, 8);
        var list = new ListBox("virtualList", provider, false, 1, 1, 14, 3, Color.White, Color.Black);
        screen.TopContainer.AddControl(list);

        screen.Render();

        Assert.True(list.IsVirtualized);
        Assert.Equal(10_000, list.ItemCount);
        Assert.Equal(3, getItemCalls);
        Assert.StartsWith(" Item 0000", Read(screen, 1, 1, 13));

        list.SelectKey("item-9999");
        getItemCalls = 0;
        screen.Render();

        Assert.Equal("item-9999", list.SelectedKey);
        Assert.Equal(3, getItemCalls);
        Assert.StartsWith("●Item 9999", Read(screen, 1, 3, 13));
    }

    [Fact]
    public void GridViewVirtualProviderKeepsNormalPagingWithoutMaterializingAllRows()
    {
        int getRowCalls = 0;
        var columns = new[]
        {
            new GridView.GridColumn { Title = "Order", Width = 8 },
            new GridView.GridColumn { Title = "Status", Width = 9 }
        };
        var provider = new VirtualGridViewDataProvider(
            50_000,
            index =>
            {
                getRowCalls++;
                return new GridView.GridRow { Cells = new[] { $"{index:D5}", index % 2 == 0 ? "Open" : "Done" } };
            },
            index => $"order-{index}");
        var screen = new Screen(34, 8);
        var grid = new GridView("virtualGrid", columns, provider, 1, 1, 20, 5, Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(grid);

        screen.Render();

        Assert.True(grid.IsVirtualized);
        Assert.Equal(50_000, grid.RowCount);
        Assert.Equal(50_000, grid.FilteredRowCount);
        Assert.Equal(16_667, grid.PageCount);
        Assert.InRange(getRowCalls, 1, 80);
        Assert.StartsWith("00000", Read(screen, 1, 3, 5));

        grid.GoToPage(2);
        getRowCalls = 0;
        screen.Render();

        Assert.Equal(2, grid.PageIndex);
        Assert.InRange(getRowCalls, 1, 80);
        Assert.StartsWith("00006", Read(screen, 1, 3, 5));
    }

    [Fact]
    public void CommandPaletteVirtualProviderFiltersAndRendersVisibleCommands()
    {
        int filteredCountCalls = 0;
        int getFilteredCalls = 0;
        var provider = new VirtualCommandPaletteDataProvider(
            () => 10_000,
            searchText =>
            {
                filteredCountCalls++;
                return searchText.Length == 0 ? 10_000 : 5;
            },
            (searchText, index) =>
            {
                getFilteredCalls++;
                return new CommandPaletteItem($"cmd-{searchText}-{index}", $"Command {searchText}{index:D2}");
            },
            name => name == "cmd--0" ? new CommandPaletteItem(name, "Command 00") : null);
        var screen = new Screen(40, 8);
        var palette = new CommandPalette("commands", provider, 1, 1, 26, Color.White, Color.Black, maxVisibleCommands: 3);
        screen.TopContainer.AddControl(palette);

        palette.Open();
        getFilteredCalls = 0;
        screen.Render();

        Assert.True(palette.IsVirtualized);
        Assert.Equal(10_000, palette.CommandCount);
        Assert.Equal(10_000, palette.FilteredCommandCount);
        Assert.True(filteredCountCalls > 0);
        Assert.Equal(3, getFilteredCalls);
        Assert.Throws<InvalidOperationException>(() => palette.AddCommand("extra", "Extra"));

        palette.SearchText = "999";
        getFilteredCalls = 0;
        screen.Render();

        Assert.Equal(5, palette.FilteredCommandCount);
        Assert.Equal(3, getFilteredCalls);
        Assert.StartsWith("Command 99900", Read(screen, 1, 2, 15));
    }

    [Fact]
    public void TreeViewVirtualProviderRendersOnlyVisibleNodesAndSelectsByKey()
    {
        int getNodeCalls = 0;
        var provider = new VirtualTreeViewDataProvider(
            () => 10_000,
            index =>
            {
                getNodeCalls++;
                return new VirtualTreeViewNode($"node-{index}", $"Node {index:D4}", depth: 0);
            },
            key => key.StartsWith("node-", StringComparison.Ordinal) &&
                int.TryParse(key["node-".Length..], out int index)
                    ? index
                    : -1);
        var screen = new Screen(30, 8);
        var tree = new TreeView("virtualTree", provider, 1, 1, 18, 4, Color.White, Color.Black);
        screen.TopContainer.AddControl(tree);

        getNodeCalls = 0;
        screen.Render();

        Assert.True(tree.IsVirtualized);
        Assert.Equal("node-0", tree.SelectedNodeKey);
        Assert.Equal(4, getNodeCalls);
        Assert.StartsWith("• Node 0000", Read(screen, 1, 1, 17));
        Assert.Throws<InvalidOperationException>(() => tree.AddNode("new", "New"));

        tree.SelectNodeKey("node-9999");
        getNodeCalls = 0;
        screen.Render();

        Assert.Equal("node-9999", tree.SelectedNodeKey);
        Assert.Equal(4, getNodeCalls);
        Assert.StartsWith("• Node 9999", Read(screen, 1, 4, 17));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
