namespace BlazorTUI.TUI
{
    public interface IClipboardControl
    {
        bool HasSelection { get; }

        string SelectedText { get; }

        void SelectAll();

        string CutSelection();

        void Paste(string value);
    }
}
