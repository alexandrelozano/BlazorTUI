using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class RadioGroup : Control
    {
        private readonly List<RadioGroupOption> options;
        private int selectedIndex;
        private RadioGroupOrientation orientation;

        public IReadOnlyList<RadioGroupOption> Options => options;

        public int SelectedIndex
        {
            get => selectedIndex;
            set => SelectIndex(value);
        }

        public RadioGroupOption? SelectedOption
            => selectedIndex >= 0 && selectedIndex < options.Count ? options[selectedIndex] : null;

        public string? SelectedItem => SelectedOption?.Text;

        public string? SelectedValue => SelectedOption?.Value;

        public RadioGroupOrientation Orientation
        {
            get => orientation;
            set
            {
                ValidateOrientation(value);
                orientation = value;
                UpdateHeight();
            }
        }

        public event EventHandler? SelectedIndexChanged;

        public event EventHandler<RadioGroupSelectionChangedEventArgs>? SelectionChanged;

        public RadioGroup(
            string name,
            IEnumerable<string> options,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            int selectedIndex = 0,
            RadioGroupOrientation orientation = RadioGroupOrientation.Vertical)
            : this(
                name,
                options.Select((text, index) => new RadioGroupOption($"option{index}", text)),
                X,
                Y,
                width,
                foreColor,
                backgroundColor,
                selectedIndex,
                orientation)
        {
        }

        public RadioGroup(
            string name,
            IEnumerable<RadioGroupOption> options,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            int selectedIndex = 0,
            RadioGroupOrientation orientation = RadioGroupOrientation.Vertical)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ValidateOrientation(orientation);

            this.options = options.Select(CloneOption).ToList();
            ValidateUniqueOptionNames(this.options);
            selectedIndex = NormalizeInitialSelectedIndex(selectedIndex, this.options.Count);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.selectedIndex = selectedIndex;
            this.orientation = orientation;
            UpdateHeight();
        }

        public void SelectIndex(int index)
        {
            ValidateSelectedIndex(index);
            SetSelectedIndex(index);
        }

        public void SelectOption(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = options.FindIndex(option => option.Name == name);
            if (index < 0)
                throw new ArgumentException("The option does not belong to this RadioGroup.", nameof(name));

            SetSelectedIndex(index);
        }

        public void SelectOption(RadioGroupOption option)
        {
            ArgumentNullException.ThrowIfNull(option);
            SelectOption(option.Name);
        }

        public void SelectItem(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            int index = options.FindIndex(option => option.Text == text);
            if (index < 0)
                throw new ArgumentException("The item does not belong to this RadioGroup.", nameof(text));

            SetSelectedIndex(index);
        }

        public void SelectValue(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            int index = options.FindIndex(option => option.Value == value);
            if (index < 0)
                throw new ArgumentException("The value does not belong to this RadioGroup.", nameof(value));

            SetSelectedIndex(index);
        }

        public RadioGroupOption AddOption(string name, string text, string? value = null)
        {
            var option = new RadioGroupOption(name, text, value);
            AddOption(option);
            return option;
        }

        public void AddOption(RadioGroupOption option)
        {
            ArgumentNullException.ThrowIfNull(option);
            if (options.Any(existing => existing.Name == option.Name))
                throw new InvalidOperationException($"A radio group option named '{option.Name}' already exists.");

            options.Add(CloneOption(option));
            if (selectedIndex < 0)
                SetSelectedIndex(0);
            UpdateHeight();
        }

        public bool RemoveOption(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = options.FindIndex(option => option.Name == name);
            return index >= 0 && RemoveOptionAt(index);
        }

        public bool RemoveOption(RadioGroupOption option)
        {
            ArgumentNullException.ThrowIfNull(option);
            int index = options.FindIndex(existing => existing.Name == option.Name);
            return index >= 0 && RemoveOptionAt(index);
        }

        public void ClearOptions()
        {
            if (options.Count == 0)
                return;

            SetOptionsAfterMutation(() => options.Clear(), -1);
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            switch (key)
            {
                case "ArrowDown":
                case "ArrowRight":
                    MoveSelection(1);
                    return options.Count > 0;
                case "ArrowUp":
                case "ArrowLeft":
                    MoveSelection(-1);
                    return options.Count > 0;
                case "Home":
                    MoveSelectionTo(0);
                    return options.Count > 0;
                case "End":
                    MoveSelectionTo(options.Count - 1);
                    return options.Count > 0;
                case "Enter":
                case "Space":
                case " ":
                    NotifyClicked();
                    return options.Count > 0;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || Y < 0 || X >= Width || Y >= Height)
                return false;

            int optionIndex = GetOptionIndexAt(X, Y);
            if (optionIndex < 0)
                return false;

            container.TopContainer().SetFocus(Name);
            SetSelectedIndex(optionIndex);
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            Clear(rows);

            if (orientation == RadioGroupOrientation.Vertical)
                RenderVertical(rows);
            else
                RenderHorizontal(rows);
        }

        protected override object? GetValidationValue() => SelectedValue ?? SelectedItem;

        private bool RemoveOptionAt(int index)
        {
            int nextSelectedIndex = selectedIndex;
            if (options.Count == 1)
                nextSelectedIndex = -1;
            else if (index < selectedIndex)
                nextSelectedIndex--;
            else if (index == selectedIndex)
                nextSelectedIndex = Math.Min(index, options.Count - 2);

            return SetOptionsAfterMutation(() => options.RemoveAt(index), nextSelectedIndex);
        }

        private bool SetOptionsAfterMutation(Action mutate, int nextSelectedIndex)
        {
            int previousSelectedIndex = selectedIndex;
            RadioGroupOption? previousSelectedOption = SelectedOption;

            mutate();
            selectedIndex = nextSelectedIndex;
            UpdateHeight();

            if (previousSelectedIndex != selectedIndex || !ReferenceEquals(previousSelectedOption, SelectedOption))
                RaiseSelectionChanged(previousSelectedIndex, previousSelectedOption);

            return true;
        }

        private void MoveSelection(int direction)
        {
            if (options.Count == 0)
                return;

            int currentIndex = selectedIndex < 0 ? 0 : selectedIndex;
            int nextIndex = Math.Clamp(currentIndex + direction, 0, options.Count - 1);
            SetSelectedIndex(nextIndex);
        }

        private void MoveSelectionTo(int index)
        {
            if (options.Count == 0)
                return;

            SetSelectedIndex(index);
        }

        private void SetSelectedIndex(int index)
        {
            if (selectedIndex == index)
                return;

            int previousSelectedIndex = selectedIndex;
            RadioGroupOption? previousSelectedOption = SelectedOption;
            selectedIndex = index;
            RaiseSelectionChanged(previousSelectedIndex, previousSelectedOption);
        }

        private void RaiseSelectionChanged(int previousSelectedIndex, RadioGroupOption? previousSelectedOption)
        {
            if (TuiEventScope.EventsSuppressed)
                return;

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(
                this,
                new RadioGroupSelectionChangedEventArgs(
                    previousSelectedIndex,
                    selectedIndex,
                    previousSelectedOption,
                    SelectedOption));
        }

        private void Clear(IList<Row> rows)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, x, y, out Cell cell))
                        continue;

                    PrepareCell(cell, ForeColor, BackgroundColor);
                }
            }
        }

        private void RenderVertical(IList<Row> rows)
        {
            for (int index = 0; index < options.Count && index < Height; index++)
                RenderOption(rows, options[index], index, 0, index, Width);
        }

        private void RenderHorizontal(IList<Row> rows)
        {
            int cursor = 0;
            for (int index = 0; index < options.Count && cursor < Width; index++)
            {
                int optionWidth = TuiText.VisualWidth(GetRenderedOptionText(options[index]));
                RenderOption(rows, options[index], index, cursor, 0, optionWidth);
                cursor += optionWidth + 1;
            }
        }

        private void RenderOption(
            IList<Row> rows,
            RadioGroupOption option,
            int optionIndex,
            int localX,
            int localY,
            int maximumWidth)
        {
            string text = GetRenderedOptionText(option, optionIndex);
            bool focusedSelection = Focus && optionIndex == selectedIndex;
            Color textForeColor = focusedSelection ? BackgroundColor : ForeColor;
            Color textBackgroundColor = focusedSelection ? ForeColor : BackgroundColor;

            int textWidth = TuiText.VisualWidth(text);
            for (int characterIndex = 0; characterIndex < textWidth && characterIndex < maximumWidth; characterIndex++)
            {
                int x = localX + characterIndex;
                if (x >= Width || !TryGetCell(rows, x, localY, out Cell cell))
                    continue;

                PrepareCell(cell, textForeColor, textBackgroundColor);
                cell.Character = TuiText.CellAt(text, characterIndex);
            }
        }

        private int GetOptionIndexAt(short x, short y)
        {
            if (orientation == RadioGroupOrientation.Vertical)
                return y < options.Count ? y : -1;

            int cursor = 0;
            for (int index = 0; index < options.Count; index++)
            {
                int optionWidth = TuiText.VisualWidth(GetRenderedOptionText(options[index]));
                if (x >= cursor && x < cursor + optionWidth)
                    return index;

                cursor += optionWidth + 1;
            }

            return -1;
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private void UpdateHeight()
            => Height = orientation == RadioGroupOrientation.Vertical
                ? (short)Math.Max(1, options.Count)
                : (short)1;

        private void ValidateSelectedIndex(int index)
        {
            if (index < -1 || index >= options.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static string GetRenderedOptionText(RadioGroupOption option)
            => $"( ) {option.Text}";

        private string GetRenderedOptionText(RadioGroupOption option, int optionIndex)
            => $"({(optionIndex == selectedIndex ? "●" : " ")}) {option.Text}";

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }

        private static RadioGroupOption CloneOption(RadioGroupOption option)
        {
            ArgumentNullException.ThrowIfNull(option);
            return new RadioGroupOption(option.Name, option.Text, option.Value);
        }

        private static void ValidateUniqueOptionNames(IEnumerable<RadioGroupOption> options)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (RadioGroupOption option in options)
            {
                if (!names.Add(option.Name))
                    throw new InvalidOperationException($"A radio group option named '{option.Name}' already exists.");
            }
        }

        private static int NormalizeInitialSelectedIndex(int selectedIndex, int optionCount)
        {
            if (optionCount == 0)
            {
                if (selectedIndex != -1 && selectedIndex != 0)
                    throw new ArgumentOutOfRangeException(nameof(selectedIndex));

                return -1;
            }

            if (selectedIndex < -1 || selectedIndex >= optionCount)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            return selectedIndex;
        }

        private static void ValidateOrientation(RadioGroupOrientation orientation)
        {
            if (!Enum.IsDefined(orientation))
                throw new ArgumentOutOfRangeException(nameof(orientation));
        }
    }
}
