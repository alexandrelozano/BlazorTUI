using System.Drawing;
using System.Globalization;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Calendar : Control
    {
        private const int CalendarWidth = 22;
        private const int CalendarHeight = 8;
        private readonly HashSet<DateOnly> disabledDates = new();
        private DateOnly? value;
        private DateOnly highlightedDate;
        private DateOnly displayedMonth;
        private DateOnly? minDate;
        private DateOnly? maxDate;

        public DateOnly? Value
        {
            get => value;
            set => SetValue(value, throwIfUnavailable: true);
        }

        public DateOnly? SelectedDate
        {
            get => value;
            set => Value = value;
        }

        public DateOnly HighlightedDate => highlightedDate;

        public DateOnly DisplayedMonth => displayedMonth;

        public DateOnly? MinDate
        {
            get => minDate;
            set
            {
                if (value.HasValue && maxDate.HasValue && value.Value > maxDate.Value)
                    throw new ArgumentOutOfRangeException(nameof(value));

                minDate = value;
                EnsureAvailableState();
            }
        }

        public DateOnly? MaxDate
        {
            get => maxDate;
            set
            {
                if (value.HasValue && minDate.HasValue && value.Value < minDate.Value)
                    throw new ArgumentOutOfRangeException(nameof(value));

                maxDate = value;
                EnsureAvailableState();
            }
        }

        public IReadOnlyCollection<DateOnly> DisabledDates => disabledDates;

        public event EventHandler<CalendarDateSelectedEventArgs>? DateSelected;

        public event EventHandler<CalendarDateSelectedEventArgs>? ValueChanged;

        public Calendar(
            string name,
            DateOnly? value,
            short X,
            short Y,
            Color foreColor,
            Color backgroundColor,
            short width = CalendarWidth,
            DateOnly? minDate = null,
            DateOnly? maxDate = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)CalendarWidth);
            if (minDate.HasValue && maxDate.HasValue && minDate.Value > maxDate.Value)
                throw new ArgumentOutOfRangeException(nameof(minDate));

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = CalendarHeight;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.minDate = minDate;
            this.maxDate = maxDate;

            DateOnly initialDate = value ?? DateOnly.FromDateTime(DateTime.Today);
            highlightedDate = initialDate;
            displayedMonth = FirstDayOfMonth(initialDate);
            SetValue(value, throwIfUnavailable: true, raiseEvent: false);
        }

        public bool AddDisabledDate(DateOnly date)
        {
            bool added = disabledDates.Add(date);
            if (added)
                EnsureAvailableState();

            return added;
        }

        public bool RemoveDisabledDate(DateOnly date)
            => disabledDates.Remove(date);

        public void ClearDisabledDates()
            => disabledDates.Clear();

        public bool IsDateEnabled(DateOnly date)
            => (!minDate.HasValue || date >= minDate.Value) &&
                (!maxDate.HasValue || date <= maxDate.Value) &&
                !disabledDates.Contains(date);

        public void DisplayMonth(DateOnly month)
        {
            highlightedDate = ClampToDateRange(month);
            displayedMonth = FirstDayOfMonth(highlightedDate);
        }

        internal void ExportCalendarState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (value.HasValue)
                state.SetString("DateValue", value.Value.ToString("O", CultureInfo.InvariantCulture));

            state.SetBoolean("HasDateValue", value.HasValue);
            state.SetString("HighlightedDate", highlightedDate.ToString("O", CultureInfo.InvariantCulture));
            state.SetString("DisplayedMonth", displayedMonth.ToString("O", CultureInfo.InvariantCulture));
        }

        internal void RestoreCalendarState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetBoolean("HasDateValue", out bool hasDateValue) && !hasDateValue)
            {
                value = null;
            }
            else if (state.TryGetString("DateValue", out string dateValue) &&
                DateOnly.TryParseExact(dateValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedValue) &&
                IsDateEnabled(parsedValue))
            {
                value = parsedValue;
            }

            if (state.TryGetString("HighlightedDate", out string highlightedValue) &&
                DateOnly.TryParseExact(highlightedValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedHighlighted))
            {
                highlightedDate = ClampToDateRange(parsedHighlighted);
            }
            else
            {
                highlightedDate = ClampToDateRange(value ?? DateOnly.FromDateTime(DateTime.Today));
            }

            if (state.TryGetString("DisplayedMonth", out string monthValue) &&
                DateOnly.TryParseExact(monthValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedMonth))
            {
                displayedMonth = FirstDayOfMonth(ClampToDateRange(parsedMonth));
            }
            else
            {
                displayedMonth = FirstDayOfMonth(highlightedDate);
            }
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (!Visible)
                return false;

            switch (key)
            {
                case "Enter":
                case "Space":
                case " ":
                    SelectHighlightedDate();
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
                    SetValue(null, throwIfUnavailable: false);
                    return true;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= CalendarWidth || Y < 0 || Y >= CalendarHeight)
                return false;

            container.TopContainer().SetFocus(Name);
            if (Y == 0)
            {
                (int previousArrowColumn, int nextArrowColumn) = GetHeaderArrowColumns();
                if (X == previousArrowColumn || X <= 2)
                    MoveDisplayedMonth(-1);
                else if (X == nextArrowColumn || X >= CalendarWidth - 3)
                    MoveDisplayedMonth(1);

                NotifyClicked();
                return true;
            }

            if (Y < 2)
                return false;

            int weekRow = Y - 2;
            int dayColumn = X / 3;
            if (dayColumn < 0 || dayColumn >= 7 || weekRow < 0 || weekRow >= 6)
                return false;

            DateOnly clickedDate = GetDateAt(weekRow, dayColumn);
            Highlight(clickedDate);
            SelectHighlightedDate();
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            ClearSurface(rows);
            DrawCalendarHeader(rows);
            DrawWeekDayRow(rows);
            for (int week = 0; week < 6; week++)
                DrawWeek(rows, week);
        }

        protected override object? GetValidationValue() => value;

        private void ClearSurface(IList<Row> rows)
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

        private void DrawCalendarHeader(IList<Row> rows)
        {
            string text = GetHeaderText();
            string centered = TuiText.PadRightToVisualWidth(text, CalendarWidth);
            int titleWidth = Math.Min(CalendarWidth, TuiText.VisualWidth(text));
            int start = Math.Max(0, (CalendarWidth - titleWidth) / 2);
            for (int x = 0; x < CalendarWidth; x++)
            {
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                PrepareCell(cell, ForeColor, BackgroundColor);
                cell.Character = x < start ? " " : TuiText.CellAt(centered, x - start);
            }
        }

        private string GetHeaderText()
        {
            string title = displayedMonth.ToString("MMM yyyy", CultureInfo.CurrentCulture);
            return $"‹ {title} ›";
        }

        private (int PreviousArrowColumn, int NextArrowColumn) GetHeaderArrowColumns()
        {
            string text = GetHeaderText();
            int headerWidth = Math.Min(CalendarWidth, TuiText.VisualWidth(text));
            int start = Math.Max(0, (CalendarWidth - headerWidth) / 2);
            int end = Math.Min(CalendarWidth - 1, start + headerWidth - 1);
            return (start, end);
        }

        private void DrawWeekDayRow(IList<Row> rows)
        {
            string[] names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            int firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            for (int day = 0; day < 7; day++)
            {
                string name = names[(firstDay + day) % 7];
                string text = name.Length >= 2 ? name[..2] : name.PadRight(2);
                DrawCalendarCell(rows, 1, day, text, selected: false, highlighted: false, muted: false, disabled: false);
            }
        }

        private void DrawWeek(IList<Row> rows, int week)
        {
            for (int day = 0; day < 7; day++)
            {
                DateOnly date = GetDateAt(week, day);
                bool selected = value == date;
                bool highlighted = Focus && date == highlightedDate;
                bool muted = date.Month != displayedMonth.Month;
                bool disabled = !IsDateEnabled(date);
                string text = date.Day.ToString("00", CultureInfo.InvariantCulture);
                DrawCalendarCell(rows, week + 2, day, text, selected, highlighted, muted, disabled);
            }
        }

        private void DrawCalendarCell(
            IList<Row> rows,
            int localY,
            int dayColumn,
            string text,
            bool selected,
            bool highlighted,
            bool muted,
            bool disabled)
        {
            int localX = dayColumn * 3;
            for (int offset = 0; offset < 3; offset++)
            {
                if (!TryGetCell(rows, localX + offset, localY, out Cell cell))
                    continue;

                Color foreground = disabled || muted ? Color.Gray : ForeColor;
                Color background = BackgroundColor;
                if (selected || highlighted)
                {
                    foreground = BackgroundColor;
                    background = disabled ? Color.Gray : ForeColor;
                }

                PrepareCell(cell, foreground, background);
                cell.Character = offset == 2 ? " " : TuiText.CellAt(text, offset);
            }
        }

        private DateOnly GetDateAt(int weekRow, int dayColumn)
        {
            int firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int monthStartDayOfWeek = (int)displayedMonth.DayOfWeek;
            int offset = (monthStartDayOfWeek - firstDayOfWeek + 7) % 7;
            return displayedMonth.AddDays(weekRow * 7 + dayColumn - offset);
        }

        private void SelectHighlightedDate()
        {
            if (IsDateEnabled(highlightedDate))
                SetValue(highlightedDate, throwIfUnavailable: false);
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
            highlightedDate = ClampToDateRange(date);
            displayedMonth = FirstDayOfMonth(highlightedDate);
        }

        private DateOnly ClampToDateRange(DateOnly date)
        {
            if (minDate.HasValue && date < minDate.Value)
                return minDate.Value;

            if (maxDate.HasValue && date > maxDate.Value)
                return maxDate.Value;

            return date;
        }

        private void SetValue(DateOnly? nextValue, bool throwIfUnavailable, bool raiseEvent = true)
        {
            if (nextValue.HasValue && !IsDateEnabled(nextValue.Value))
            {
                if (throwIfUnavailable)
                    throw new ArgumentOutOfRangeException(nameof(nextValue));

                return;
            }

            DateOnly? previousValue = value;
            if (previousValue == nextValue)
                return;

            value = nextValue;
            if (nextValue.HasValue)
                Highlight(nextValue.Value);

            if (!raiseEvent)
                return;

            var args = new CalendarDateSelectedEventArgs(previousValue, value);
            ValueChanged?.Invoke(this, args);
            DateSelected?.Invoke(this, args);
        }

        private void EnsureAvailableState()
        {
            if (value.HasValue && !IsDateEnabled(value.Value))
                SetValue(null, throwIfUnavailable: false);

            Highlight(highlightedDate);
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

        private static DateOnly FirstDayOfMonth(DateOnly date)
            => new(date.Year, date.Month, 1);

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
