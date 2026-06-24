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

    public interface IClipboardPermissions
    {
        bool AllowCopy { get; set; }

        bool AllowPaste { get; set; }
    }
}
