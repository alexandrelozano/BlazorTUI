using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class StatePersistenceBenchmarks
{
    private Screen screen = null!;
    private TuiScreenState state = null!;
    private string stateJson = "";

    [GlobalSetup]
    public void Setup()
    {
        screen = BenchmarkDataFactory.CreateStateScreen();
        screen.Render();

        GridView grid = (GridView)screen.TopContainer.GetControl("stateGrid")!;
        grid.SetExactFilter(2, "Cooking");
        grid.SortByColumn(1, GridSortDirection.Ascending);
        grid.SelectSourceRow(250);

        TreeView tree = (TreeView)screen.TopContainer.GetControl("stateTree")!;
        tree.SelectNode("root-10-child-3");

        state = screen.ExportState();
        stateJson = state.ToJson(indented: false);
    }

    [Benchmark(Baseline = true)]
    public int ExportLargeState()
    {
        TuiScreenState exported = screen.ExportState();
        return exported.Controls.Count;
    }

    [Benchmark]
    public int ExportLargeStateJson()
    {
        string json = screen.ExportStateJson();
        return json.Length;
    }

    [Benchmark]
    public int RestoreLargeState()
    {
        screen.RestoreState(state);
        return screen.TopContainer.GetInvalidControls().Count;
    }

    [Benchmark]
    public int RestoreLargeStateJson()
    {
        screen.RestoreStateJson(stateJson);
        return screen.TopContainer.GetInvalidControls().Count;
    }
}
