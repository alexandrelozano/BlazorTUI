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
        Assert.Contains("class=\"gridfs sizefs-40\"", html);
        Assert.Contains("data:image/png;base64,", html);
    }
}
