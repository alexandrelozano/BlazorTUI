using System.Drawing;
using Bunit;
using BlazorTUI.TUI;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorTUI.Tests;

public class BlazorTUIComponentTests : BunitContext
{
    [Fact]
    public void ComponentRendersOneTilePerCell()
    {
        var screen = new Screen(4, 2);
        screen.topContainer.AddControl(new Label("label", "OK", 0, 0, 2, Color.White, Color.Black));

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        Assert.Equal(8, component.FindAll(".tilefs").Count);
        Assert.Contains(">O</div>", component.Markup);
        Assert.Contains(">K</div>", component.Markup);
    }

    [Fact]
    public void KeyDownMovesFocusAndClickInvokesControl()
    {
        var screen = new Screen(8, 4);
        var first = new TextBox("first", "", 0, 0, 4, Color.White, Color.Black);
        var second = new TextBox("second", "", 0, 1, 4, Color.White, Color.Black);
        bool clicked = false;
        var button = new Button("button", "Go", 0, 2, 4, Color.White, Color.DarkGreen)
        {
            OnClick = _ => clicked = true
        };
        screen.topContainer.AddControl(first);
        screen.topContainer.AddControl(second);
        screen.topContainer.AddControl(button);
        screen.SetFocus("first");

        var component = Render<global::BlazorTUI.BlazorTUI>(parameters =>
            parameters.Add(instance => instance.screen, screen));

        component.Find(".gridfs").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.True(second.Focus);

        component.FindAll(".tilefs")[16].Click();
        Assert.True(clicked);
    }
}
