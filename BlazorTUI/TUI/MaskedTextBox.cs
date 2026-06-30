using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class MaskedTextBox : TextBox
    {
        private string mask = "";

        public MaskedTextBox(
            string name,
            string mask,
            string value,
            short X,
            short Y,
            Color foreColor,
            Color backgroundColor)
            : base(name, "", X, Y, (short)Math.Max(1, TuiText.VisualWidth(mask)), foreColor, backgroundColor)
        {
            Mask = mask;
            RawValue = value ?? "";
        }

        public string Mask
        {
            get => mask;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                mask = value;
                Width = (short)Math.Max(1, TuiText.VisualWidth(mask));
                text = ExtractRawValue(text);
                cursor = (short)Math.Clamp(cursor, 0, EditableCount);
            }
        }

        public char PlaceholderChar { get; set; } = '_';

        public bool RequireCompleteForValidation { get; set; } = true;

        public string RawValue
        {
            get => text;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                text = ExtractRawValue(value);
                cursor = (short)Math.Min(text.Length, EditableCount);
                ClearHistory();
            }
        }

        public new string Value
        {
            get => GetFormattedValue(includePlaceholders: false);
            set => RawValue = value;
        }

        public bool IsComplete => text.Length >= EditableCount;

        public override bool Click(short X, short Y)
        {
            if (!Visible || Y != 0)
                return false;

            cursor = MaskColumnToEditableIndex(X);
            container.TopContainer().SetFocus(Name);
            NotifyClicked();
            return true;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (!Visible)
                return false;

            switch (key)
            {
                case "Home":
                    cursor = 0;
                    return true;
                case "End":
                    cursor = (short)text.Length;
                    return true;
                case "ArrowLeft":
                    if (cursor > 0)
                        cursor--;
                    return true;
                case "ArrowRight":
                    if (cursor < text.Length)
                        cursor++;
                    return true;
                case "Backspace":
                    if (cursor > 0)
                    {
                        text = text.Remove(cursor - 1, 1);
                        cursor--;
                    }
                    return true;
                case "Delete":
                    if (cursor < text.Length)
                        text = text.Remove(cursor, 1);
                    return true;
                case "Tab":
                case "Enter":
                    return false;
                default:
                    if (TuiText.TextElementCount(key) != 1 || cursor >= EditableCount)
                        return true;

                    int maskIndex = EditableIndexToMaskIndex(cursor);
                    if (maskIndex < 0 || !MatchesMask(key[0], mask[maskIndex]))
                        return true;

                    if (cursor < text.Length)
                        text = text.Remove(cursor, 1).Insert(cursor, key);
                    else
                        text += key;
                    cursor++;
                    return true;
            }
        }

        public override void Render(IList<Row> rows)
        {
            if (!Visible || container is null)
                return;

            string formatted = GetFormattedValue(includePlaceholders: true);
            for (int n = 0; n < Width; n++)
            {
                if (!TryGetCell(rows, n, out Cell cell))
                    continue;

                cell.ForeColor = ForeColor;
                cell.BackgroundColor = BackgroundColor;
                cell.Decoration = Cell.TextDecoration.None;
                cell.Character = TuiText.CellAt(formatted, n);
                cell.IsVisible = true;
                cell.BackgroundImage = "";
                cell.ScaleX = 1;
                cell.ScaleY = 1;

                if (Focus && n == EditableIndexToVisualColumn(cursor))
                {
                    if (blinkCursor)
                        cell.Decoration = Cell.TextDecoration.UnderLine;

                    blinkCursor = !blinkCursor;
                }
            }
        }

        protected override object? GetValidationValue()
            => RequireCompleteForValidation && !IsComplete ? null : Value;

        private int EditableCount => mask.Count(IsEditableMaskCharacter);

        private string ExtractRawValue(string value)
        {
            var result = new List<char>();
            int editableIndex = 0;
            foreach (char character in value)
            {
                while (editableIndex < EditableCount)
                {
                    int maskIndex = EditableIndexToMaskIndex(editableIndex);
                    if (maskIndex >= 0 && MatchesMask(character, mask[maskIndex]))
                    {
                        result.Add(character);
                        editableIndex++;
                        break;
                    }

                    editableIndex++;
                }

                if (editableIndex >= EditableCount)
                    break;
            }

            return new string(result.ToArray());
        }

        private string GetFormattedValue(bool includePlaceholders)
        {
            var result = new System.Text.StringBuilder();
            int rawIndex = 0;
            foreach (char maskCharacter in mask)
            {
                if (!IsEditableMaskCharacter(maskCharacter))
                {
                    result.Append(maskCharacter);
                    continue;
                }

                if (rawIndex < text.Length)
                    result.Append(text[rawIndex++]);
                else if (includePlaceholders)
                    result.Append(PlaceholderChar);
            }

            return result.ToString();
        }

        private short MaskColumnToEditableIndex(int column)
        {
            int editableIndex = 0;
            for (int maskIndex = 0; maskIndex < mask.Length; maskIndex++)
            {
                if (maskIndex >= column)
                    break;

                if (IsEditableMaskCharacter(mask[maskIndex]))
                    editableIndex++;
            }

            return (short)Math.Clamp(editableIndex, 0, text.Length);
        }

        private int EditableIndexToMaskIndex(int editableIndex)
        {
            int currentEditableIndex = 0;
            for (int maskIndex = 0; maskIndex < mask.Length; maskIndex++)
            {
                if (!IsEditableMaskCharacter(mask[maskIndex]))
                    continue;

                if (currentEditableIndex == editableIndex)
                    return maskIndex;

                currentEditableIndex++;
            }

            return -1;
        }

        private int EditableIndexToVisualColumn(int editableIndex)
        {
            int maskIndex = EditableIndexToMaskIndex(editableIndex);
            return maskIndex >= 0 ? maskIndex : Width - 1;
        }

        private bool TryGetCell(IList<Row> rows, int localX, out Cell cell)
        {
            int absoluteX = container.XOffset() + X + localX;
            int absoluteY = container.YOffset() + Y;
            if (absoluteX < container.XOffset() ||
                absoluteX >= container.XOffset() + container.Width ||
                absoluteY < container.YOffset() ||
                absoluteY >= container.YOffset() + container.Height ||
                absoluteY < 0 ||
                absoluteY >= rows.Count ||
                absoluteX < 0 ||
                absoluteX >= rows[absoluteY].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[absoluteY].Cells[absoluteX];
            return true;
        }

        private static bool IsEditableMaskCharacter(char maskCharacter)
            => maskCharacter is '0' or '9' or 'A' or 'a' or 'L' or '?' or '*';

        private static bool MatchesMask(char value, char maskCharacter)
            => maskCharacter switch
            {
                '0' or '9' => char.IsDigit(value),
                'A' or 'a' or 'L' or '?' => char.IsLetter(value),
                '*' => char.IsLetterOrDigit(value),
                _ => false
            };
    }
}
