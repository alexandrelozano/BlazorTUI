using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class EditHistoryTests
{
    [Fact]
    public void TextBoxUndoesAndRedoesIndividualEdits()
    {
        var textBox = new TextBox("name", "", 0, 0, 20, Color.White, Color.Black)
        {
            Value = "ab"
        };

        textBox.KeyDown("c", false);
        textBox.KeyDown("Backspace", false);

        Assert.Equal("ab", textBox.Value);
        Assert.True(textBox.CanUndo);
        Assert.True(textBox.Undo());
        Assert.Equal("abc", textBox.Value);
        Assert.True(textBox.Undo());
        Assert.Equal("ab", textBox.Value);
        Assert.True(textBox.Redo());
        Assert.Equal("abc", textBox.Value);

        textBox.KeyDown("x", false);
        Assert.Equal("abcx", textBox.Value);
        Assert.False(textBox.CanRedo);
    }

    [Fact]
    public void UndoRestoresSelectionBeforeCut()
    {
        var textBox = new TextBox("name", "hello", 0, 0, 10, Color.White, Color.Black);
        textBox.SelectAll();

        textBox.CutSelection();
        Assert.Equal("", textBox.Value);

        Assert.True(textBox.Undo());
        Assert.Equal("hello", textBox.Value);
        Assert.Equal("hello", textBox.SelectedText);

        Assert.True(textBox.Redo());
        Assert.Equal("", textBox.Value);
    }

    [Fact]
    public void TextAreaRestoresMultilinePaste()
    {
        string original = $"one{Environment.NewLine}two";
        var textArea = new TextArea("notes", original, 0, 0, 12, 4, 10, 5, Color.White, Color.Black);

        textArea.Paste($"A{Environment.NewLine}");
        Assert.Equal($"A{Environment.NewLine}{original}", textArea.Value);

        Assert.True(textArea.Undo());
        Assert.Equal(original, textArea.Value);
        Assert.True(textArea.Redo());
        Assert.Equal($"A{Environment.NewLine}{original}", textArea.Value);
    }

    [Fact]
    public void AssigningValueClearsEditHistory()
    {
        var textBox = new TextBox("name", "", 0, 0, 10, Color.White, Color.Black);
        textBox.KeyDown("a", false);
        Assert.True(textBox.CanUndo);

        textBox.Value = "external";

        Assert.False(textBox.CanUndo);
        Assert.False(textBox.CanRedo);
        Assert.False(textBox.Undo());
    }

    [Fact]
    public void EditHistoryIsLimitedToOneHundredOperations()
    {
        var textBox = new TextBox("text", "", 0, 0, 120, Color.White, Color.Black);
        for (int index = 0; index < 105; index++)
            textBox.KeyDown("x", false);

        int undoCount = 0;
        while (textBox.Undo())
            undoCount++;

        Assert.Equal(100, undoCount);
        Assert.Equal("xxxxx", textBox.Value);
    }
}
