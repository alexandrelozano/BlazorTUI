namespace BlazorTUI.TUI
{
    public sealed class TuiShortcutMap
    {
        private readonly Dictionary<TuiShortcutAction, List<TuiKeyGesture>> bindings = new();

        public IReadOnlyDictionary<TuiShortcutAction, IReadOnlyList<TuiKeyGesture>> Bindings
            => bindings.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<TuiKeyGesture>)pair.Value.AsReadOnly());

        public static TuiShortcutMap CreateDefault()
        {
            var shortcuts = new TuiShortcutMap();
            shortcuts.SetBindings(TuiShortcutAction.FocusNext, TuiKeyGesture.Parse("Tab"));
            shortcuts.SetBindings(TuiShortcutAction.FocusPrevious, TuiKeyGesture.Parse("Shift+Tab"));
            shortcuts.SetBindings(TuiShortcutAction.ToggleMenuShortcuts, TuiKeyGesture.Parse("Alt"));
            shortcuts.SetBindings(
                TuiShortcutAction.ToggleCommandPalette,
                TuiKeyGesture.Parse("F2"),
                TuiKeyGesture.Parse("Control+K"),
                TuiKeyGesture.Parse("Meta+K"));
            shortcuts.SetBindings(
                TuiShortcutAction.SelectNextTab,
                TuiKeyGesture.Parse("Control+Tab"),
                TuiKeyGesture.Parse("Alt+PageDown"));
            shortcuts.SetBindings(
                TuiShortcutAction.SelectPreviousTab,
                TuiKeyGesture.Parse("Control+Shift+Tab"),
                TuiKeyGesture.Parse("Alt+PageUp"));
            shortcuts.SetBindings(TuiShortcutAction.SelectAll, TuiKeyGesture.Parse("Control+A"), TuiKeyGesture.Parse("Meta+A"));
            shortcuts.SetBindings(TuiShortcutAction.Copy, TuiKeyGesture.Parse("Control+C"), TuiKeyGesture.Parse("Meta+C"));
            shortcuts.SetBindings(TuiShortcutAction.Cut, TuiKeyGesture.Parse("Control+X"), TuiKeyGesture.Parse("Meta+X"));
            shortcuts.SetBindings(TuiShortcutAction.Paste, TuiKeyGesture.Parse("Control+V"), TuiKeyGesture.Parse("Meta+V"));
            shortcuts.SetBindings(TuiShortcutAction.Undo, TuiKeyGesture.Parse("Control+Z"), TuiKeyGesture.Parse("Meta+Z"));
            shortcuts.SetBindings(
                TuiShortcutAction.Redo,
                TuiKeyGesture.Parse("Control+Y"),
                TuiKeyGesture.Parse("Meta+Y"),
                TuiKeyGesture.Parse("Control+Shift+Z"),
                TuiKeyGesture.Parse("Meta+Shift+Z"));
            return shortcuts;
        }

        public IReadOnlyList<TuiKeyGesture> GetBindings(TuiShortcutAction action)
        {
            ValidateAction(action);
            return bindings.TryGetValue(action, out List<TuiKeyGesture>? gestures)
                ? gestures.AsReadOnly()
                : Array.Empty<TuiKeyGesture>();
        }

        public void SetBindings(TuiShortcutAction action, params TuiKeyGesture[] gestures)
        {
            ValidateAction(action);
            ArgumentNullException.ThrowIfNull(gestures);

            List<TuiKeyGesture> uniqueGestures = NormalizeGestures(gestures);
            EnsureGesturesAreAvailable(action, uniqueGestures);
            if (uniqueGestures.Count == 0)
                bindings.Remove(action);
            else
                bindings[action] = uniqueGestures;
        }

        public void SetBindings(TuiShortcutAction action, params string[] gestures)
        {
            ArgumentNullException.ThrowIfNull(gestures);
            SetBindings(action, gestures.Select(TuiKeyGesture.Parse).ToArray());
        }

        public void AddBinding(TuiShortcutAction action, TuiKeyGesture gesture)
        {
            ValidateAction(action);
            ArgumentNullException.ThrowIfNull(gesture);
            EnsureGesturesAreAvailable(action, [gesture]);

            if (!bindings.TryGetValue(action, out List<TuiKeyGesture>? gestures))
            {
                gestures = new List<TuiKeyGesture>();
                bindings.Add(action, gestures);
            }

            if (!gestures.Contains(gesture))
                gestures.Add(gesture);
        }

        public void AddBinding(TuiShortcutAction action, string gesture)
            => AddBinding(action, TuiKeyGesture.Parse(gesture));

        public bool RemoveBinding(TuiShortcutAction action, TuiKeyGesture gesture)
        {
            ValidateAction(action);
            ArgumentNullException.ThrowIfNull(gesture);

            if (!bindings.TryGetValue(action, out List<TuiKeyGesture>? gestures))
                return false;

            bool removed = gestures.Remove(gesture);
            if (gestures.Count == 0)
                bindings.Remove(action);

            return removed;
        }

        public bool RemoveBinding(TuiShortcutAction action, string gesture)
            => RemoveBinding(action, TuiKeyGesture.Parse(gesture));

        public void ClearBindings(TuiShortcutAction action)
        {
            ValidateAction(action);
            bindings.Remove(action);
        }

        public void Clear()
            => bindings.Clear();

        public bool TryGetAction(TuiKeyGesture gesture, out TuiShortcutAction action)
        {
            ArgumentNullException.ThrowIfNull(gesture);

            foreach ((TuiShortcutAction candidateAction, List<TuiKeyGesture> gestures) in bindings)
            {
                if (gestures.Contains(gesture))
                {
                    action = candidateAction;
                    return true;
                }
            }

            action = default;
            return false;
        }

        public string ToAriaKeyShortcuts()
            => string.Join(" ", bindings.Values.SelectMany(gestures => gestures).Select(gesture => gesture.ToString()));

        internal IEnumerable<TuiShortcutDescriptor> GetDescriptors()
        {
            foreach ((TuiShortcutAction action, List<TuiKeyGesture> gestures) in bindings)
            {
                foreach (TuiKeyGesture gesture in gestures)
                {
                    yield return new TuiShortcutDescriptor(
                        action.ToString(),
                        gesture.Key,
                        gesture.Control,
                        gesture.Shift,
                        gesture.Alt,
                        gesture.Meta);
                }
            }
        }

        private static List<TuiKeyGesture> NormalizeGestures(IEnumerable<TuiKeyGesture> gestures)
        {
            var uniqueGestures = new List<TuiKeyGesture>();
            foreach (TuiKeyGesture? gesture in gestures)
            {
                ArgumentNullException.ThrowIfNull(gesture);
                if (!uniqueGestures.Contains(gesture))
                    uniqueGestures.Add(gesture);
            }

            return uniqueGestures;
        }

        private void EnsureGesturesAreAvailable(TuiShortcutAction action, IEnumerable<TuiKeyGesture> gestures)
        {
            foreach (TuiKeyGesture gesture in gestures)
            {
                foreach ((TuiShortcutAction existingAction, List<TuiKeyGesture> existingGestures) in bindings)
                {
                    if (existingAction != action && existingGestures.Contains(gesture))
                    {
                        throw new InvalidOperationException(
                            $"The key gesture '{gesture}' is already bound to '{existingAction}'.");
                    }
                }
            }
        }

        private static void ValidateAction(TuiShortcutAction action)
        {
            if (!Enum.IsDefined(action))
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    internal readonly record struct TuiShortcutDescriptor(
        string Action,
        string Key,
        bool Control,
        bool Shift,
        bool Alt,
        bool Meta);
}
