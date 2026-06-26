using Bunit;
using BlazorTUI.TUI;
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
    [InlineData("controlsExample", "/examples/controls-events")]
    [InlineData("dialogsExample", "/examples/dialogs-menus")]
    [InlineData("imagesExample", "/examples/images")]
    [InlineData("tabsExample", "/examples/tabs")]
    [InlineData("treeExample", "/examples/tree-view")]
    [InlineData("sliderExample", "/examples/sliders")]
    [InlineData("splitExample", "/examples/split-panels")]
    [InlineData("breadcrumbExample", "/examples/breadcrumbs")]
    [InlineData("themesExample", "/examples/themes")]
    [InlineData("showcaseExample", "/")]
    public void CatalogButtonsNavigateToTheirExample(string controlName, string expectedPath)
    {
        var component = Render<global::SampleApp.Pages.Examples.Index>();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IRenderedComponent<global::BlazorTUI.BlazorTUI> terminal =
            component.FindComponent<global::BlazorTUI.BlazorTUI>();
        Button button = Assert.IsType<Button>(terminal.Instance.screen.TopContainer.GetControl(controlName));

        button.Click(0, 0);

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
