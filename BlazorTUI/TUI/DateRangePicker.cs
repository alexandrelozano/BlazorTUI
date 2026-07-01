using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class DateRangePicker : Control, IPopupControl
    {
        private const int PopupWidth = 22;
        private const int PopupHeight = 8;
        private DateOnly? startValue;
        private DateOnly? endValue;
        private DateOnly highlightedDate;
        private DateOnly displayedMonth;
        private DateBox.DateFormat format;
        private DateOnly? mouseRangeStartDate;
        private bool mouseRangeSelectionMoved;

        public DateOnly? StartValue
        {
            get => startValue;
            set => SetRange(value, endValue);
        }

        public DateOnly? EndValue
        {
            get => endValue;
            set => SetRange(startValue, value);
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

        public DateRangePickerSelectionTarget SelectionTarget { get; private set; }

        public bool HasCompleteRange => startValue.HasValue && endValue.HasValue;

        public bool EnableMouseRangeSelection { get; set; } = true;

        public event EventHandler<DateRangePickerValueChangedEventArgs>? ValueChanged;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public override string GetAccessibilitySummary()
        {
            string selected = (StartValue, EndValue) switch
            {
                ({ } start, { } end) => $"selected range {CultureOptions.FormatLongDate(start)} to {CultureOptions.FormatLongDate(end)}",
                ({ } start, null) => $"start date {CultureOptions.FormatLongDate(start)}, no end date",
                (null, { } end) => $"end date {CultureOptions.FormatLongDate(end)}, no start date",
                _ => "no date range selected"
            };
            string popupState = IsDropDownOpen ? "calendar open" : "calendar closed";
            return FormatAccessibilitySummary($"DateRangePicker {Name}: {selected}, selecting {SelectionTarget}, showing {DisplayedMonth.ToString("MMMM yyyy", CultureOptions.ResolvedCulture)}, {popupState}.");
        }

        public DateRangePicker(
            string name,
            DateOnly? startValue,
            DateOnly? endValue,
            DateBox.DateFormat format,
            short X,
            short Y,
            Color foreColor,
            Color backgroundColor,
            short width = 25,
            TuiCultureOptions? cultureOptions = null)
        {
            ValidateFormat(format);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)25);

            if (cultureOptions is not null)
                CultureOptions = cultureOptions;

            Name = name;
            this.format = format;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;

            SetRangeCore(startValue, endValue, raiseEvent: false, updateHighlight: false);
            DateOnly initialDate = this.startValue ?? this.endValue ?? DateOnly.FromDateTime(DateTime.Today);
            highlightedDate = initialDate;
            displayedMonth = FirstDayOfMonth(initialDate);
            SelectionTarget = this.startValue.HasValue && !this.endValue.HasValue
                ? DateRangePickerSelectionTarget.End
                : DateRangePickerSelectionTarget.Start;

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

            DateOnly date = startValue ?? endValue ?? highlightedDate;
            highlightedDate = date;
            displayedMonth = FirstDayOfMonth(date);
            SelectionTarget = startValue.HasValue && !endValue.HasValue
                ? DateRangePickerSelectionTarget.End
                : DateRangePickerSelectionTarget.Start;
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

        public void ClearRange()
            => SetRange(null, null);

        public void SetRange(DateOnly? startValue, DateOnly? endValue)
            => SetRangeCore(startValue, endValue, raiseEvent: true, updateHighlight: true);

        internal void ExportDateRangePickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (startValue.HasValue)
                state.SetString("StartValue", startValue.Value.ToString("O", CultureInfo.InvariantCulture));
            if (endValue.HasValue)
                state.SetString("EndValue", endValue.Value.ToString("O", CultureInfo.InvariantCulture));

            state.SetBoolean("HasStartValue", startValue.HasValue);
            state.SetBoolean("HasEndValue", endValue.HasValue);
            state.SetString("HighlightedDate", highlightedDate.ToString("O", CultureInfo.InvariantCulture));
            state.SetString("DisplayedMonth", displayedMonth.ToString("O", CultureInfo.InvariantCulture));
            state.SetInteger("SelectionTarget", (int)SelectionTarget);
            state.SetBoolean("IsDropDownOpen", IsDropDownOpen);
        }

        internal void RestoreDateRangePickerState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            DateOnly? restoredStart = null;
            DateOnly? restoredEnd = null;
            if ((!state.TryGetBoolean("HasStartValue", out bool hasStartValue) || hasStartValue) &&
                state.TryGetString("StartValue", out string startDate) &&
                DateOnly.TryParseExact(startDate, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedStart))
            {
                restoredStart = parsedStart;
            }

            if ((!state.TryGetBoolean("HasEndValue", out bool hasEndValue) || hasEndValue) &&
                state.TryGetString("EndValue", out string endDate) &&
                DateOnly.TryParseExact(endDate, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedEnd))
            {
                restoredEnd = parsedEnd;
            }

            SetRangeCore(restoredStart, restoredEnd, raiseEvent: false, updateHighlight: false);

            if (state.TryGetString("HighlightedDate", out string highlightedValue) &&
                DateOnly.TryParseExact(highlightedValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedHighlighted))
            {
                highlightedDate = parsedHighlighted;
            }
            else
            {
                highlightedDate = startValue ?? endValue ?? DateOnly.FromDateTime(DateTime.Today);
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

            if (state.TryGetInteger("SelectionTarget", out int selectionTarget) &&
                Enum.IsDefined((DateRangePickerSelectionTarget)selectionTarget))
            {
                SelectionTarget = (DateRangePickerSelectionTarget)selectionTarget;
            }
            else
            {
                SelectionTarget = startValue.HasValue && !endValue.HasValue
                    ? DateRangePickerSelectionTarget.End
                    : DateRangePickerSelectionTarget.Start;
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
                        ClearRange();
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
                    if (CommitHighlightedDate())
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
                    ClearRange();
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
            if (CommitHighlightedDate())
                CloseCalendar();

            NotifyClicked();
            return true;
        }

        public override bool BeginDrag(short X, short Y)
        {
            mouseRangeStartDate = null;
            mouseRangeSelectionMoved = false;

            if (!EnableMouseRangeSelection || !Visible || !IsDropDownOpen)
                return false;

            if (!TryGetCalendarDateAtLocalPoint(X, Y, out DateOnly date))
                return false;

            mouseRangeStartDate = date;
            container.TopContainer().SetFocus(Name);
            return true;
        }

        public override bool Drag(short startX, short startY, short currentX, short currentY)
        {
            if (!mouseRangeStartDate.HasValue ||
                !TryGetCalendarDateAtLocalPoint(currentX, currentY, out DateOnly currentDate))
            {
                return false;
            }

            if (currentX == startX && currentY == startY && !mouseRangeSelectionMoved)
                return false;

            mouseRangeSelectionMoved = true;
            DateOnly? previousStart = startValue;
            DateOnly? previousEnd = endValue;
            SetRange(mouseRangeStartDate.Value, currentDate);
            Highlight(currentDate);
            bool changed = previousStart != startValue || previousEnd != endValue;
            if (changed)
                NotifyClicked();

            return changed;
        }

        public override bool EndDrag(short startX, short startY, short currentX, short currentY)
        {
            bool changed = Drag(startX, startY, currentX, currentY);
            bool shouldClose = mouseRangeSelectionMoved;
            mouseRangeStartDate = null;
            mouseRangeSelectionMoved = false;
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

        protected override object? GetValidationValue()
            => startValue.HasValue && endValue.HasValue
                ? new DateRangePickerValue(startValue.Value, endValue.Value)
                : null;

        private void DrawClosedControl(IList<Row> rows)
        {
            string text = FormatRange();
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
                DrawCalendarCell(rows, 2, day, text, highlighted: false, muted: false, selected: false);
            }
        }

        private void DrawWeek(IList<Row> rows, int week)
        {
            for (int day = 0; day < 7; day++)
            {
                DateOnly date = GetDateAt(week, day);
                bool highlighted = date == highlightedDate;
                bool muted = date.Month != displayedMonth.Month;
                bool selected = IsSelectedRangeDate(date) || IsTentativeRangeDate(date);
                string text = date.Day.ToString("00", CultureInfo.InvariantCulture);
                DrawCalendarCell(rows, week + 3, day, text, highlighted, muted, selected);
            }
        }

        private void DrawCalendarCell(IList<Row> rows, int localY, int dayColumn, string text, bool highlighted, bool muted, bool selected)
        {
            int localX = dayColumn * 3;
            for (int offset = 0; offset < 3; offset++)
            {
                if (!TryGetCell(rows, localX + offset, localY, out Cell cell))
                    continue;

                Color foreground = muted ? Color.Gray : ForeColor;
                Color background = BackgroundColor;
                if (highlighted || selected)
                {
                    foreground = BackgroundColor;
                    background = ForeColor;
                }

                PrepareCell(cell, foreground, background);
                cell.Character = offset == 2 ? " " : TuiText.CellAt(text, offset);
            }
        }

        private bool CommitHighlightedDate()
        {
            if (SelectionTarget == DateRangePickerSelectionTarget.End && startValue.HasValue && !endValue.HasValue)
            {
                SetRange(startValue.Value, highlightedDate);
                return true;
            }

            SetRange(highlightedDate, null);
            SelectionTarget = DateRangePickerSelectionTarget.End;
            return false;
        }

        private bool IsSelectedRangeDate(DateOnly date)
        {
            if (startValue.HasValue && endValue.HasValue)
                return date >= startValue.Value && date <= endValue.Value;

            return date == startValue || date == endValue;
        }

        private bool IsTentativeRangeDate(DateOnly date)
        {
            if (SelectionTarget != DateRangePickerSelectionTarget.End || !startValue.HasValue || endValue.HasValue)
                return false;

            DateOnly start = startValue.Value <= highlightedDate ? startValue.Value : highlightedDate;
            DateOnly end = startValue.Value <= highlightedDate ? highlightedDate : startValue.Value;
            return date >= start && date <= end;
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

        private void SetRangeCore(DateOnly? startValue, DateOnly? endValue, bool raiseEvent, bool updateHighlight)
        {
            DateOnly? normalizedStart = startValue;
            DateOnly? normalizedEnd = endValue;
            if (normalizedStart.HasValue && normalizedEnd.HasValue && normalizedStart.Value > normalizedEnd.Value)
                (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);

            DateOnly? previousStart = this.startValue;
            DateOnly? previousEnd = this.endValue;
            if (previousStart == normalizedStart && previousEnd == normalizedEnd)
                return;

            this.startValue = normalizedStart;
            this.endValue = normalizedEnd;
            SelectionTarget = this.startValue.HasValue && !this.endValue.HasValue
                ? DateRangePickerSelectionTarget.End
                : DateRangePickerSelectionTarget.Start;

            if (updateHighlight)
            {
                if (this.endValue.HasValue)
                    Highlight(this.endValue.Value);
                else if (this.startValue.HasValue)
                    Highlight(this.startValue.Value);
            }

            if (raiseEvent)
            {
                ValueChanged?.Invoke(
                    this,
                    new DateRangePickerValueChangedEventArgs(previousStart, previousEnd, this.startValue, this.endValue));
            }
        }

        private string FormatRange()
        {
            string start = startValue.HasValue ? FormatDate(startValue.Value) : string.Empty;
            string end = endValue.HasValue ? FormatDate(endValue.Value) : string.Empty;
            if (start.Length == 0 && end.Length == 0)
                return string.Empty;

            return $"{start}..{end}";
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
