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
    public async Task ListBoxOperationsProviderPushesSearchAndWindowQuery()
    {
        int getSourceItemCalls = 0;
        int getViewItemCalls = 0;
        int applyQueryCalls = 0;
        VirtualListBoxQuery? lastQuery = null;
        var provider = new VirtualListBoxDataOperationsProvider(
            10_000,
            index =>
            {
                getSourceItemCalls++;
                return $"Item {index:D4}";
            },
            query => query.HasSearch ? 100 : 10_000,
            (query, viewIndex) =>
            {
                getViewItemCalls++;
                int sourceIndex = query.HasSearch ? 99 + (viewIndex * 100) : viewIndex;
                return $"Item {sourceIndex:D4}";
            },
            getKey: index => $"item-{index}",
            getViewKey: (query, viewIndex) =>
            {
                int sourceIndex = query.HasSearch ? 99 + (viewIndex * 100) : viewIndex;
                return $"item-{sourceIndex}";
            },
            findViewIndexByKey: (query, key) =>
            {
                if (!key.StartsWith("item-", StringComparison.Ordinal) ||
                    !int.TryParse(key["item-".Length..], out int sourceIndex))
                {
                    return -1;
                }

                return query.HasSearch
                    ? sourceIndex >= 99 && (sourceIndex - 99) % 100 == 0 ? (sourceIndex - 99) / 100 : -1
                    : sourceIndex;
            },
            applyQuery: query =>
            {
                applyQueryCalls++;
                lastQuery = query;
            },
            applyQueryAsync: (query, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                applyQueryCalls++;
                lastQuery = query;
                return Task.CompletedTask;
            });
        var screen = new Screen(30, 8);
        var list = new ListBox("virtualList", provider, false, 1, 1, 14, 3, Color.White, Color.Black);
        screen.TopContainer.AddControl(list);

        list.SearchText = "99";
        await list.RefreshVirtualQueryAsync();
        getViewItemCalls = 0;
        screen.Render();

        Assert.Equal(100, list.ItemCount);
        Assert.Equal(0, getSourceItemCalls);
        Assert.Equal(3, getViewItemCalls);
        Assert.NotNull(lastQuery);
        Assert.Equal("99", lastQuery.SearchText);
        Assert.Equal(0, lastQuery.PageIndex);
        Assert.Equal(3, lastQuery.PageSize);
        Assert.True(applyQueryCalls > 0);
        Assert.StartsWith(" Item 0099", Read(screen, 1, 1, 13));

        list.SelectKey("item-199");

        Assert.Equal("item-199", list.SelectedKey);
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
    public async Task GridViewOperationsProviderPushesFilterSortAndPagingQuery()
    {
        int getSourceRowCalls = 0;
        int getViewRowCalls = 0;
        int applyQueryCalls = 0;
        VirtualGridViewQuery? lastQuery = null;
        var columns = new[]
        {
            new GridView.GridColumn { Title = "Order", Width = 8 },
            new GridView.GridColumn { Title = "Status", Width = 9 }
        };
        var provider = new VirtualGridViewDataOperationsProvider(
            50_000,
            index =>
            {
                getSourceRowCalls++;
                return CreateOrderRow(index);
            },
            QueryCount,
            (query, viewIndex) =>
            {
                getViewRowCalls++;
                return CreateOrderRow(MapSourceIndex(query, viewIndex));
            },
            getRowKey: index => $"order-{index}",
            getViewRowKey: (query, viewIndex) => $"order-{MapSourceIndex(query, viewIndex)}",
            getSourceIndex: MapSourceIndex,
            findViewIndexBySourceIndex: FindViewIndex,
            findViewIndexByKey: (query, key) =>
            {
                if (!key.StartsWith("order-", StringComparison.Ordinal) ||
                    !int.TryParse(key["order-".Length..], out int sourceIndex))
                {
                    return -1;
                }

                return FindViewIndex(query, sourceIndex);
            },
            applyQuery: query =>
            {
                applyQueryCalls++;
                lastQuery = query;
            },
            applyQueryAsync: (query, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                applyQueryCalls++;
                lastQuery = query;
                return Task.CompletedTask;
            });
        var screen = new Screen(34, 8);
        var grid = new GridView("virtualGrid", columns, provider, 1, 1, 20, 5, Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(grid);

        grid.SetExactFilter(1, "Open");
        grid.SortByColumn(0, GridSortDirection.Descending);
        grid.GoToPage(1);
        await grid.RefreshVirtualQueryAsync();
        getViewRowCalls = 0;
        screen.Render();

        Assert.Equal(25_000, grid.FilteredRowCount);
        Assert.Equal(0, getSourceRowCalls);
        Assert.InRange(getViewRowCalls, 1, 12);
        Assert.NotNull(lastQuery);
        Assert.Equal(1, lastQuery.PageIndex);
        Assert.Equal(3, lastQuery.PageSize);
        Assert.Equal(0, lastQuery.SortColumnIndex);
        Assert.Equal(GridSortDirection.Descending, lastQuery.SortDirection);
        VirtualGridViewColumnFilter filter = Assert.Single(lastQuery.ColumnFilters);
        Assert.Equal(1, filter.ColumnIndex);
        Assert.Equal(GridViewFilterKind.Exact, filter.Kind);
        Assert.Equal("Open", Assert.Single(filter.Values));
        Assert.True(applyQueryCalls > 0);
        Assert.Equal("49992", grid.CurrentPageRows[0].Cells[0]);
        Assert.StartsWith("49992", Read(screen, 1, 3, 5));

        grid.SelectRowKey("order-49990");

        Assert.Equal("order-49990", grid.SelectedRowKey);

        static GridView.GridRow CreateOrderRow(int sourceIndex)
            => new() { Cells = new[] { $"{sourceIndex:D5}", sourceIndex % 2 == 0 ? "Open" : "Done" } };

        static bool HasOpenFilter(VirtualGridViewQuery query)
            => query.ColumnFilters.Any(filter =>
                filter.ColumnIndex == 1 &&
                filter.Kind == GridViewFilterKind.Exact &&
                filter.Values.Contains("Open"));

        static int QueryCount(VirtualGridViewQuery query)
            => HasOpenFilter(query) ? 25_000 : 50_000;

        static int MapSourceIndex(VirtualGridViewQuery query, int viewIndex)
        {
            int count = QueryCount(query);
            if (viewIndex < 0 || viewIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(viewIndex));

            int normalizedIndex = query.SortColumnIndex == 0 &&
                query.SortDirection == GridSortDirection.Descending
                    ? count - 1 - viewIndex
                    : viewIndex;
            return HasOpenFilter(query) ? normalizedIndex * 2 : normalizedIndex;
        }

        static int FindViewIndex(VirtualGridViewQuery query, int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= 50_000)
                return -1;
            if (HasOpenFilter(query) && sourceIndex % 2 != 0)
                return -1;

            int normalizedIndex = HasOpenFilter(query) ? sourceIndex / 2 : sourceIndex;
            int count = QueryCount(query);
            return query.SortColumnIndex == 0 && query.SortDirection == GridSortDirection.Descending
                ? count - 1 - normalizedIndex
                : normalizedIndex;
        }
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
        Assert.Equal("999", provider.CurrentQuery.SearchText);
        Assert.Equal(0, provider.CurrentQuery.ScrollIndex);
        Assert.Equal(3, provider.CurrentQuery.VisibleCount);
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
        Assert.Equal(0, provider.CurrentQuery.ScrollIndex);
        Assert.Equal(4, provider.CurrentQuery.VisibleCount);
        Assert.Equal(4, getNodeCalls);
        Assert.StartsWith("• Node 0000", Read(screen, 1, 1, 17));
        Assert.Throws<InvalidOperationException>(() => tree.AddNode("new", "New"));

        tree.SelectNodeKey("node-9999");
        getNodeCalls = 0;
        screen.Render();

        Assert.Equal("node-9999", tree.SelectedNodeKey);
        Assert.Equal(9996, provider.CurrentQuery.ScrollIndex);
        Assert.Equal(4, provider.CurrentQuery.VisibleCount);
        Assert.Equal(4, getNodeCalls);
        Assert.StartsWith("• Node 9999", Read(screen, 1, 4, 17));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
