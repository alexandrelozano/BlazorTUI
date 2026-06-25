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
        Assert.Contains("_framework/blazor.server.js", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:80; --tui-rows:40", html);
        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public async Task ExampleCatalogRendersAsABlazorTerminal()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/examples");

        Assert.Contains("<title>BlazorTUI examples</title>", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:60; --tui-rows:30", html);
        Assert.Contains("aria-label=\"BlazorTUI example catalog\"", html);
    }

    [Theory]
    [InlineData("/examples/controls-events", "Controls and events example")]
    [InlineData("/examples/dialogs-menus", "Dialogs and menus example")]
    [InlineData("/examples/images", "Images example")]
    [InlineData("/examples/tabs", "TabControl example")]
    [InlineData("/examples/tree-view", "TreeView example")]
    [InlineData("/examples/sliders", "Slider example")]
    [InlineData("/examples/split-panels", "SplitPanel example")]
    public async Task FocusedExampleStartsAndRendersTerminal(string route, string title)
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync(route);

        Assert.Contains($"<title>{title}</title>", html);
        Assert.Contains("class=\"gridfs\"", html);
        Assert.Contains("--tui-columns:60; --tui-rows:30", html);
    }

    [Fact]
    public async Task ImageExampleEmbedsEncodedImage()
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync("/examples/images");

        Assert.Contains("data:image/png;base64,", html);
    }
}
