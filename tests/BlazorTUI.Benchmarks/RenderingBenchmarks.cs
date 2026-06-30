using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class RenderingBenchmarks
{
    private Screen staticScreen = null!;
    private Screen changingScreen = null!;
    private StatusBar changingStatus = null!;
    private int counter;

    [GlobalSetup]
    public void Setup()
    {
        staticScreen = BenchmarkDataFactory.CreateShowcaseScreen();
        changingScreen = BenchmarkDataFactory.CreateShowcaseScreen();
        changingStatus = (StatusBar)changingScreen.TopContainer.GetControl("status")!;

        staticScreen.Render();
        changingScreen.Render();
    }

    [Benchmark(Baseline = true)]
    public long RenderUnchangedScreen()
    {
        staticScreen.Render();
        return staticScreen.Revision;
    }

    [Benchmark]
    public long RenderAfterSingleControlChange()
    {
        changingStatus.Message = $"Tick {counter++}";
        changingScreen.Render();
        return changingScreen.Revision;
    }
}
