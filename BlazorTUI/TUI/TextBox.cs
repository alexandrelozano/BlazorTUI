using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class TextBox : Control, IClipboardControl, IClipboardPermissions, IUndoableControl
    {
        private readonly EditHistory<TextBoxState> editHistory = new();
        internal string text;

        internal bool blinkCursor;
        internal short cursor;
        private short? selectionAnchor;

        internal string value
        {
            get => text;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                text = value;
                cursor = ToShortTextElementCount(value);
                selectionAnchor = null;
                editHistory.Clear();
            }
        }

        public string Value { get => value; set => this.value = value; }

        public bool HasSelection => selectionAnchor.HasValue && selectionAnchor.Value != cursor;

        public short SelectionStart => HasSelection ? Math.Min(selectionAnchor!.Value, cursor) : cursor;

        public short SelectionLength => HasSelection ? (short)Math.Abs(selectionAnchor!.Value - cursor) : (short)0;

        public string SelectedText => HasSelection ? TuiText.SubstringByTextElements(text, SelectionStart, SelectionLength) : "";

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
            cursor = ToShortTextElementCount(text);
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
            int capacity = Math.Max(0, width - 1 - TuiText.VisualWidth(text));
            if (capacity == 0)
            {
                RecordEdit(previousState);
                return;
            }

            string insertedText = TuiText.TruncateByVisualWidth(singleLineValue, capacity);
            if (insertedText.Length == 0)
            {
                RecordEdit(previousState);
                return;
            }

            text = TuiText.InsertAtTextElement(text, cursor, insertedText);
            cursor += ToShortTextElementCount(insertedText);
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

        internal void ExportTextInputState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            state.SetString("Value", text);
            state.SetInteger("Cursor", cursor);
            if (selectionAnchor.HasValue)
                state.SetInteger("SelectionAnchor", selectionAnchor.Value);
        }

        internal void RestoreTextInputState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetString("Value", out string restoredText))
                text = restoredText;

            short maximumPosition = ToShortTextElementCount(text);
            cursor = state.TryGetInteger("Cursor", out int restoredCursor)
                ? (short)Math.Clamp(restoredCursor, 0, maximumPosition)
                : maximumPosition;

            selectionAnchor = state.TryGetInteger("SelectionAnchor", out int restoredAnchor)
                ? (short)Math.Clamp(restoredAnchor, 0, maximumPosition)
                : null;

            if (selectionAnchor == cursor)
                selectionAnchor = null;

            ClearHistory();
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                cursor = (short)TuiText.TextElementIndexFromVisualColumn(text, X);
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
                        MoveCursor(ToShortTextElementCount(text), shiftKey);
                        handled = true;
                        break;
                    case "Backspace":
                        TextBoxState stateBeforeBackspace = CaptureState();
                        if (!DeleteSelection() && cursor > 0)
                        {
                            text = TuiText.RemoveTextElements(text, cursor - 1, 1);
                            cursor--;
                        }
                        RecordEdit(stateBeforeBackspace);
                        handled = true;
                        break;
                    case "Delete":
                        TextBoxState stateBeforeDelete = CaptureState();
                        if (!DeleteSelection() && cursor < TuiText.TextElementCount(text))
                            text = TuiText.RemoveTextElements(text, cursor, 1);
                        RecordEdit(stateBeforeDelete);
                        handled = true;
                        break;
                    case "ArrowRight":
                        if (!shiftKey && HasSelection)
                            MoveCursor((short)(SelectionStart + SelectionLength), false);
                        else
                            MoveCursor((short)TuiText.NextTextElementIndex(text, cursor), shiftKey);
                        handled = true;
                        break;
                    case "ArrowLeft":
                        if (!shiftKey && HasSelection)
                            MoveCursor(SelectionStart, false);
                        else
                            MoveCursor((short)TuiText.PreviousTextElementIndex(text, cursor), shiftKey);
                        handled = true;
                        break;
                    default:
                        if (TuiText.TextElementCount(key) == 1)
                        {
                            TextBoxState stateBeforeInput = CaptureState();
                            DeleteSelection();
                            if (TuiText.VisualWidth(text) + TuiText.VisualWidth(key) <= width - 1)
                            {
                                text = TuiText.InsertAtTextElement(text, cursor, key);
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
                TuiText.TextCell textCell = TuiText.CellInfoAt(text, n);
                bool selected = textCell.TextElementIndex.HasValue && IsSelected(textCell.TextElementIndex.Value);
                cell.foreColor = selected ? backgroundColor : foreColor;
                cell.backgroundColor = selected ? foreColor : backgroundColor;
                cell.textDecoration = Cell.TextDecoration.None;
                cell.character = GetDisplayCharacter(n);

                if (Focus && n == TuiText.VisualWidth(text, cursor))
                {
                    if (blinkCursor)
                        cell.textDecoration = Cell.TextDecoration.UnderLine;

                    blinkCursor = !blinkCursor;
                }
            }
        }

        protected override object? GetValidationValue() => value;

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            short start = SelectionStart;
            text = TuiText.RemoveTextElements(text, start, SelectionLength);
            cursor = start;
            selectionAnchor = null;
            return true;
        }

        protected virtual string GetDisplayCharacter(short position)
            => TuiText.CellAt(text, position);

        private bool IsSelected(int position)
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

            cursor = (short)Math.Clamp(newPosition, (short)0, ToShortTextElementCount(text));
            if (selectionAnchor == cursor)
                selectionAnchor = null;
        }

        private static string NormalizeSingleLine(string value)
            => value.Replace("\r\n", " ", StringComparison.Ordinal)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

        private static short ToShortTextElementCount(string value)
            => (short)Math.Min(TuiText.TextElementCount(value), short.MaxValue);

        private readonly record struct TextBoxState(string Text, short Cursor, short? SelectionAnchor);
    }
}
