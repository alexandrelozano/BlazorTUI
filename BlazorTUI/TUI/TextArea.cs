using System.Drawing;

namespace BlazorTUI.TUI
{
    public class TextArea : Control, IClipboardControl, IClipboardPermissions, IUndoableControl
    {
        private readonly EditHistory<TextAreaState> editHistory = new();
        private List<string> text;
        private bool blinkCursor;
        private short cursorX;
        private short cursorY;
        private short scrollY;
        private short scrollX;
        private TextPosition? selectionAnchor;

        public short maxLines;
        public short MaxLines { get => maxLines; set => maxLines = value; }
        public short maxTextWidth;
        public short MaxTextWidth { get => maxTextWidth; set => maxTextWidth = value; }

        public string value
        {
            get => string.Join(Environment.NewLine, text);
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                text = SplitLines(value);
                cursorX = 0;
                cursorY = 0;
                scrollX = 0;
                scrollY = 0;
                selectionAnchor = null;
                editHistory.Clear();
            }
        }

        public string Value { get => value; set => this.value = value; }

        public bool HasSelection => selectionAnchor.HasValue && selectionAnchor.Value != CursorPosition;

        public int SelectionStart => GetSelectionRange().Start;

        public int SelectionLength
        {
            get
            {
                (int start, int end) = GetSelectionRange();
                return end - start;
            }
        }

        public string SelectedText
        {
            get
            {
                if (!HasSelection)
                    return "";

                string selectedText = CanonicalText.Substring(SelectionStart, SelectionLength);
                return selectedText.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
            }
        }

        public bool AllowCopy { get; set; } = true;

        public bool AllowPaste { get; set; } = true;

        public bool CanUndo => editHistory.CanUndo;

        public bool CanRedo => editHistory.CanRedo;

        private TextPosition CursorPosition => new(cursorY, cursorX);

        private string CanonicalText => string.Join('\n', text);

        public TextArea(string name, string text, short X, short Y, short width, short height, short maxTextWidth, short maxLines, Color forecolor, Color backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(text);

            Name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            this.maxLines = maxLines;
            this.maxTextWidth = maxTextWidth;
            this.text = SplitLines(text);
            foreColor = forecolor;
            backgroundColor = backgroundcolor;

            Focus = false;
            TabStop = true;
        }

        public void SelectAll()
        {
            selectionAnchor = new TextPosition(0, 0);
            cursorY = (short)(text.Count - 1);
            cursorX = (short)Math.Min(text[^1].Length, short.MaxValue);
            EnsureCursorVisible();
        }

        public string CutSelection()
        {
            TextAreaState previousState = CaptureState();
            string selectedText = SelectedText;
            DeleteSelection();
            RecordEdit(previousState);
            return selectedText;
        }

        public void Paste(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            string normalizedValue = NormalizeLineBreaks(value);
            if (normalizedValue.Length == 0)
                return;

            TextAreaState previousState = CaptureState();
            DeleteSelection();
            string currentText = CanonicalText;
            int cursorOffset = GetOffset(CursorPosition);
            string candidate = currentText.Insert(cursorOffset, normalizedValue);
            int candidateCursorOffset = cursorOffset + normalizedValue.Length;
            List<string> candidateLines = candidate.Split('\n').ToList();
            TextPosition candidateCursor = PositionFromOffset(candidateLines, candidateCursorOffset);

            int lineLimit = Math.Max(1, (int)maxLines);
            int widthLimit = Math.Max(0, (int)maxTextWidth);
            text = candidateLines
                .Take(lineLimit)
                .Select(line => line[..Math.Min(line.Length, widthLimit)])
                .ToList();

            cursorY = (short)Math.Min(candidateCursor.Line, text.Count - 1);
            cursorX = (short)Math.Min(candidateCursor.Column, text[cursorY].Length);
            selectionAnchor = null;
            EnsureCursorVisible();
            RecordEdit(previousState);
        }

        public bool Undo()
        {
            if (!editHistory.TryUndo(CaptureState(), out TextAreaState targetState))
                return false;

            RestoreState(targetState);
            return true;
        }

        public bool Redo()
        {
            if (!editHistory.TryRedo(CaptureState(), out TextAreaState targetState))
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
                selectionAnchor = null;
                if (X == width - 1)
                {
                    if (Y == 0 && scrollY > 0)
                        scrollY--;
                    else if (Y == height - 1 && scrollY < Math.Max(0, text.Count - (height - 1)))
                        scrollY++;
                }
                else if (Y == height - 1)
                {
                    if (X == 0 && scrollX > 0)
                        scrollX--;
                    else if (X == width - 2 && scrollX < maxTextWidth)
                        scrollX++;
                }
                else if (Y + scrollY < text.Count)
                {
                    cursorY = (short)(Y + scrollY);
                    cursorX = (short)Math.Min(X + scrollX, text[cursorY].Length);
                }

                EnsureCursorVisible();
                container.TopContainer().SetFocus(name);
                handled = true;
            }

            if (handled)
                NotifyClicked();

            return handled;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            bool handled = true;
            switch (key)
            {
                case "Home":
                    MoveCursor(new TextPosition(cursorY, 0), shiftKey);
                    break;
                case "End":
                    MoveCursor(new TextPosition(cursorY, text[cursorY].Length), shiftKey);
                    break;
                case "Tab":
                    handled = false;
                    break;
                case "Enter":
                    if (HasSelection || text.Count < maxLines)
                        Paste(Environment.NewLine);
                    break;
                case "Backspace":
                    TextAreaState stateBeforeBackspace = CaptureState();
                    Backspace();
                    RecordEdit(stateBeforeBackspace);
                    break;
                case "Delete":
                    TextAreaState stateBeforeDelete = CaptureState();
                    Delete();
                    RecordEdit(stateBeforeDelete);
                    break;
                case "ArrowRight":
                    MoveHorizontally(1, shiftKey);
                    break;
                case "ArrowLeft":
                    MoveHorizontally(-1, shiftKey);
                    break;
                case "ArrowUp":
                    MoveVertically(-1, shiftKey);
                    break;
                case "ArrowDown":
                    MoveVertically(1, shiftKey);
                    break;
                default:
                    if (key.Length == 1)
                        Paste(key);
                    break;
            }

            EnsureCursorVisible();
            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (!Visible)
                return;

            for (short h = 0; h < height; h++)
            {
                int textLineIndex = h + scrollY;
                string line = textLineIndex < text.Count ? text[textLineIndex] : "";
                int rowIndex = container.YOffset() + Y + h;
                if (rowIndex < 0 || rowIndex >= rows.Count)
                    continue;

                for (short n = 0; n < width; n++)
                {
                    int columnIndex = container.XOffset() + X + n;
                    if (columnIndex < 0 || columnIndex >= rows[rowIndex].Cells.Count)
                        continue;

                    Cell cell = rows[rowIndex].Cells[columnIndex];
                    int textColumnIndex = n + scrollX;
                    bool selected = h < height - 1 && n < width - 1 && textLineIndex < text.Count &&
                        textColumnIndex < line.Length && IsSelected(new TextPosition(textLineIndex, textColumnIndex));
                    cell.foreColor = selected ? backgroundColor : foreColor;
                    cell.backgroundColor = selected ? foreColor : backgroundColor;
                    cell.textDecoration = Cell.TextDecoration.None;

                    string character;
                    if (h == height - 1)
                    {
                        character = n switch
                        {
                            0 => "←",
                            _ when n == width - 2 => "→",
                            _ when n == width - 1 => "↓",
                            _ => "─"
                        };
                    }
                    else if (n == width - 1)
                    {
                        character = h == 0 ? "↑" : "│";
                    }
                    else
                    {
                        character = textColumnIndex < line.Length ? line.Substring(textColumnIndex, 1) : " ";
                    }

                    if (Focus && h == cursorY - scrollY && n == cursorX - scrollX)
                    {
                        if (blinkCursor)
                            cell.textDecoration = Cell.TextDecoration.UnderLine;

                        blinkCursor = !blinkCursor;
                    }

                    cell.character = character;
                }
            }
        }

        private void Backspace()
        {
            if (DeleteSelection())
                return;

            if (cursorX > 0)
            {
                text[cursorY] = text[cursorY].Remove(cursorX - 1, 1);
                cursorX--;
                return;
            }

            if (cursorY == 0)
                return;

            int previousLineLength = text[cursorY - 1].Length;
            if (previousLineLength + text[cursorY].Length > maxTextWidth)
                return;

            text[cursorY - 1] += text[cursorY];
            text.RemoveAt(cursorY);
            cursorY--;
            cursorX = (short)previousLineLength;
        }

        private void Delete()
        {
            if (DeleteSelection())
                return;

            if (cursorX < text[cursorY].Length)
            {
                text[cursorY] = text[cursorY].Remove(cursorX, 1);
                return;
            }

            if (cursorY >= text.Count - 1 || text[cursorY].Length + text[cursorY + 1].Length > maxTextWidth)
                return;

            text[cursorY] += text[cursorY + 1];
            text.RemoveAt(cursorY + 1);
        }

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            (int start, int end) = GetSelectionRange();
            string remainingText = CanonicalText.Remove(start, end - start);
            text = remainingText.Split('\n').ToList();
            TextPosition position = PositionFromOffset(text, start);
            cursorY = (short)position.Line;
            cursorX = (short)position.Column;
            selectionAnchor = null;
            EnsureCursorVisible();
            return true;
        }

        private void MoveHorizontally(int direction, bool extendSelection)
        {
            if (!extendSelection && HasSelection)
            {
                int targetOffset = direction < 0 ? SelectionStart : SelectionStart + SelectionLength;
                MoveCursor(PositionFromOffset(text, targetOffset), false);
                return;
            }

            TextPosition target = CursorPosition;
            if (direction < 0)
            {
                if (cursorX > 0)
                    target = new TextPosition(cursorY, (short)(cursorX - 1));
                else if (cursorY > 0)
                    target = new TextPosition(cursorY - 1, text[cursorY - 1].Length);
            }
            else if (cursorX < text[cursorY].Length)
            {
                target = new TextPosition(cursorY, (short)(cursorX + 1));
            }
            else if (cursorY < text.Count - 1)
            {
                target = new TextPosition((short)(cursorY + 1), 0);
            }

            MoveCursor(target, extendSelection);
        }

        private void MoveVertically(int direction, bool extendSelection)
        {
            int targetLine = Math.Clamp(cursorY + direction, 0, text.Count - 1);
            short targetColumn = (short)Math.Min(cursorX, text[targetLine].Length);
            MoveCursor(new TextPosition((short)targetLine, targetColumn), extendSelection);
        }

        private void MoveCursor(TextPosition newPosition, bool extendSelection)
        {
            if (extendSelection)
                selectionAnchor ??= CursorPosition;
            else
                selectionAnchor = null;

            cursorY = (short)Math.Clamp(newPosition.Line, 0, text.Count - 1);
            cursorX = (short)Math.Clamp(newPosition.Column, 0, text[cursorY].Length);
            if (selectionAnchor == CursorPosition)
                selectionAnchor = null;
        }

        private bool IsSelected(TextPosition position)
        {
            if (!HasSelection)
                return false;

            int offset = GetOffset(position);
            (int start, int end) = GetSelectionRange();
            return offset >= start && offset < end;
        }

        private TextAreaState CaptureState()
            => new(CanonicalText, cursorX, cursorY, scrollX, scrollY, selectionAnchor);

        private void RestoreState(TextAreaState state)
        {
            text = state.Text.Split('\n').ToList();
            cursorX = state.CursorX;
            cursorY = state.CursorY;
            scrollX = state.ScrollX;
            scrollY = state.ScrollY;
            selectionAnchor = state.SelectionAnchor;
            EnsureCursorVisible();
        }

        private void RecordEdit(TextAreaState previousState)
        {
            if (!string.Equals(previousState.Text, CanonicalText, StringComparison.Ordinal))
                editHistory.Record(previousState);
        }

        private (int Start, int End) GetSelectionRange()
        {
            int cursorOffset = GetOffset(CursorPosition);
            if (!selectionAnchor.HasValue)
                return (cursorOffset, cursorOffset);

            int anchorOffset = GetOffset(selectionAnchor.Value);
            return (Math.Min(anchorOffset, cursorOffset), Math.Max(anchorOffset, cursorOffset));
        }

        private int GetOffset(TextPosition position)
        {
            int offset = position.Column;
            for (int line = 0; line < position.Line; line++)
                offset += text[line].Length + 1;

            return offset;
        }

        private void EnsureCursorVisible()
        {
            int visibleWidth = Math.Max(1, width - 1);
            int visibleHeight = Math.Max(1, height - 1);

            if (cursorX < scrollX)
                scrollX = cursorX;
            else if (cursorX >= scrollX + visibleWidth)
                scrollX = (short)(cursorX - visibleWidth + 1);

            if (cursorY < scrollY)
                scrollY = cursorY;
            else if (cursorY >= scrollY + visibleHeight)
                scrollY = (short)(cursorY - visibleHeight + 1);

            scrollX = (short)Math.Max(0, (int)scrollX);
            scrollY = (short)Math.Max(0, (int)scrollY);
        }

        private static TextPosition PositionFromOffset(IReadOnlyList<string> lines, int offset)
        {
            int remaining = Math.Max(0, offset);
            for (int line = 0; line < lines.Count; line++)
            {
                if (remaining <= lines[line].Length)
                    return new TextPosition(line, remaining);

                remaining -= lines[line].Length + 1;
            }

            int lastLine = lines.Count - 1;
            return new TextPosition(lastLine, lines[lastLine].Length);
        }

        private static List<string> SplitLines(string value)
            => NormalizeLineBreaks(value).Split('\n').ToList();

        private static string NormalizeLineBreaks(string value)
            => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        private readonly record struct TextPosition(int Line, int Column);

        private readonly record struct TextAreaState(
            string Text,
            short CursorX,
            short CursorY,
            short ScrollX,
            short ScrollY,
            TextPosition? SelectionAnchor);
    }
}
