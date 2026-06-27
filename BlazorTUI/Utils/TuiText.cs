using System.Globalization;
using System.Text;

namespace BlazorTUI.Utils
{
    internal static class TuiText
    {
        public static int TextElementCount(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int count = 0;
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
                count++;

            return count;
        }

        public static int VisualWidth(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int width = 0;
            foreach (TextElementInfo element in GetTextElements(text))
                width += element.Width;

            return width;
        }

        public static int VisualWidth(string? text, int textElementCount)
        {
            if (string.IsNullOrEmpty(text) || textElementCount <= 0)
                return 0;

            int width = 0;
            int remaining = textElementCount;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                if (remaining-- <= 0)
                    break;

                width += element.Width;
            }

            return width;
        }

        public static string CellAt(string? text, int visualColumn)
            => CellInfoAt(text, visualColumn).Character;

        public static TextCell CellInfoAt(string? text, int visualColumn)
        {
            if (string.IsNullOrEmpty(text) || visualColumn < 0)
                return new TextCell(" ", null, false);

            int visualOffset = 0;
            int textElementIndex = 0;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                int elementWidth = element.Width;
                if (elementWidth > 0 && visualColumn >= visualOffset && visualColumn < visualOffset + elementWidth)
                {
                    bool isContinuation = visualColumn > visualOffset;
                    return new TextCell(isContinuation ? " " : element.Text, textElementIndex, isContinuation);
                }

                visualOffset += elementWidth;
                textElementIndex++;
            }

            return new TextCell(" ", null, false);
        }

        public static string TruncateByVisualWidth(string? text, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0)
                return "";

            var builder = new StringBuilder();
            int visualWidth = 0;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                if (element.Width > 0 && visualWidth + element.Width > width)
                    break;

                builder.Append(element.Text);
                visualWidth += element.Width;
            }

            return builder.ToString();
        }

        public static string PadRightToVisualWidth(string? text, int width)
        {
            if (width <= 0)
                return "";

            string value = TruncateByVisualWidth(text, width);
            return value + new string(' ', Math.Max(0, width - VisualWidth(value)));
        }

        public static string CenterToVisualWidth(string? text, int width)
        {
            if (width <= 0)
                return "";

            string value = TruncateByVisualWidth(text, width);
            int valueWidth = VisualWidth(value);
            int leftPadding = Math.Max(0, (width - valueWidth) / 2);
            int rightPadding = Math.Max(0, width - valueWidth - leftPadding);
            return new string(' ', leftPadding) + value + new string(' ', rightPadding);
        }

        public static string SubstringByTextElements(string? text, int start, int length)
        {
            if (string.IsNullOrEmpty(text) || length <= 0)
                return "";

            int textElementCount = TextElementCount(text);
            int safeStart = Math.Clamp(start, 0, textElementCount);
            int safeLength = Math.Clamp(length, 0, textElementCount - safeStart);
            int stringStart = GetStringIndexFromTextElementIndex(text, safeStart);
            int stringEnd = GetStringIndexFromTextElementIndex(text, safeStart + safeLength);
            return text[stringStart..stringEnd];
        }

        public static string RemoveTextElements(string value, int start, int length)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (length <= 0 || value.Length == 0)
                return value;

            int textElementCount = TextElementCount(value);
            int safeStart = Math.Clamp(start, 0, textElementCount);
            int safeLength = Math.Clamp(length, 0, textElementCount - safeStart);
            if (safeLength == 0)
                return value;

            int stringStart = GetStringIndexFromTextElementIndex(value, safeStart);
            int stringEnd = GetStringIndexFromTextElementIndex(value, safeStart + safeLength);
            return value.Remove(stringStart, stringEnd - stringStart);
        }

        public static string InsertAtTextElement(string value, int textElementIndex, string insertValue)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(insertValue);

            int stringIndex = GetStringIndexFromTextElementIndex(value, textElementIndex);
            return value.Insert(stringIndex, insertValue);
        }

        public static int GetStringIndexFromTextElementIndex(string? text, int textElementIndex)
        {
            if (string.IsNullOrEmpty(text) || textElementIndex <= 0)
                return 0;

            int currentIndex = 0;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                if (currentIndex == textElementIndex)
                    return element.StringIndex;

                currentIndex++;
            }

            return text.Length;
        }

        public static int TextElementIndexFromStringIndex(string? text, int stringIndex)
        {
            if (string.IsNullOrEmpty(text) || stringIndex <= 0)
                return 0;

            int safeStringIndex = Math.Min(stringIndex, text.Length);
            int textElementIndex = 0;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                int elementEnd = element.StringIndex + element.Text.Length;
                if (safeStringIndex <= element.StringIndex)
                    return textElementIndex;

                if (safeStringIndex < elementEnd)
                    return textElementIndex + 1;

                textElementIndex++;
            }

            return textElementIndex;
        }

        public static int TextElementIndexFromVisualColumn(string? text, int visualColumn)
        {
            if (string.IsNullOrEmpty(text) || visualColumn <= 0)
                return 0;

            int visualOffset = 0;
            int textElementIndex = 0;
            foreach (TextElementInfo element in GetTextElements(text))
            {
                int elementEnd = visualOffset + element.Width;
                if (visualColumn <= visualOffset)
                    return textElementIndex;

                if (visualColumn < elementEnd)
                    return textElementIndex + 1;

                visualOffset = elementEnd;
                textElementIndex++;
            }

            return textElementIndex;
        }

        public static int PreviousTextElementIndex(string? text, int textElementIndex)
            => Math.Max(0, Math.Min(textElementIndex, TextElementCount(text)) - 1);

        public static int NextTextElementIndex(string? text, int textElementIndex)
            => Math.Min(TextElementCount(text), Math.Max(0, textElementIndex) + 1);

        public static IEnumerable<TextElementInfo> GetTextElements(string? text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
            int textElementIndex = 0;
            while (enumerator.MoveNext())
            {
                string element = enumerator.GetTextElement();
                yield return new TextElementInfo(
                    element,
                    enumerator.ElementIndex,
                    textElementIndex,
                    GetTextElementWidth(element));
                textElementIndex++;
            }
        }

        private static int GetTextElementWidth(string textElement)
        {
            bool hasVisibleNarrowRune = false;
            foreach (Rune rune in textElement.EnumerateRunes())
            {
                int width = GetRuneWidth(rune);
                if (width >= 2)
                    return 2;

                if (width == 1)
                    hasVisibleNarrowRune = true;
            }

            return hasVisibleNarrowRune ? 1 : 0;
        }

        private static int GetRuneWidth(Rune rune)
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or
                UnicodeCategory.NonSpacingMark or
                UnicodeCategory.EnclosingMark or
                UnicodeCategory.SpacingCombiningMark or
                UnicodeCategory.Format)
            {
                return 0;
            }

            int value = rune.Value;
            if (IsVariationSelector(value) || value == 0x200D)
                return 0;

            return IsWideCodePoint(value) ? 2 : 1;
        }

        private static bool IsVariationSelector(int value)
            => value is >= 0xFE00 and <= 0xFE0F or >= 0xE0100 and <= 0xE01EF;

        private static bool IsWideCodePoint(int value)
            => value is >= 0x1100 and <= 0x115F or
                >= 0x2329 and <= 0x232A or
                >= 0x2E80 and <= 0xA4CF or
                >= 0xAC00 and <= 0xD7A3 or
                >= 0xF900 and <= 0xFAFF or
                >= 0xFE10 and <= 0xFE19 or
                >= 0xFE30 and <= 0xFE6F or
                >= 0xFF00 and <= 0xFF60 or
                >= 0xFFE0 and <= 0xFFE6 or
                >= 0x1F000 and <= 0x1FAFF or
                >= 0x1FC00 and <= 0x1FFFD;

        public readonly record struct TextCell(string Character, int? TextElementIndex, bool IsContinuation);

        public readonly record struct TextElementInfo(string Text, int StringIndex, int TextElementIndex, int Width);
    }
}
