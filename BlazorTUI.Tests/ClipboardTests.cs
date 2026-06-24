using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class ClipboardTests
{
    [Fact]
    public void TextBoxSupportsSelectionCutAndPaste()
    {
        var textBox = new TextBox("name", "hello", 0, 0, 8, Color.White, Color.Black);

        textBox.SelectAll();
        Assert.True(textBox.HasSelection);
        Assert.Equal((short)0, textBox.SelectionStart);
        Assert.Equal((short)5, textBox.SelectionLength);
        Assert.Equal("hello", textBox.SelectedText);
        textBox.Paste("");
        Assert.Equal("hello", textBox.Value);
        Assert.True(textBox.HasSelection);
        Assert.Equal("hello", textBox.CutSelection());
        Assert.Equal("", textBox.Value);

        textBox.Paste($"ab{Environment.NewLine}cdefgh");

        Assert.Equal("ab cdef", textBox.Value);
        Assert.False(textBox.HasSelection);
    }

    [Fact]
    public void TextBoxReplacesKeyboardSelection()
    {
        var textBox = new TextBox("name", "abcd", 0, 0, 8, Color.White, Color.Black)
        {
            Value = "abcd"
        };

        textBox.KeyDown("ArrowLeft", true);
        textBox.KeyDown("ArrowLeft", true);

        Assert.Equal("cd", textBox.SelectedText);
        textBox.KeyDown("Z", false);
        Assert.Equal("abZ", textBox.Value);
    }

    [Fact]
    public void TextAreaSupportsMultilineClipboardOperations()
    {
        string original = $"one{Environment.NewLine}two";
        var textArea = new TextArea("notes", original, 0, 0, 10, 4, 8, 3, Color.White, Color.Black);

        textArea.SelectAll();
        Assert.Equal(original, textArea.SelectedText);
        Assert.Equal(original, textArea.CutSelection());
        Assert.Equal("", textArea.Value);

        textArea.Paste($"123456789{Environment.NewLine}second{Environment.NewLine}third{Environment.NewLine}ignored");

        Assert.Equal(
            $"12345678{Environment.NewLine}second{Environment.NewLine}third",
            textArea.Value);
        Assert.False(textArea.HasSelection);
    }

    [Fact]
    public void TextAreaSelectsAcrossLinesWithShiftNavigation()
    {
        var textArea = new TextArea(
            "notes", $"ab{Environment.NewLine}cd", 0, 0, 10, 4, 8, 3, Color.White, Color.Black);

        textArea.KeyDown("ArrowDown", true);

        Assert.Equal($"ab{Environment.NewLine}", textArea.SelectedText);
        textArea.Paste("Z");
        Assert.Equal("Zcd", textArea.Value);
    }

    [Fact]
    public void SelectedTextUsesInvertedColorsWhenRendered()
    {
        var screen = new Screen(8, 3);
        var textBox = new TextBox("name", "abc", 0, 0, 5, Color.Yellow, Color.DarkBlue);
        screen.TopContainer.AddControl(textBox);
        screen.SetFocus("name");
        textBox.SelectAll();

        screen.Render();

        Assert.Equal(Color.DarkBlue, screen.Rows[0].Cells[0].ForeColor);
        Assert.Equal(Color.Yellow, screen.Rows[0].Cells[0].BackgroundColor);
        Assert.Equal(Color.Yellow, screen.Rows[0].Cells[3].ForeColor);
        Assert.Equal(Color.DarkBlue, screen.Rows[0].Cells[3].BackgroundColor);
    }
}
