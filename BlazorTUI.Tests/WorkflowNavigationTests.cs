using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class WorkflowNavigationTests
{
    [Fact]
    public void FocusScopeConstrainsTabNavigationAcrossNestedContainers()
    {
        var screen = new Screen(40, 10);
        var scope = new Frame(
            "scope", "", 0, 0, 30, 8,
            Frame.BorderStyle.None, Color.White, Color.Black)
        {
            IsFocusScope = true
        };
        var left = new Container("left") { X = 0, Y = 0, Width = 12, Height = 4 };
        var right = new Container("right") { X = 12, Y = 0, Width = 12, Height = 4 };
        var first = new TextBox("first", "", 1, 1, 6, Color.White, Color.Black);
        var second = new TextBox("second", "", 1, 1, 6, Color.White, Color.Black);
        var outside = new TextBox("outside", "", 31, 1, 6, Color.White, Color.Black);

        left.AddControl(first);
        right.AddControl(second);
        scope.AddContainer(left);
        scope.AddContainer(right);
        screen.TopContainer.AddContainer(scope);
        screen.TopContainer.AddControl(outside);
        screen.SetFocus("first");

        screen.KeyDown("Tab", false);
        Assert.True(second.Focus);
        Assert.False(outside.Focus);

        screen.KeyDown("Tab", false);
        Assert.True(first.Focus);
        Assert.False(outside.Focus);

        Assert.True(scope.FocusLastControl());
        Assert.True(second.Focus);
        Assert.True(scope.FocusFirstControl());
        Assert.True(first.Focus);
    }

    [Fact]
    public void ValidationGroupsValidateOnlyMatchingControlsAndFocusFirstInvalid()
    {
        var screen = new Screen(40, 10);
        var shipping = new TextBox("shippingName", "", 1, 1, 12, Color.White, Color.Black)
        {
            IsRequired = true,
            ValidationGroup = "shipping",
            RequiredMessage = "Shipping required"
        };
        var billing = new TextBox("billingName", "", 1, 3, 12, Color.White, Color.Black)
        {
            IsRequired = true,
            ValidationGroup = "billing",
            RequiredMessage = "Billing required"
        };
        screen.TopContainer.AddControl(shipping);
        screen.TopContainer.AddControl(billing);

        Assert.False(screen.Validate("shipping"));
        Assert.True(shipping.HasValidationError);
        Assert.False(billing.HasValidationError);
        Assert.Same(shipping, screen.TopContainer.GetCurrentFocusControl());
        Assert.Equal(new[] { shipping }, screen.GetInvalidControls("shipping"));

        shipping.Value = "Alex";

        Assert.True(screen.Validate("shipping"));
        Assert.False(shipping.HasValidationError);
        Assert.False(billing.HasValidationError);
    }

    [Fact]
    public void WizardValidatesCurrentStepAndMovesFocusToNextStep()
    {
        var screen = new Screen(50, 16);
        var customerStep = new Frame(
            "customerStep", "CUSTOMER", 1, 1, 40, 10,
            Frame.BorderStyle.Line, Color.White, Color.Black);
        var addressStep = new Frame(
            "addressStep", "ADDRESS", 1, 1, 40, 10,
            Frame.BorderStyle.Line, Color.White, Color.Black);
        var name = new TextBox("customerName", "", 2, 2, 14, Color.White, Color.Black)
        {
            IsRequired = true,
            ValidationGroup = "customer",
            RequiredMessage = "Name required"
        };
        var street = new TextBox("street", "", 2, 2, 14, Color.White, Color.Black)
        {
            IsRequired = true,
            ValidationGroup = "address"
        };
        customerStep.AddControl(name);
        addressStep.AddControl(street);
        screen.TopContainer.AddContainer(customerStep);
        screen.TopContainer.AddContainer(addressStep);

        var wizard = new TuiWizard();
        wizard.AddStep("customer", customerStep, "customer");
        wizard.AddStep("address", addressStep, "address");

        Assert.True(customerStep.Visible);
        Assert.False(addressStep.Visible);

        Assert.False(wizard.MoveNext());
        Assert.Equal(0, wizard.CurrentIndex);
        Assert.True(customerStep.Visible);
        Assert.False(addressStep.Visible);
        Assert.Same(name, screen.TopContainer.GetCurrentFocusControl());

        name.Value = "Alex";

        Assert.True(wizard.MoveNext());
        Assert.Equal("address", wizard.CurrentStep?.Name);
        Assert.False(customerStep.Visible);
        Assert.True(addressStep.Visible);
        Assert.Same(street, screen.TopContainer.GetCurrentFocusControl());

        Assert.True(wizard.MovePrevious());
        Assert.True(customerStep.Visible);
        Assert.False(addressStep.Visible);
    }
}
