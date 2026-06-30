using System.Drawing;
using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class RowDiffingBenchmarks
{
    private Screen screen = null!;
    private Label firstRowLabel = null!;
    private Label lastRowLabel = null!;
    private int counter;

    [GlobalSetup]
    public void Setup()
    {
        screen = new Screen(120, 60);
        for (short y = 0; y < 60; y++)
        {
            screen.TopContainer.AddControl(new Label(
                $"row-{y:00}",
                $"Row {y:00} baseline content for final-state comparison",
                0, y, 70,
                Color.White,
                Color.Blue));
        }

        firstRowLabel = (Label)screen.TopContainer.GetControl("row-00")!;
        lastRowLabel = (Label)screen.TopContainer.GetControl("row-59")!;
        screen.Render();
    }

    [Benchmark(Baseline = true)]
    public long RenderWithoutFinalStateChanges()
    {
        screen.Render();
        return screen.Revision;
    }

    [Benchmark]
    public long RenderAfterFirstRowChange()
    {
        firstRowLabel.Text = $"Top {counter++}";
        screen.Render();
        return screen.Revision;
    }

    [Benchmark]
    public long RenderAfterLastRowChange()
    {
        lastRowLabel.Text = $"Bottom {counter++}";
        screen.Render();
        return screen.Revision;
    }
}
