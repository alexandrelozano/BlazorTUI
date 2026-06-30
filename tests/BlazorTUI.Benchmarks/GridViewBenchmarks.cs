using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class GridViewBenchmarks
{
    private Screen materializedScreen = null!;
    private GridView materializedGrid = null!;
    private Screen virtualScreen = null!;

    [GlobalSetup]
    public void Setup()
    {
        materializedScreen = new Screen(90, 24);
        materializedGrid = BenchmarkDataFactory.CreateMaterializedGridView(
            "orders",
            1, 1, 72, 18,
            rowCount: 10_000,
            pageSize: 15);
        materializedScreen.TopContainer.AddControl(materializedGrid);

        virtualScreen = new Screen(90, 24);
        virtualScreen.TopContainer.AddControl(BenchmarkDataFactory.CreateVirtualGridView(
            "virtualOrders",
            1, 1, 72, 18,
            rowCount: 250_000,
            pageSize: 15));
    }

    [Benchmark(Baseline = true)]
    public long RenderMaterializedGridPage()
    {
        materializedScreen.Render();
        return materializedScreen.Revision;
    }

    [Benchmark]
    public long RenderVirtualGridPage()
    {
        virtualScreen.Render();
        return virtualScreen.Revision;
    }

    [Benchmark]
    public int SortMaterializedGrid()
    {
        materializedGrid.ClearSort();
        materializedGrid.SortByColumn(1, GridSortDirection.Ascending);
        materializedScreen.Render();
        return materializedGrid.SelectedSourceRowIndex;
    }

    [Benchmark]
    public int FilterMaterializedGrid()
    {
        materializedGrid.ClearFilters();
        materializedGrid.SetExactFilter(2, "Cooking");
        materializedScreen.Render();
        return materializedGrid.FilteredRowCount;
    }
}
