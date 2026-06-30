using System.Drawing;

namespace BlazorTUI.TUI
{
    public abstract class Control
    {
        private bool focus;
        private bool isValid = true;
        private string validationMessage = "";
        private bool validationAppliedErrorColors;
        private Color validationPreviousForeColor;
        private Color validationPreviousBackgroundColor;
        private TuiThemeState validationPreviousThemeState;

        public string name { get; set; } = "";

        public string Name
        {
            get => name;
            set => name = ValidateName(value);
        }

        public Container container { get; set; } = null!;

        public Container? ParentContainer => container;

        public short X { get; set; }

        public short Y { get; set; }

        public short width { get; set; }

        public short Width { get => width; set => width = value; }

        public short height { get; set; }

        public short Height { get => height; set => height = value; }

        public Color foreColor { get; set; }

        public Color ForeColor { get => foreColor; set => foreColor = value; }

        public Color backgroundColor { get; set; }

        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

        public bool Focus
        {
            get => focus;
            set
            {
                if (value == focus)
                    return;

                focus = value;
                if (TuiEventScope.EventsSuppressed)
                    return;

                if (value)
                {
                    OnFocus?.Invoke();
                    GotFocus?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnLostFocus?.Invoke();
                    LostFocus?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsFocused { get => Focus; set => Focus = value; }

        public bool TabStop { get; set; }

        public short TabIndex { get; set; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        public TuiThemeRole ThemeRole { get; set; } = TuiThemeRole.Default;

        public TuiThemeState ThemeState { get; set; } = TuiThemeState.Normal;

        public bool IsRequired { get; set; }

        public string RequiredMessage { get; set; } = "This field is required.";

        public bool IsValid => isValid;

        public string ValidationMessage => validationMessage;

        public bool HasValidationError => !isValid;

        public TuiValidationRuleCollection ValidationRules { get; } = new();

        public bool ShowValidationMessage { get; set; } = true;

        public Color ValidationErrorForeColor { get; set; } = Color.White;

        public Color ValidationErrorBackgroundColor { get; set; } = Color.DarkRed;

        public Color ValidationMessageForeColor { get; set; } = Color.White;

        public Color ValidationMessageBackgroundColor { get; set; } = Color.DarkRed;

        public Action<Control>? OnClick;

        public Action? OnFocus;

        public Action? OnLostFocus;

        public event EventHandler? Clicked;

        public event EventHandler? GotFocus;

        public event EventHandler? LostFocus;

        public event EventHandler<TuiValidationChangedEventArgs>? ValidationChanged;

        public abstract void Render(IList<Row> rows);

        public virtual bool KeyDown(string key, bool shiftKey) => false;

        public virtual bool Click(short X, short Y) => false;

        public virtual bool Validate()
        {
            if (!Visible)
            {
                ClearValidation();
                return true;
            }

            object? value = GetValidationValue();
            if (IsRequired && IsEmptyValidationValue(value))
            {
                SetValidationResult(false, RequiredMessage);
                return false;
            }

            foreach (TuiValidationRule rule in ValidationRules)
            {
                if (!rule.IsValid(value))
                {
                    SetValidationResult(false, rule.Message);
                    return false;
                }
            }

            SetValidationResult(true, "");
            return true;
        }

        public void ClearValidation()
            => SetValidationResult(true, "");

        internal void NotifyClicked()
        {
            if (TuiEventScope.EventsSuppressed)
                return;

            OnClick?.Invoke(this);
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected static string ValidateName(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }

        protected virtual object? GetValidationValue() => null;

        protected virtual bool IsEmptyValidationValue(object? value)
            => value switch
            {
                null => true,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                bool boolValue => !boolValue,
                System.Collections.ICollection collection => collection.Count == 0,
                System.Collections.IEnumerable enumerable => !enumerable.Cast<object?>().Any(),
                _ => false
            };

        private void SetValidationResult(bool valid, string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            bool changed = isValid != valid || validationMessage != message;
            isValid = valid;
            validationMessage = message;

            if (valid)
                RestoreValidationColors();
            else
                ApplyValidationColors();

            if (changed && !TuiEventScope.EventsSuppressed)
                ValidationChanged?.Invoke(this, new TuiValidationChangedEventArgs(isValid, validationMessage));
        }

        private void ApplyValidationColors()
        {
            if (validationAppliedErrorColors)
                return;

            validationPreviousForeColor = foreColor;
            validationPreviousBackgroundColor = backgroundColor;
            validationPreviousThemeState = ThemeState;
            foreColor = ValidationErrorForeColor;
            backgroundColor = ValidationErrorBackgroundColor;
            ThemeState = TuiThemeState.Error;
            validationAppliedErrorColors = true;
        }

        private void RestoreValidationColors()
        {
            if (!validationAppliedErrorColors)
                return;

            foreColor = validationPreviousForeColor;
            backgroundColor = validationPreviousBackgroundColor;
            ThemeState = validationPreviousThemeState;
            validationAppliedErrorColors = false;
        }
    }
}
