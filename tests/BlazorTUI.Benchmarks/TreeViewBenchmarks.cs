using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class TreeViewBenchmarks
{
    private Screen materializedScreen = null!;
    private TreeView materializedTree = null!;
    private Screen virtualScreen = null!;
    private TreeView virtualTree = null!;
    private int selectedIndex;

    [GlobalSetup]
    public void Setup()
    {
        materializedScreen = new Screen(70, 30);
        materializedTree = BenchmarkDataFactory.CreateMaterializedTreeView(
            "tree",
            1, 1, 50, 24,
            rootCount: 250,
            childCount: 8);
        materializedTree.ExpandAll();
        materializedScreen.TopContainer.AddControl(materializedTree);

        virtualScreen = new Screen(70, 30);
        virtualTree = BenchmarkDataFactory.CreateVirtualTreeView(
            "virtualTree",
            1, 1, 50, 24,
            visibleCount: 100_000);
        virtualScreen.TopContainer.AddControl(virtualTree);
    }

    [Benchmark(Baseline = true)]
    public long RenderMaterializedTree()
    {
        materializedScreen.Render();
        return materializedScreen.Revision;
    }

    [Benchmark]
    public long RenderVirtualTree()
    {
        virtualScreen.Render();
        return virtualScreen.Revision;
    }

    [Benchmark]
    public string SelectVirtualTreeNodeByKey()
    {
        selectedIndex = (selectedIndex + 997) % 100_000;
        string key = $"node-{selectedIndex:000000}";
        virtualTree.SelectNodeKey(key);
        virtualScreen.Render();
        return virtualTree.SelectedNodeKey ?? "";
    }
}
