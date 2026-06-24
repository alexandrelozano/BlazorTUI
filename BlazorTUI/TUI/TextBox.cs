using System.Drawing;

namespace BlazorTUI.TUI
{
    public class TextBox : Control, IClipboardControl, IClipboardPermissions, IUndoableControl
    {
        private readonly EditHistory<TextBoxState> editHistory = new();
        internal string text;

        internal bool blinkCursor;
        internal short cursor;
        private short? selectionAnchor;

        public string value
        {
            get => text;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                text = value;
                cursor = (short)Math.Min(value.Length, short.MaxValue);
                selectionAnchor = null;
                editHistory.Clear();
            }
        }

        public string Value { get => value; set => this.value = value; }

        public bool HasSelection => selectionAnchor.HasValue && selectionAnchor.Value != cursor;

        public short SelectionStart => HasSelection ? Math.Min(selectionAnchor!.Value, cursor) : cursor;

        public short SelectionLength => HasSelection ? (short)Math.Abs(selectionAnchor!.Value - cursor) : (short)0;

        public string SelectedText => HasSelection ? text.Substring(SelectionStart, SelectionLength) : "";

        public virtual bool AllowCopy { get; set; } = true;

        public virtual bool AllowPaste { get; set; } = true;

        public bool CanUndo => editHistory.CanUndo;

        public bool CanRedo => editHistory.CanRedo;

        public TextBox(string name, string text, short X, short Y, short width, Color forecolor, Color backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(text);

            Name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            height = 1;
            this.text = text;
            foreColor = forecolor;
            backgroundColor = backgroundcolor;

            Focus = false;
            TabStop = true;
            cursor = 0;
        }

        public void SelectAll()
        {
            selectionAnchor = 0;
            cursor = (short)Math.Min(text.Length, short.MaxValue);
        }

        public string CutSelection()
        {
            TextBoxState previousState = CaptureState();
            string selectedText = SelectedText;
            DeleteSelection();
            RecordEdit(previousState);
            return selectedText;
        }

        public void Paste(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            string singleLineValue = NormalizeSingleLine(value);
            if (singleLineValue.Length == 0)
                return;

            TextBoxState previousState = CaptureState();
            DeleteSelection();
            int capacity = Math.Max(0, width - 1 - text.Length);
            if (capacity == 0)
            {
                RecordEdit(previousState);
                return;
            }

            string insertedText = singleLineValue[..Math.Min(singleLineValue.Length, capacity)];
            text = text.Insert(cursor, insertedText);
            cursor += (short)insertedText.Length;
            RecordEdit(previousState);
        }

        public bool Undo()
        {
            if (!editHistory.TryUndo(CaptureState(), out TextBoxState targetState))
                return false;

            RestoreState(targetState);
            return true;
        }

        public bool Redo()
        {
            if (!editHistory.TryRedo(CaptureState(), out TextBoxState targetState))
                return false;

            RestoreState(targetState);
            return true;
        }

        public void ClearHistory()
        {
            editHistory.Clear();
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                cursor = X < text.Length ? X : (short)Math.Min(text.Length, short.MaxValue);
                selectionAnchor = null;
                container.TopContainer().SetFocus(name);
                handled = true;
            }

            if (handled)
                NotifyClicked();

            return handled;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            bool handled = false;

            if (Visible)
            {
                switch (key)
                {
                    case "Tab":
                    case "Enter":
                        break;
                    case "Home":
                        MoveCursor(0, shiftKey);
                        handled = true;
                        break;
                    case "End":
                        MoveCursor((short)Math.Min(text.Length, short.MaxValue), shiftKey);
                        handled = true;
                        break;
                    case "Backspace":
                        TextBoxState stateBeforeBackspace = CaptureState();
                        if (!DeleteSelection() && cursor > 0)
                        {
                            text = text.Remove(cursor - 1, 1);
                            cursor--;
                        }
                        RecordEdit(stateBeforeBackspace);
                        handled = true;
                        break;
                    case "Delete":
                        TextBoxState stateBeforeDelete = CaptureState();
                        if (!DeleteSelection() && cursor < text.Length)
                            text = text.Remove(cursor, 1);
                        RecordEdit(stateBeforeDelete);
                        handled = true;
                        break;
                    case "ArrowRight":
                        if (!shiftKey && HasSelection)
                            MoveCursor((short)(SelectionStart + SelectionLength), false);
                        else
                            MoveCursor((short)Math.Min(cursor + 1, text.Length), shiftKey);
                        handled = true;
                        break;
                    case "ArrowLeft":
                        if (!shiftKey && HasSelection)
                            MoveCursor(SelectionStart, false);
                        else
                            MoveCursor((short)Math.Max(0, cursor - 1), shiftKey);
                        handled = true;
                        break;
                    default:
                        if (key.Length == 1)
                        {
                            TextBoxState stateBeforeInput = CaptureState();
                            DeleteSelection();
                            if (text.Length < width - 1)
                            {
                                text = text.Insert(cursor, key);
                                cursor++;
                            }
                            RecordEdit(stateBeforeInput);
                        }
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (!Visible)
                return;

            for (short n = 0; n < width; n++)
            {
                if (container.YOffset() + Y >= container.YOffset() + container.height || container.YOffset() + Y >= rows.Count)
                    continue;

                if (container.XOffset() + X + n >= container.XOffset() + container.width || container.XOffset() + X + n >= rows[Y].Cells.Count)
                    continue;

                Cell cell = rows[container.YOffset() + Y].Cells[container.XOffset() + X + n];
                bool selected = n < text.Length && IsSelected(n);
                cell.foreColor = selected ? backgroundColor : foreColor;
                cell.backgroundColor = selected ? foreColor : backgroundColor;
                cell.textDecoration = Cell.TextDecoration.None;
                cell.character = GetDisplayCharacter(n);

                if (Focus && n == cursor)
                {
                    if (blinkCursor)
                        cell.textDecoration = Cell.TextDecoration.UnderLine;

                    blinkCursor = !blinkCursor;
                }
            }
        }

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            short start = SelectionStart;
            text = text.Remove(start, SelectionLength);
            cursor = start;
            selectionAnchor = null;
            return true;
        }

        protected virtual string GetDisplayCharacter(short position)
            => position < text.Length ? text.Substring(position, 1) : " ";

        private bool IsSelected(short position)
            => HasSelection && position >= SelectionStart && position < SelectionStart + SelectionLength;

        private TextBoxState CaptureState()
            => new(text, cursor, selectionAnchor);

        private void RestoreState(TextBoxState state)
        {
            text = state.Text;
            cursor = state.Cursor;
            selectionAnchor = state.SelectionAnchor;
        }

        private void RecordEdit(TextBoxState previousState)
        {
            if (!string.Equals(previousState.Text, text, StringComparison.Ordinal))
                editHistory.Record(previousState);
        }

        private void MoveCursor(short newPosition, bool extendSelection)
        {
            if (extendSelection)
                selectionAnchor ??= cursor;
            else
                selectionAnchor = null;

            cursor = newPosition;
            if (selectionAnchor == cursor)
                selectionAnchor = null;
        }

        private static string NormalizeSingleLine(string value)
            => value.Replace("\r\n", " ", StringComparison.Ordinal)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

        private readonly record struct TextBoxState(string Text, short Cursor, short? SelectionAnchor);
    }
}
