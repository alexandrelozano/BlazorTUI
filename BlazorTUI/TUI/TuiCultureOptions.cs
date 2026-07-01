using System.Globalization;

namespace BlazorTUI.TUI
{
    public sealed class TuiCultureOptions
    {
        private CultureInfo? culture;
        private string requiredMessage = "This field is required.";
        private string invalidOptionMessage = "Value must be one of the configured options.";
        private string invalidNumberMessage = "Value must be numeric.";
        private string invalidDateMessage = "Value must be a valid date.";
        private string? currencyFormat;
        private string? numberFormat;

        public TuiCultureOptions()
        {
        }

        public TuiCultureOptions(CultureInfo culture)
        {
            Culture = culture;
        }

        public TuiCultureOptions(string cultureName)
            : this(CultureInfo.GetCultureInfo(cultureName))
        {
        }

        public static TuiCultureOptions Current => new();

        public static TuiCultureOptions Invariant => new(CultureInfo.InvariantCulture);

        public CultureInfo? Culture
        {
            get => culture;
            set => culture = value is null ? null : (CultureInfo)value.Clone();
        }

        public string RequiredMessage
        {
            get => requiredMessage;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                requiredMessage = value;
            }
        }

        public string InvalidOptionMessage
        {
            get => invalidOptionMessage;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                invalidOptionMessage = value;
            }
        }

        public string InvalidNumberMessage
        {
            get => invalidNumberMessage;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                invalidNumberMessage = value;
            }
        }

        public string InvalidDateMessage
        {
            get => invalidDateMessage;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                invalidDateMessage = value;
            }
        }

        public string? NumberFormat
        {
            get => numberFormat;
            set => numberFormat = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public string? CurrencyFormat
        {
            get => currencyFormat;
            set => currencyFormat = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public CultureInfo ResolvedCulture => culture ?? CultureInfo.CurrentCulture;

        public char DecimalSeparator
        {
            get
            {
                string separator = ResolvedCulture.NumberFormat.NumberDecimalSeparator;
                return string.IsNullOrEmpty(separator) ? '.' : separator[0];
            }
        }

        public string FormatDate(DateOnly value, DateBox.DateFormat format)
            => format switch
            {
                DateBox.DateFormat.DDMMYYYY => value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                DateBox.DateFormat.MMDDYYYY => value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                DateBox.DateFormat.YYYYMMDD => value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                DateBox.DateFormat.CultureShortDate => value.ToString(GetShortDatePattern(), ResolvedCulture),
                _ => value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
            };

        public bool TryParseDate(string value, DateBox.DateFormat format, out DateOnly date)
        {
            ArgumentNullException.ThrowIfNull(value);
            return DateOnly.TryParseExact(
                value,
                GetDatePattern(format),
                format == DateBox.DateFormat.CultureShortDate ? ResolvedCulture : CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        public string FormatLongDate(DateOnly value)
            => value.ToString("D", ResolvedCulture);

        public string FormatMonth(DateOnly value, MonthPicker.MonthFormat format)
            => format switch
            {
                MonthPicker.MonthFormat.YYYYMM => value.ToString("yyyy/MM", CultureInfo.InvariantCulture),
                MonthPicker.MonthFormat.MMYYYY => value.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                MonthPicker.MonthFormat.MMMYYYY => value.ToString("MMM yyyy", ResolvedCulture),
                MonthPicker.MonthFormat.CultureMonthYear => value.ToString("Y", ResolvedCulture),
                _ => value.ToString("yyyy/MM", CultureInfo.InvariantCulture)
            };

        public string FormatTime(TimeOnly value)
            => value.ToString("HH:mm", ResolvedCulture);

        public bool TryParseTime(string value, out TimeOnly time)
        {
            ArgumentNullException.ThrowIfNull(value);
            return TimeOnly.TryParseExact(value, "HH:mm", ResolvedCulture, DateTimeStyles.None, out time);
        }

        public string FormatNumber(double value, string? format = null)
            => value.ToString(format ?? NumberFormat ?? "N", ResolvedCulture);

        public string FormatCurrency(decimal value, string? format = null)
            => value.ToString(format ?? CurrencyFormat ?? "C", ResolvedCulture);

        public string FormatCurrency(double value, string? format = null)
            => ((decimal)value).ToString(format ?? CurrencyFormat ?? "C", ResolvedCulture);

        public string FormatValidationMessage(string message, params object?[] args)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            ArgumentNullException.ThrowIfNull(args);
            return args.Length == 0
                ? message
                : string.Format(ResolvedCulture, message, args);
        }

        internal string GetDatePattern(DateBox.DateFormat format)
            => format switch
            {
                DateBox.DateFormat.DDMMYYYY => "dd/MM/yyyy",
                DateBox.DateFormat.MMDDYYYY => "MM/dd/yyyy",
                DateBox.DateFormat.YYYYMMDD => "yyyy/MM/dd",
                DateBox.DateFormat.CultureShortDate => GetShortDatePattern(),
                _ => "yyyy/MM/dd"
            };

        internal int GetDateTextLength(DateBox.DateFormat format)
            => GetDatePattern(format).Length;

        internal IReadOnlySet<int> GetDateSeparatorIndexes(DateBox.DateFormat format)
        {
            string pattern = GetDatePattern(format);
            var indexes = new HashSet<int>();
            for (int index = 0; index < pattern.Length; index++)
            {
                char character = pattern[index];
                if (character != 'd' && character != 'M' && character != 'y')
                    indexes.Add(index);
            }

            return indexes;
        }

        internal string GetMonthName(int month)
        {
            string[] names = ResolvedCulture.DateTimeFormat.AbbreviatedMonthNames;
            string name = month >= 1 && month <= names.Length ? names[month - 1] : "";
            return string.IsNullOrWhiteSpace(name)
                ? CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1]
                : name;
        }

        private string GetShortDatePattern()
        {
            string pattern = ResolvedCulture.DateTimeFormat.ShortDatePattern;
            string normalized = NormalizeDatePattern(pattern);
            return string.IsNullOrWhiteSpace(normalized)
                ? "yyyy/MM/dd"
                : normalized;
        }

        private static string NormalizeDatePattern(string pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            string normalized = pattern
                .Replace("dddd", "dd", StringComparison.Ordinal)
                .Replace("ddd", "dd", StringComparison.Ordinal)
                .Replace("dd", "d", StringComparison.Ordinal)
                .Replace("d", "dd", StringComparison.Ordinal)
                .Replace("MMMM", "MM", StringComparison.Ordinal)
                .Replace("MMM", "MM", StringComparison.Ordinal)
                .Replace("MM", "M", StringComparison.Ordinal)
                .Replace("M", "MM", StringComparison.Ordinal)
                .Replace("yyyy", "y", StringComparison.Ordinal)
                .Replace("yyy", "y", StringComparison.Ordinal)
                .Replace("yy", "y", StringComparison.Ordinal)
                .Replace("y", "yyyy", StringComparison.Ordinal);

            return normalized.Length == 0 ? "yyyy/MM/dd" : normalized;
        }
    }
}
