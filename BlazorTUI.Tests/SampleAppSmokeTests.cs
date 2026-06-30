using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace BlazorTUI.Tests;

public class SampleAppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SampleAppSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
        });
    }

    [Fact]
    public async Task HomePageStartsAndRendersBlazorTerminal()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/");
        string html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("<title>BlazorTUI examples</title>", html);
        Assert.Contains("_framework/blazor.server.js", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:80; --tui-rows:38", html);
        Assert.Contains("aria-label=\"BlazorTUI example catalog\"", html);
    }

    [Fact]
    public async Task ExampleCatalogRendersAsABlazorTerminal()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/examples");

        Assert.Contains("<title>BlazorTUI examples</title>", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:80; --tui-rows:38", html);
        Assert.Contains("aria-label=\"BlazorTUI example catalog\"", html);
    }

    [Fact]
    public async Task DocumentationSiteStartsAndLinksToRunnableExamples()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/docs");

        Assert.Contains("<title>BlazorTUI documentation</title>", html);
        Assert.Contains("BlazorTUI documentation site", html);
        Assert.Contains("Quick start", html);
        Assert.Contains("Runnable examples", html);
        Assert.Contains("Screenshot-ready previews", html);
        Assert.Contains("API notes", html);
        Assert.Contains("Migration guidance", html);
        Assert.Contains("href=\"/examples/grid-view\"", html);
        Assert.Contains("GridView", html);
        Assert.Contains("ExportCsv", html);
    }

    [Theory]
    [InlineData("/examples/controls-events", "Controls and events example")]
    [InlineData("/examples/additional-inputs", "Additional input controls example")]
    [InlineData("/examples/form-validation", "Form validation example")]
    [InlineData("/examples/data-form", "DataForm example")]
    [InlineData("/examples/grid-view", "GridView example")]
    [InlineData("/examples/data-visualizations", "Data visualizations example")]
    [InlineData("/examples/dialogs-menus", "Dialogs and menus example")]
    [InlineData("/examples/images", "Images example")]
    [InlineData("/examples/tabs", "TabControl example")]
    [InlineData("/examples/tree-view", "TreeView example")]
    [InlineData("/examples/sliders", "Slider example")]
    [InlineData("/examples/split-panels", "SplitPanel example")]
    [InlineData("/examples/stack-panel", "StackPanel example")]
    [InlineData("/examples/grid-panel", "GridPanel example")]
    [InlineData("/examples/dock-panel", "DockPanel example")]
    [InlineData("/examples/wrap-panel", "WrapPanel example")]
    [InlineData("/examples/scroll-viewer", "ScrollViewer example")]
    [InlineData("/examples/calendar", "Calendar example")]
    [InlineData("/examples/date-picker", "DatePicker example")]
    [InlineData("/examples/date-range-picker", "DateRangePicker example")]
    [InlineData("/examples/month-picker", "MonthPicker example")]
    [InlineData("/examples/breadcrumbs", "Breadcrumb example")]
    [InlineData("/examples/themes", "Themes example")]
    [InlineData("/examples/transient-ui", "Transient UI example")]
    public async Task FocusedExampleStartsAndRendersTerminal(string route, string title)
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync(route);

        Assert.Contains($"<title>{title}</title>", html);
        Assert.Contains("class=\"gridfs\"", html);
        string expectedSize = route == "/examples/grid-view"
            ? "--tui-columns:60; --tui-rows:32"
            : "--tui-columns:60; --tui-rows:30";
        Assert.Contains(expectedSize, html);
    }

    [Fact]
    public async Task ImageExampleEmbedsEncodedImage()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/examples/images");

        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public async Task CompleteShowcaseStartsAndEmbedsEncodedImage()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/examples/showcase");

        Assert.Contains("<title>Complete showcase</title>", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:80; --tui-rows:40", html);
        Assert.Contains("data:image/png;base64,", html);
    }
}
