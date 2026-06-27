using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class UnicodeTests
{
    [Fact]
    public void TextBoxEditsByTextElementWithoutSplittingCombiningEmojiOrWideCharacters()
    {
        var textBox = new TextBox("unicode", "", 0, 0, 10, Color.White, Color.Black)
        {
            Value = "e\u0301🙂漢"
        };

        textBox.SelectAll();
        Assert.Equal((short)3, textBox.SelectionLength);
        Assert.Equal("e\u0301🙂漢", textBox.SelectedText);

        textBox.KeyDown("End", false);
        textBox.KeyDown("Backspace", false);
        Assert.Equal("e\u0301🙂", textBox.Value);

        textBox.KeyDown("Backspace", false);
        Assert.Equal("e\u0301", textBox.Value);

        textBox.KeyDown("Backspace", false);
        Assert.Equal("", textBox.Value);
    }

    [Fact]
    public void TextBoxRendersCombiningEmojiAndWideCharactersByVisualCell()
    {
        var screen = new Screen(8, 3);
        var textBox = new TextBox("unicode", "e\u0301🙂漢", 0, 0, 8, Color.White, Color.Black);
        screen.TopContainer.AddControl(textBox);

        screen.Render();

        Assert.Equal("e\u0301", screen.Rows[0].Cells[0].Character);
        Assert.Equal("🙂", screen.Rows[0].Cells[1].Character);
        Assert.Equal(" ", screen.Rows[0].Cells[2].Character);
        Assert.Equal("漢", screen.Rows[0].Cells[3].Character);
        Assert.Equal(" ", screen.Rows[0].Cells[4].Character);
    }

    [Fact]
    public void TextBoxPasteClipsByVisualWidthWithoutSplittingWideCharacters()
    {
        var textBox = new TextBox("unicode", "", 0, 0, 5, Color.White, Color.Black);

        textBox.Paste("ab漢c");

        Assert.Equal("ab漢", textBox.Value);
    }

    [Fact]
    public void TextAreaEditsAndSelectsUnicodeByTextElement()
    {
        string value = $"e\u0301🙂漢{Environment.NewLine}ab🙂";
        var textArea = new TextArea("unicode", "", 0, 0, 8, 4, 6, 4, Color.White, Color.Black)
        {
            Value = value
        };

        textArea.SelectAll();
        Assert.Equal(value, textArea.SelectedText);

        textArea.KeyDown("End", false);
        textArea.KeyDown("Backspace", false);

        Assert.Equal($"e\u0301🙂漢{Environment.NewLine}ab", textArea.Value);
    }

    [Fact]
    public void TextAreaRendersUnicodeByVisualCell()
    {
        var screen = new Screen(8, 4);
        var textArea = new TextArea("unicode", "e\u0301🙂漢", 0, 0, 8, 4, 6, 4, Color.White, Color.Black);
        screen.TopContainer.AddControl(textArea);

        screen.Render();

        Assert.Equal("e\u0301", screen.Rows[0].Cells[0].Character);
        Assert.Equal("🙂", screen.Rows[0].Cells[1].Character);
        Assert.Equal(" ", screen.Rows[0].Cells[2].Character);
        Assert.Equal("漢", screen.Rows[0].Cells[3].Character);
        Assert.Equal(" ", screen.Rows[0].Cells[4].Character);
    }

    [Fact]
    public void LabelRendersWideCharactersByVisualCell()
    {
        var screen = new Screen(6, 2);
        screen.TopContainer.AddControl(new Label("label", "A漢B", 0, 0, 6, Color.White, Color.Black));

        screen.Render();

        Assert.Equal("A", screen.Rows[0].Cells[0].Character);
        Assert.Equal("漢", screen.Rows[0].Cells[1].Character);
        Assert.Equal(" ", screen.Rows[0].Cells[2].Character);
        Assert.Equal("B", screen.Rows[0].Cells[3].Character);
    }
}
