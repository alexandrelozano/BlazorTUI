namespace BlazorTUI.TUI
{
    public interface IUndoableControl
    {
        bool CanUndo { get; }

        bool CanRedo { get; }

        bool Undo();

        bool Redo();

        void ClearHistory();
    }
}
