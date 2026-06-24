namespace BlazorTUI.TUI
{
    internal sealed class EditHistory<TState>
    {
        private const int MaximumEntryCount = 100;
        private readonly Stack<TState> undoStates = new();
        private readonly Stack<TState> redoStates = new();

        internal bool CanUndo => undoStates.Count > 0;

        internal bool CanRedo => redoStates.Count > 0;

        internal void Record(TState previousState)
        {
            undoStates.Push(previousState);
            Trim(undoStates);
            redoStates.Clear();
        }

        internal bool TryUndo(TState currentState, out TState targetState)
        {
            if (!undoStates.TryPop(out targetState!))
                return false;

            redoStates.Push(currentState);
            Trim(redoStates);
            return true;
        }

        internal bool TryRedo(TState currentState, out TState targetState)
        {
            if (!redoStates.TryPop(out targetState!))
                return false;

            undoStates.Push(currentState);
            Trim(undoStates);
            return true;
        }

        internal void Clear()
        {
            undoStates.Clear();
            redoStates.Clear();
        }

        private static void Trim(Stack<TState> states)
        {
            if (states.Count <= MaximumEntryCount)
                return;

            TState[] newestStates = states.Take(MaximumEntryCount).ToArray();
            states.Clear();
            for (int index = newestStates.Length - 1; index >= 0; index--)
                states.Push(newestStates[index]);
        }
    }
}
