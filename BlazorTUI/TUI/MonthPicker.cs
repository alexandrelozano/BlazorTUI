using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class MonthPicker : Control, IPopupControl
    {
        private const int PopupWidth = 22;
        private const int PopupHeight = 5;
        private const int MonthCellWidth = 7;
        private const int MonthColumns = 3;
        private const int MonthRows = 4;
        private DateOnly? value;
        private DateOnly highlightedMonth;
        private int displayedYear;
        private MonthFormat format;

        public enum MonthFormat
        {
            YYYYMM,
            MMYYYY,
            MMMYYYY
        }

        public DateOnly? Value
        {
            get => value;
            set => SetValue(value);
        }

        public MonthFormat Format
        {
            get => format;
            set
            {
                ValidateFormat(value);
                format = value;
            }
        }

        public bool IsDropDownOpen { get; private set; }

        public bool IsMonthGridOpen => IsDropDownOpen;

        public int DisplayedYear => displayedYear;

        public DateOnly HighlightedMonth => highlightedMonth;

        public event EventHandler<MonthPickerValueChangedEventArgs>? ValueChanged;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public MonthPicker(
            string name,
            DateOnly? value,
            MonthFormat format,
            short X,
            short Y,
            Color foreColor,
            Color backgroundColor,
            short width = 10)
        {
            ValidateFormat(format);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)10);

            DateOnly initialMonth = FirstDayOfMonth(value ?? DateOnly.FromDateTime(DateTime.Today));
            Name = name;
            this.value = value.HasValue ? FirstDayOfMonth(value.Value) : null;
            this.format = format;
            highlightedMonth = initialMonth;
            displayedYear = initialMonth.Year;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;

            LostFocus += (_, _) => CloseMonthGrid();
        }

        public void OpenMonthGrid()
        {
            if (!Visible || IsDropDownOpen)
                return;

            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }

            DateOnly month = value ?? highlightedMonth;
            highlightedMonth = FirstDayOfMonth(month);
            displayedYear = highlightedMonth.Year;
            IsDropDownOpen = true;
        }

        public void CloseMonthGrid()
            => IsDropDownOpen = false;

        public void ToggleMonthGrid()
        {
            if (IsDropDownOpen)
                CloseMonthGrid();
            else
                OpenMonthGrid();
        }

        internal void ExportMonthPickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (value.HasValue)
                state.SetString("MonthValue", value.Value.ToString("O", CultureInfo.InvariantCulture));
            state.SetBoolean("HasMonthValue", value.HasValue);
            state.SetString("HighlightedMonth", highlightedMonth.ToString("O", CultureInfo.InvariantCulture));
            state.SetInteger("DisplayedYear", displayedYear);
            state.SetBoolean("IsDropDownOpen", IsDropDownOpen);
        }

        internal void RestoreMonthPickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetBoolean("HasMonthValue", out bool hasMonthValue) && !hasMonthValue)
            {
                value = null;
            }
            else if (state.TryGetString("MonthValue", out string monthValue) &&
                DateOnly.TryParseExact(monthValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedValue))
            {
                value = FirstDayOfMonth(parsedValue);
            }

            if (state.TryGetString("HighlightedMonth", out string highlightedValue) &&
                DateOnly.TryParseExact(highlightedValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedHighlighted))
            {
                highlightedMonth = FirstDayOfMonth(parsedHighlighted);
            }
            else
            {
                highlightedMonth = value ?? FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));
            }

            displayedYear = state.TryGetInteger("DisplayedYear", out int restoredYear) &&
                restoredYear >= 1 &&
                restoredYear <= 9999
                    ? restoredYear
                    : highlightedMonth.Year;

            IsDropDownOpen = state.TryGetBoolean("IsDropDownOpen", out bool isOpen) && isOpen && Visible;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (!Visible)
                return false;

            if (!IsDropDownOpen)
            {
                switch (key)
                {
                    case "Enter":
                    case "Space":
                    case " ":
                    case "F4":
                    case "ArrowDown":
                        OpenMonthGrid();
                        NotifyClicked();
                        return true;
                    case "Backspace":
                    case "Delete":
                        SetValue(null);
                        return true;
                    default:
                        return false;
                }
            }

            switch (key)
            {
                case "Escape":
                    CloseMonthGrid();
                    return true;
                case "F4":
                    ToggleMonthGrid();
                    return true;
                case "Enter":
                case "Space":
                case " ":
                    SetValue(highlightedMonth);
                    CloseMonthGrid();
                    NotifyClicked();
                    return true;
                case "ArrowLeft":
                    MoveHighlight(-1);
                    return true;
                case "ArrowRight":
                    MoveHighlight(1);
                    return true;
                case "ArrowUp":
                    MoveHighlight(-MonthColumns);
                    return true;
                case "ArrowDown":
                    MoveHighlight(MonthColumns);
                    return true;
                case "Home":
                    Highlight(new DateOnly(displayedYear, 1, 1));
                    return true;
                case "End":
                    Highlight(new DateOnly(displayedYear, 12, 1));
                    return true;
                case "PageUp":
                    MoveDisplayedYear(-1);
                    return true;
                case "PageDown":
                    MoveDisplayedYear(1);
                    return true;
                case "Backspace":
                case "Delete":
                    SetValue(null);
                    CloseMonthGrid();
                    return true;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Math.Max((int)Width, PopupWidth) || Y < 0)
                return false;

            container.TopContainer().SetFocus(Name);
            if (Y == 0 && X < Width)
            {
                ToggleMonthGrid();
                NotifyClicked();
                return true;
            }

            if (!IsDropDownOpen || Y >= PopupHeight + 1)
                return false;

            int popupY = Y - 1;
            if (popupY == 0)
            {
                (int previousArrowColumn, int nextArrowColumn) = GetHeaderArrowColumns();
                if (X == previousArrowColumn || X <= 2)
                    MoveDisplayedYear(-1);
                else if (X == nextArrowColumn || X >= PopupWidth - 3)
                    MoveDisplayedYear(1);

                NotifyClicked();
                return true;
            }

            int row = popupY - 1;
            int column = X / MonthCellWidth;
            if (row < 0 || row >= MonthRows || column < 0 || column >= MonthColumns)
                return false;

            int month = row * MonthColumns + column + 1;
            DateOnly clickedMonth = new(displayedYear, month, 1);
            Highlight(clickedMonth);
            SetValue(clickedMonth);
            CloseMonthGrid();
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            DrawClosedControl(rows);
            if (IsDropDownOpen)
                DrawMonthGrid(rows);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && X >= this.X && X < this.X + Math.Max((int)Width, PopupWidth) &&
                Y >= this.Y && Y < this.Y + 1 + (IsDropDownOpen ? PopupHeight : 0);

        void IPopupControl.ClosePopup()
            => CloseMonthGrid();

        protected override object? GetValidationValue() => value;

        private void DrawClosedControl(IList<Row> rows)
        {
            string text = value.HasValue ? FormatMonth(value.Value) : string.Empty;
            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                PrepareCell(cell, Focus ? BackgroundColor : ForeColor, Focus ? ForeColor : BackgroundColor);
                cell.Character = x switch
                {
                    0 => "[",
                    _ when x == Width - 2 => IsDropDownOpen ? "▲" : "▼",
                    _ when x == Width - 1 => "]",
                    _ => TuiText.CellAt(text, x - 1)
                };
            }
        }

        private void DrawMonthGrid(IList<Row> rows)
        {
            DrawYearHeader(rows);
            for (int row = 0; row < MonthRows; row++)
            {
                ClearPopupRow(rows, row + 2);
                for (int column = 0; column < MonthColumns; column++)
                {
                    int month = row * MonthColumns + column + 1;
                    DrawMonthCell(rows, row + 2, column, month);
                }
            }
        }

        private void DrawYearHeader(IList<Row> rows)
        {
            string text = GetHeaderText();
            string centered = TuiText.PadRightToVisualWidth(text, PopupWidth);
            int titleWidth = Math.Min(PopupWidth, TuiText.VisualWidth(text));
            int start = Math.Max(0, (PopupWidth - titleWidth) / 2);
            for (int x = 0; x < PopupWidth; x++)
            {
                if (!TryGetCell(rows, x, 1, out Cell cell))
                    continue;

                PrepareCell(cell, ForeColor, BackgroundColor);
                cell.Character = x < start ? " " : TuiText.CellAt(centered, x - start);
            }
        }

        private void ClearPopupRow(IList<Row> rows, int localY)
        {
            for (int x = 0; x < PopupWidth; x++)
            {
                if (!TryGetCell(rows, x, localY, out Cell cell))
                    continue;

                PrepareCell(cell, ForeColor, BackgroundColor);
            }
        }

        private void DrawMonthCell(IList<Row> rows, int localY, int column, int month)
        {
            DateOnly cellMonth = new(displayedYear, month, 1);
            bool selected = value == cellMonth;
            bool highlighted = highlightedMonth == cellMonth;
            string text = TuiText.CenterToVisualWidth(GetMonthName(month), MonthCellWidth);
            int localX = column * MonthCellWidth;

            for (int offset = 0; offset < MonthCellWidth; offset++)
            {
                if (!TryGetCell(rows, localX + offset, localY, out Cell cell))
                    continue;

                Color foreground = ForeColor;
                Color background = BackgroundColor;
                if (highlighted || selected)
                {
                    foreground = BackgroundColor;
                    background = ForeColor;
                }

                PrepareCell(cell, foreground, background);
                cell.Character = TuiText.CellAt(text, offset);
            }
        }

        private string GetHeaderText()
            => $"‹ {displayedYear.ToString("0000", CultureInfo.InvariantCulture)} ›";

        private (int PreviousArrowColumn, int NextArrowColumn) GetHeaderArrowColumns()
        {
            string text = GetHeaderText();
            int headerWidth = Math.Min(PopupWidth, TuiText.VisualWidth(text));
            int start = Math.Max(0, (PopupWidth - headerWidth) / 2);
            int end = Math.Min(PopupWidth - 1, start + headerWidth - 1);
            return (start, end);
        }

        private void MoveHighlight(int months)
            => Highlight(highlightedMonth.AddMonths(months));

        private void MoveDisplayedYear(int years)
            => Highlight(highlightedMonth.AddYears(years));

        private void Highlight(DateOnly month)
        {
            highlightedMonth = FirstDayOfMonth(month);
            displayedYear = highlightedMonth.Year;
        }

        private void SetValue(DateOnly? nextValue)
        {
            DateOnly? nextMonth = nextValue.HasValue ? FirstDayOfMonth(nextValue.Value) : null;
            DateOnly? previousValue = value;
            if (previousValue == nextMonth)
                return;

            value = nextMonth;
            if (nextMonth.HasValue)
                Highlight(nextMonth.Value);
            ValueChanged?.Invoke(this, new MonthPickerValueChangedEventArgs(previousValue, value));
        }

        private string FormatMonth(DateOnly month)
            => format switch
            {
                MonthFormat.YYYYMM => month.ToString("yyyy/MM", CultureInfo.InvariantCulture),
                MonthFormat.MMYYYY => month.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                MonthFormat.MMMYYYY => month.ToString("MMM yyyy", CultureInfo.CurrentCulture),
                _ => month.ToString("yyyy/MM", CultureInfo.InvariantCulture)
            };

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

        private static DateOnly FirstDayOfMonth(DateOnly date)
            => new(date.Year, date.Month, 1);

        private static string GetMonthName(int month)
        {
            string[] names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
            string name = month >= 1 && month <= names.Length ? names[month - 1] : "";
            return string.IsNullOrWhiteSpace(name)
                ? CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1]
                : name;
        }

        private static void ValidateFormat(MonthFormat format)
        {
            if (!Enum.IsDefined(format))
                throw new ArgumentOutOfRangeException(nameof(format));
        }

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
    }
}
