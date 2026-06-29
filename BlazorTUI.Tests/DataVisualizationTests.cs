using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class DataVisualizationTests
{
    [Fact]
    public void SparklineRendersScaledGlyphs()
    {
        var screen = new Screen(30, 6);
        var sparkline = new Sparkline(
            "trend",
            new[] { 0.0, 5.0, 10.0 },
            2, 2, 3,
            Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(sparkline);

        screen.Render();

        Assert.Equal("▁▅█", Read(screen, 2, 2, 3));
    }

    [Fact]
    public void BarChartRendersHorizontalBarsAndRejectsDuplicateNames()
    {
        var screen = new Screen(40, 8);
        var chart = new BarChart(
            "orders",
            new[]
            {
                new BarChartItem("open", "Open", 5),
                new BarChartItem("ready", "Ready", 10)
            },
            2, 2, 18, 2,
            Color.Yellow, Color.Black)
        {
            LabelWidth = 6
        };
        screen.TopContainer.AddControl(chart);

        screen.Render();

        Assert.StartsWith("Open", Read(screen, 2, 2, 18));
        Assert.Contains("█", Read(screen, 8, 2, 12));
        Assert.Throws<InvalidOperationException>(() => chart.AddItem(new BarChartItem("open", "Open", 1)));
    }

    [Fact]
    public void BarChartCanRenderVerticalBars()
    {
        var screen = new Screen(20, 8);
        var chart = new BarChart(
            "orders",
            new[]
            {
                new BarChartItem("low", "Low", 1),
                new BarChartItem("high", "High", 3)
            },
            2, 2, 2, 3,
            Color.Yellow, Color.Black,
            BarChartOrientation.Vertical);
        screen.TopContainer.AddControl(chart);

        screen.Render();

        Assert.Equal(" █", Read(screen, 2, 2, 2));
        Assert.Equal("██", Read(screen, 2, 4, 2));
    }

    [Fact]
    public void GaugeClampsValueAndRendersPercentage()
    {
        var screen = new Screen(30, 6);
        var gauge = new Gauge(
            "cpu",
            0, 100, 150,
            2, 2, 10,
            Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(gauge);

        screen.Render();

        Assert.Equal(100, gauge.Value);
        Assert.Contains("100%", Read(screen, 2, 2, 10));
    }

    [Fact]
    public void TimelineAndKeyValueListRenderText()
    {
        var screen = new Screen(50, 10);
        var timeline = new Timeline(
            "releaseTimeline",
            new[]
            {
                new TimelineItem("plan", "Planned"),
                new TimelineItem("ship", "Shipped")
            },
            2, 2, 14, 3,
            Color.Cyan, Color.Black);
        var details = new KeyValueList(
            "details",
            new[]
            {
                new KeyValueListItem("status", "Status", "Ready"),
                new KeyValueListItem("owner", "Owner", "Ops")
            },
            20, 2, 20, 2,
            Color.White, Color.Black,
            keyWidth: 7);
        screen.TopContainer.AddControl(timeline);
        screen.TopContainer.AddControl(details);

        screen.Render();

        Assert.Contains("Planned", Read(screen, 2, 2, 14));
        Assert.Contains("Status", Read(screen, 20, 2, 20));
        Assert.Contains("Ready", Read(screen, 20, 2, 20));
    }

    [Fact]
    public void StatePersistenceRestoresVisualizationValues()
    {
        var screen = new Screen(60, 12);
        var sparkline = new Sparkline("trend", new[] { 1.0, 2.0 }, 0, 0, 8, Color.Yellow, Color.Black);
        var chart = new BarChart(
            "chart",
            new[] { new BarChartItem("a", "A", 1) },
            0, 1, 10, 1,
            Color.Yellow, Color.Black);
        var gauge = new Gauge("gauge", 0, 10, 5, 0, 2, 8, Color.Yellow, Color.Black);
        var timeline = new Timeline("timeline", new[] { new TimelineItem("a", "Alpha") }, 0, 3, 12, 1, Color.Yellow, Color.Black);
        var keyValues = new KeyValueList("kv", new[] { new KeyValueListItem("a", "A", "1") }, 0, 4, 12, 1, Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(sparkline);
        screen.TopContainer.AddControl(chart);
        screen.TopContainer.AddControl(gauge);
        screen.TopContainer.AddControl(timeline);
        screen.TopContainer.AddControl(keyValues);

        TuiScreenState state = screen.ExportState();

        var restoredScreen = new Screen(60, 12);
        var restoredSparkline = new Sparkline("trend", Array.Empty<double>(), 0, 0, 8, Color.Yellow, Color.Black);
        var restoredChart = new BarChart("chart", Array.Empty<BarChartItem>(), 0, 1, 10, 1, Color.Yellow, Color.Black);
        var restoredGauge = new Gauge("gauge", 0, 100, 0, 0, 2, 8, Color.Yellow, Color.Black);
        var restoredTimeline = new Timeline("timeline", Array.Empty<TimelineItem>(), 0, 3, 12, 1, Color.Yellow, Color.Black);
        var restoredKeyValues = new KeyValueList("kv", Array.Empty<KeyValueListItem>(), 0, 4, 12, 1, Color.Yellow, Color.Black);
        restoredScreen.TopContainer.AddControl(restoredSparkline);
        restoredScreen.TopContainer.AddControl(restoredChart);
        restoredScreen.TopContainer.AddControl(restoredGauge);
        restoredScreen.TopContainer.AddControl(restoredTimeline);
        restoredScreen.TopContainer.AddControl(restoredKeyValues);

        restoredScreen.RestoreState(state);

        Assert.Equal(new[] { 1.0, 2.0 }, restoredSparkline.Values);
        Assert.Equal("a", restoredChart.Items.Single().Name);
        Assert.Equal(5, restoredGauge.Value);
        Assert.Equal("Alpha", restoredTimeline.Items.Single().Text);
        Assert.Equal("1", restoredKeyValues.Items.Single().Value);
    }

    [Fact]
    public void ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Sparkline("trend", Array.Empty<double>(), 0, 0, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BarChartItem("item", "Item", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Gauge("gauge", 10, 0, 5, 0, 0, 8, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Timeline("timeline", Array.Empty<TimelineItem>(), 0, 0, 2, 1, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KeyValueList("kv", Array.Empty<KeyValueListItem>(), 0, 0, 0, 1, Color.White, Color.Black));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
