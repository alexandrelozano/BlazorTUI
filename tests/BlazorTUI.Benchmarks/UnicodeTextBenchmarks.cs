using System.Drawing;
using BenchmarkDotNet.Attributes;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

[MemoryDiagnoser]
public class UnicodeTextBenchmarks
{
    private const string UnicodePayload = "Cafe\u0301 🙂 漢字 — delivery notes with accents àèíòú and emoji 🚚";
    private TextBox textBox = null!;
    private TextArea textArea = null!;
    private Screen renderScreen = null!;

    [GlobalSetup]
    public void Setup()
    {
        textBox = new TextBox("unicodeTextBox", "", 0, 0, 60, Color.White, Color.Black);
        textArea = new TextArea("unicodeTextArea", "", 0, 0, 60, 6, 160, 20, Color.White, Color.Black);
        renderScreen = new Screen(80, 12);
        renderScreen.TopContainer.AddControl(new TextBox(
            "renderTextBox",
            UnicodePayload,
            1, 1, 60,
            Color.White,
            Color.Black));
        renderScreen.TopContainer.AddControl(new TextArea(
            "renderTextArea",
            UnicodePayload + Environment.NewLine + UnicodePayload,
            1, 3, 60, 5, 160, 20,
            Color.White,
            Color.Black));
    }

    [Benchmark(Baseline = true)]
    public int PasteUnicodeIntoTextBox()
    {
        textBox.Value = "";
        textBox.ClearHistory();
        textBox.Paste(UnicodePayload);
        return textBox.Value.Length;
    }

    [Benchmark]
    public int PasteUnicodeIntoTextArea()
    {
        textArea.Value = "";
        textArea.ClearHistory();
        textArea.Paste(UnicodePayload + Environment.NewLine + UnicodePayload);
        return textArea.Value.Length;
    }

    [Benchmark]
    public long RenderUnicodeControls()
    {
        renderScreen.Render();
        return renderScreen.Revision;
    }
}
