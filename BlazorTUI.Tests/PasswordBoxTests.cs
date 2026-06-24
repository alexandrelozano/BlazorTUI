using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class PasswordBoxTests
{
    [Fact]
    public void PasswordIsMaskedInTheCellBuffer()
    {
        var screen = new Screen(12, 3);
        var password = new PasswordBox("password", "secret", 0, 0, 10, Color.White, Color.Black);
        screen.TopContainer.AddControl(password);

        screen.Render();

        string renderedText = string.Concat(screen.Rows[0].Cells.Select(cell => cell.Character));
        Assert.StartsWith("••••••", renderedText);
        Assert.DoesNotContain("secret", renderedText);
        Assert.Equal("secret", password.Value);
    }

    [Fact]
    public void PasswordCanBeRevealedAndUsesACustomMask()
    {
        var screen = new Screen(12, 3);
        var password = new PasswordBox("password", "secret", 0, 0, 10, Color.White, Color.Black, '*');
        screen.TopContainer.AddControl(password);

        screen.Render();
        Assert.StartsWith("******", string.Concat(screen.Rows[0].Cells.Select(cell => cell.Character)));

        password.ToggleReveal();
        screen.Render();

        Assert.True(password.IsRevealed);
        Assert.StartsWith("secret", string.Concat(screen.Rows[0].Cells.Select(cell => cell.Character)));
    }

    [Fact]
    public void PasswordBoxRetainsTextEditingAndHistoryBehavior()
    {
        var password = new PasswordBox("password", "", 0, 0, 12, Color.White, Color.Black)
        {
            Value = "secret"
        };

        password.KeyDown("1", false);
        Assert.Equal("secret1", password.Value);
        Assert.True(password.Undo());
        Assert.Equal("secret", password.Value);
        Assert.True(password.Redo());
        Assert.Equal("secret1", password.Value);
    }

    [Fact]
    public void PasswordBoxBlocksCopyButAllowsPasteByDefault()
    {
        var password = new PasswordBox("password", "secret", 0, 0, 12, Color.White, Color.Black);

        Assert.False(password.AllowCopy);
        Assert.True(password.AllowPaste);
        Assert.Throws<ArgumentException>(() => password.MaskCharacter = '\n');
    }
}
