using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class DatePicker : Control, IPopupControl
    {
        private const int PopupWidth = 22;
        private const int PopupHeight = 8;
        private DateOnly? value;
        private DateOnly highlightedDate;
        private DateOnly displayedMonth;
        private DateBox.DateFormat format;
        private bool mouseDateSelectionMoved;

        public DateOnly? Value
        {
            get => value;
            set => SetValue(value);
        }

        public DateBox.DateFormat Format
        {
            get => format;
            set
            {
                ValidateFormat(value);
                format = value;
            }
        }

        public bool IsDropDownOpen { get; private set; }

        public bool IsCalendarOpen => IsDropDownOpen;

        public DateOnly DisplayedMonth => displayedMonth;

        public DateOnly HighlightedDate => highlightedDate;

        public bool EnableMouseDateSelection { get; set; } = true;

        public event EventHandler<DatePickerValueChangedEventArgs>? ValueChanged;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public override string GetAccessibilitySummary()
        {
            string selected = Value.HasValue
                ? $"selected date {CultureOptions.FormatLongDate(Value.Value)}"
                : "no date selected";
            string popupState = IsDropDownOpen ? "calendar open" : "calendar closed";
            return FormatAccessibilitySummary($"DatePicker {Name}: {selected}, showing {DisplayedMonth.ToString("MMMM yyyy", CultureOptions.ResolvedCulture)}, {popupState}.");
        }

        public DatePicker(
            string name,
            DateOnly? value,
            DateBox.DateFormat format,
            short X,
            short Y,
            Color foreColor,
            Color backgroundColor,
            short width = 13,
            TuiCultureOptions? cultureOptions = null)
        {
            ValidateFormat(format);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)13);

            if (cultureOptions is not null)
                CultureOptions = cultureOptions;

            DateOnly initialDate = value ?? DateOnly.FromDateTime(DateTime.Today);
            Name = name;
            this.value = value;
            this.format = format;
            highlightedDate = initialDate;
            displayedMonth = FirstDayOfMonth(initialDate);
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;

            LostFocus += (_, _) => CloseCalendar();
        }

        public void OpenCalendar()
        {
            if (!Visible || IsDropDownOpen)
                return;

            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }

            DateOnly date = value ?? highlightedDate;
            highlightedDate = date;
            displayedMonth = FirstDayOfMonth(date);
            IsDropDownOpen = true;
        }

        public void CloseCalendar()
            => IsDropDownOpen = false;

        public void ToggleCalendar()
        {
            if (IsDropDownOpen)
                CloseCalendar();
            else
                OpenCalendar();
        }

        internal void ExportDatePickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (value.HasValue)
                state.SetString("DateValue", value.Value.ToString("O", CultureInfo.InvariantCulture));
            state.SetBoolean("HasDateValue", value.HasValue);
            state.SetString("HighlightedDate", highlightedDate.ToString("O", CultureInfo.InvariantCulture));
            state.SetString("DisplayedMonth", displayedMonth.ToString("O", CultureInfo.InvariantCulture));
            state.SetBoolean("IsDropDownOpen", IsDropDownOpen);
        }

        internal void RestoreDatePickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetBoolean("HasDateValue", out bool hasDateValue) && !hasDateValue)
            {
                value = null;
            }
            else if (state.TryGetString("DateValue", out string dateValue) &&
                DateOnly.TryParseExact(dateValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedValue))
            {
                value = parsedValue;
            }

            if (state.TryGetString("HighlightedDate", out string highlightedValue) &&
                DateOnly.TryParseExact(highlightedValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedHighlighted))
            {
                highlightedDate = parsedHighlighted;
            }
            else
            {
                highlightedDate = value ?? DateOnly.FromDateTime(DateTime.Today);
            }

            if (state.TryGetString("DisplayedMonth", out string monthValue) &&
                DateOnly.TryParseExact(monthValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedMonth))
            {
                displayedMonth = FirstDayOfMonth(parsedMonth);
            }
            else
            {
                displayedMonth = FirstDayOfMonth(highlightedDate);
            }

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
                        OpenCalendar();
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
                    CloseCalendar();
                    return true;
                case "F4":
                    ToggleCalendar();
                    return true;
                case "Enter":
                case "Space":
                case " ":
                    SetValue(highlightedDate);
                    CloseCalendar();
                    NotifyClicked();
                    return true;
                case "ArrowLeft":
                    MoveHighlight(-1);
                    return true;
                case "ArrowRight":
                    MoveHighlight(1);
                    return true;
                case "ArrowUp":
                    MoveHighlight(-7);
                    return true;
                case "ArrowDown":
                    MoveHighlight(7);
                    return true;
                case "Home":
                    Highlight(new DateOnly(displayedMonth.Year, displayedMonth.Month, 1));
                    return true;
                case "End":
                    Highlight(new DateOnly(displayedMonth.Year, displayedMonth.Month, DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month)));
                    return true;
                case "PageUp":
                    MoveDisplayedMonth(-1);
                    return true;
                case "PageDown":
                    MoveDisplayedMonth(1);
                    return true;
                case "Backspace":
                case "Delete":
                    SetValue(null);
                    CloseCalendar();
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
                ToggleCalendar();
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
                    MoveDisplayedMonth(-1);
                else if (X == nextArrowColumn || X >= PopupWidth - 3)
                    MoveDisplayedMonth(1);

                NotifyClicked();
                return true;
            }

            if (popupY < 2)
                return false;

            int weekRow = popupY - 2;
            int dayColumn = X / 3;
            if (dayColumn < 0 || dayColumn >= 7 || weekRow < 0 || weekRow >= 6)
                return false;

            DateOnly clickedDate = GetDateAt(weekRow, dayColumn);
            Highlight(clickedDate);
            SetValue(clickedDate);
            CloseCalendar();
            NotifyClicked();
            return true;
        }

        public override bool BeginDrag(short X, short Y)
        {
            mouseDateSelectionMoved = false;
            if (!EnableMouseDateSelection ||
                !Visible ||
                !IsDropDownOpen ||
                !TryGetCalendarDateAtLocalPoint(X, Y, out _))
            {
                return false;
            }

            container.TopContainer().SetFocus(Name);
            return true;
        }

        public override bool Drag(short startX, short startY, short currentX, short currentY)
        {
            if (!TryGetCalendarDateAtLocalPoint(currentX, currentY, out DateOnly date))
                return false;

            if (currentX == startX && currentY == startY && !mouseDateSelectionMoved)
                return false;

            DateOnly? previousValue = value;
            DateOnly previousHighlight = highlightedDate;
            mouseDateSelectionMoved = true;
            Highlight(date);
            SetValue(date);
            bool changed = previousValue != value || previousHighlight != highlightedDate;
            if (changed)
                NotifyClicked();

            return changed;
        }

        public override bool EndDrag(short startX, short startY, short currentX, short currentY)
        {
            bool changed = mouseDateSelectionMoved && Drag(startX, startY, currentX, currentY);
            bool shouldClose = mouseDateSelectionMoved;
            mouseDateSelectionMoved = false;
            if (shouldClose)
            {
                CloseCalendar();
                return true;
            }

            return changed;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            DrawClosedControl(rows);
            if (IsDropDownOpen)
                DrawCalendar(rows);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && X >= this.X && X < this.X + Math.Max((int)Width, PopupWidth) &&
                Y >= this.Y && Y < this.Y + 1 + (IsDropDownOpen ? PopupHeight : 0);

        void IPopupControl.ClosePopup()
            => CloseCalendar();

        protected override object? GetValidationValue() => value;

        private void DrawClosedControl(IList<Row> rows)
        {
            string text = value.HasValue ? FormatDate(value.Value) : string.Empty;
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

        private void DrawCalendar(IList<Row> rows)
        {
            DrawCalendarHeader(rows);
            DrawWeekDayRow(rows);
            for (int week = 0; week < 6; week++)
                DrawWeek(rows, week);
        }

        private void DrawCalendarHeader(IList<Row> rows)
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

        private string GetHeaderText()
        {
            string title = displayedMonth.ToString("MMM yyyy", CultureOptions.ResolvedCulture);
            return $"‹ {title} ›";
        }

        private (int PreviousArrowColumn, int NextArrowColumn) GetHeaderArrowColumns()
        {
            string text = GetHeaderText();
            int headerWidth = Math.Min(PopupWidth, TuiText.VisualWidth(text));
            int start = Math.Max(0, (PopupWidth - headerWidth) / 2);
            int end = Math.Min(PopupWidth - 1, start + headerWidth - 1);
            return (start, end);
        }

        private void DrawWeekDayRow(IList<Row> rows)
        {
            string[] names = CultureOptions.ResolvedCulture.DateTimeFormat.AbbreviatedDayNames;
            int firstDay = (int)CultureOptions.ResolvedCulture.DateTimeFormat.FirstDayOfWeek;
            for (int day = 0; day < 7; day++)
            {
                string name = names[(firstDay + day) % 7];
                string text = name.Length >= 2 ? name[..2] : name.PadRight(2);
                DrawCalendarCell(rows, 2, day, text, highlighted: false, muted: false);
            }
        }

        private void DrawWeek(IList<Row> rows, int week)
        {
            for (int day = 0; day < 7; day++)
            {
                DateOnly date = GetDateAt(week, day);
                bool selected = value == date;
                bool highlighted = date == highlightedDate;
                bool muted = date.Month != displayedMonth.Month;
                string text = date.Day.ToString("00", CultureInfo.InvariantCulture);
                DrawCalendarCell(rows, week + 3, day, text, highlighted || selected, muted);
            }
        }

        private void DrawCalendarCell(IList<Row> rows, int localY, int dayColumn, string text, bool highlighted, bool muted)
        {
            int localX = dayColumn * 3;
            for (int offset = 0; offset < 3; offset++)
            {
                if (!TryGetCell(rows, localX + offset, localY, out Cell cell))
                    continue;

                Color foreground = muted ? Color.Gray : ForeColor;
                Color background = BackgroundColor;
                if (highlighted)
                {
                    foreground = BackgroundColor;
                    background = ForeColor;
                }

                PrepareCell(cell, foreground, background);
                cell.Character = offset == 2 ? " " : TuiText.CellAt(text, offset);
            }
        }

        private DateOnly GetDateAt(int weekRow, int dayColumn)
        {
            int firstDayOfWeek = (int)CultureOptions.ResolvedCulture.DateTimeFormat.FirstDayOfWeek;
            int monthStartDayOfWeek = (int)displayedMonth.DayOfWeek;
            int offset = (monthStartDayOfWeek - firstDayOfWeek + 7) % 7;
            return displayedMonth.AddDays(weekRow * 7 + dayColumn - offset);
        }

        private bool TryGetCalendarDateAtLocalPoint(short X, short Y, out DateOnly date)
        {
            date = default;
            if (!IsDropDownOpen || X < 0 || X >= PopupWidth || Y < 3 || Y >= PopupHeight + 1)
                return false;

            int popupY = Y - 1;
            int weekRow = popupY - 2;
            int dayColumn = X / 3;
            if (dayColumn < 0 || dayColumn >= 7 || weekRow < 0 || weekRow >= 6)
                return false;

            date = GetDateAt(weekRow, dayColumn);
            return true;
        }

        private void MoveHighlight(int days)
            => Highlight(highlightedDate.AddDays(days));

        private void MoveDisplayedMonth(int months)
        {
            DateOnly nextMonth = displayedMonth.AddMonths(months);
            int day = Math.Min(highlightedDate.Day, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
            Highlight(new DateOnly(nextMonth.Year, nextMonth.Month, day));
        }

        private void Highlight(DateOnly date)
        {
            highlightedDate = date;
            displayedMonth = FirstDayOfMonth(date);
        }

        private void SetValue(DateOnly? nextValue)
        {
            DateOnly? previousValue = value;
            if (previousValue == nextValue)
                return;

            value = nextValue;
            if (nextValue.HasValue)
                Highlight(nextValue.Value);
            ValueChanged?.Invoke(this, new DatePickerValueChangedEventArgs(previousValue, value));
        }

        private string FormatDate(DateOnly date)
            => CultureOptions.FormatDate(date, format);

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

        private static void ValidateFormat(DateBox.DateFormat format)
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
