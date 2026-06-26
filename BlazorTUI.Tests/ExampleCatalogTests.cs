using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTUI.Tests;

public class ExampleCatalogTests : BunitContext
{
    public ExampleCatalogTests()
    {
        BunitJSModuleInterop keyboardModule = JSInterop.SetupModule("./_content/BlazorTUI/blazorTui.js");
        keyboardModule.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void CatalogUsesBlazorTuiAndListsEveryDestination()
    {
        var component = Render<global::SampleApp.Pages.Examples.Index>();
        string accessibleText = component.Find("pre.blazortui-visually-hidden").TextContent;

        Assert.Contains("BLAZORTUI EXAMPLES", accessibleText);
        Assert.Contains("Controls and events", accessibleText);
        Assert.Contains("Dialogs and menus", accessibleText);
        Assert.Contains("Images", accessibleText);
        Assert.Contains("TabControl", accessibleText);
        Assert.Contains("TreeView", accessibleText);
        Assert.Contains("Slider", accessibleText);
        Assert.Contains("SplitPanel", accessibleText);
        Assert.Contains("Breadcrumb", accessibleText);
        Assert.Contains("Themes", accessibleText);
        Assert.Contains("Complete showcase", accessibleText);
    }

    [Theory]
    [InlineData(0, "/examples/controls-events")]
    [InlineData(1, "/examples/dialogs-menus")]
    [InlineData(2, "/examples/images")]
    [InlineData(3, "/examples/tabs")]
    [InlineData(4, "/examples/tree-view")]
    [InlineData(5, "/examples/sliders")]
    [InlineData(6, "/examples/split-panels")]
    [InlineData(7, "/examples/breadcrumbs")]
    [InlineData(8, "/examples/themes")]
    [InlineData(9, "/")]
    public void CatalogButtonsNavigateToTheirExample(int buttonIndex, string expectedPath)
    {
        var component = Render<global::SampleApp.Pages.Examples.Index>();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        int x = 9;
        int y = 8 + buttonIndex * 2;
        int cellIndex = y * 60 + x;

        component.FindAll(".tilefs")[cellIndex].Click();

        Assert.Equal(expectedPath, new Uri(navigation.Uri).AbsolutePath);
    }

    [Fact]
    public void FocusedCatalogButtonNavigatesWithEnter()
    {
        var component = Render<global::SampleApp.Pages.Examples.Index>();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();

        component.Find(".gridfs").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("/examples/controls-events", new Uri(navigation.Uri).AbsolutePath);
    }
}
