using System.Globalization;
using System.Drawing;

namespace BlazorTUI.TUI
{
    internal static class TuiStatePersistence
    {
        public static TuiScreenState Export(Screen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);

            var state = new TuiScreenState
            {
                FocusedControlName = GetCurrentFocusedControlName(screen)
            };

            ExportContainer(screen.TopContainer, state);
            foreach (Dialog dialog in screen.Dialogs)
                ExportContainer(dialog, state);

            return state;
        }

        public static void Restore(Screen screen, TuiScreenState state)
        {
            ArgumentNullException.ThrowIfNull(screen);
            ArgumentNullException.ThrowIfNull(state);

            RestoreContainer(screen.TopContainer, state);
            foreach (Dialog dialog in screen.Dialogs)
                RestoreContainer(dialog, state);

            if (!string.IsNullOrWhiteSpace(state.FocusedControlName) &&
                TryFindControl(screen, state.FocusedControlName) is { TabStop: true })
            {
                screen.SetFocus(state.FocusedControlName);
            }
        }

        private static void ExportContainer(Container container, TuiScreenState screenState)
        {
            if (TryExportContainerState(container) is { } containerState)
                screenState.Containers[container.Name] = containerState;

            foreach (Control control in container.Controls)
            {
                if (TryExportControlState(control) is { } controlState)
                    screenState.Controls[control.Name] = controlState;
            }

            foreach (Container child in container.Containers)
                ExportContainer(child, screenState);
        }

        private static void RestoreContainer(Container container, TuiScreenState screenState)
        {
            if (screenState.Containers is not null &&
                screenState.Containers.TryGetValue(container.Name, out TuiElementState? containerState))
            {
                RestoreContainerState(container, containerState);
            }

            foreach (Control control in container.Controls)
            {
                if (screenState.Controls is not null &&
                    screenState.Controls.TryGetValue(control.Name, out TuiElementState? controlState))
                {
                    RestoreControlState(control, controlState);
                }
            }

            foreach (Container child in container.Containers)
                RestoreContainer(child, screenState);
        }

        private static TuiElementState? TryExportContainerState(Container container)
        {
            switch (container)
            {
                case TabControl tabControl:
                    var tabState = new TuiElementState(nameof(TabControl));
                    tabControl.ExportTabControlState(tabState);
                    return tabState;
                case SplitPanel splitPanel:
                    var splitState = new TuiElementState(nameof(SplitPanel));
                    splitState.SetInteger("SplitterPosition", splitPanel.SplitterPosition);
                    return splitState;
                default:
                    return null;
            }
        }

        private static TuiElementState? TryExportControlState(Control control)
        {
            switch (control)
            {
                case PasswordBox passwordBox:
                    var passwordState = new TuiElementState(nameof(PasswordBox));
                    passwordBox.ExportTextInputState(passwordState);
                    passwordState.SetBoolean("IsRevealed", passwordBox.IsRevealed);
                    return passwordState;
                case NumericBox numericBox:
                    var numericState = new TuiElementState(nameof(NumericBox));
                    numericBox.ExportTextInputState(numericState);
                    if (numericBox.Value.HasValue)
                        numericState.SetString("NumericValue", numericBox.Value.Value.ToString("R", CultureInfo.InvariantCulture));
                    numericState.SetBoolean("HasNumericValue", numericBox.Value.HasValue);
                    return numericState;
                case DateBox dateBox:
                    var dateState = new TuiElementState(nameof(DateBox));
                    dateBox.ExportTextInputState(dateState);
                    if (dateBox.Value.HasValue)
                        dateState.SetString("DateValue", dateBox.Value.Value.ToString("O", CultureInfo.InvariantCulture));
                    dateState.SetBoolean("HasDateValue", dateBox.Value.HasValue);
                    return dateState;
                case Calendar calendar:
                    var calendarState = new TuiElementState(nameof(Calendar));
                    calendar.ExportCalendarState(calendarState);
                    return calendarState;
                case DatePicker datePicker:
                    var datePickerState = new TuiElementState(nameof(DatePicker));
                    datePicker.ExportDatePickerState(datePickerState);
                    return datePickerState;
                case DateRangePicker dateRangePicker:
                    var dateRangePickerState = new TuiElementState(nameof(DateRangePicker));
                    dateRangePicker.ExportDateRangePickerState(dateRangePickerState);
                    return dateRangePickerState;
                case MonthPicker monthPicker:
                    var monthPickerState = new TuiElementState(nameof(MonthPicker));
                    monthPicker.ExportMonthPickerState(monthPickerState);
                    return monthPickerState;
                case TimeBox timeBox:
                    var timeState = new TuiElementState(nameof(TimeBox));
                    timeBox.ExportTextInputState(timeState);
                    if (timeBox.Value.HasValue)
                        timeState.SetString("TimeValue", timeBox.Value.Value.ToString("O", CultureInfo.InvariantCulture));
                    timeState.SetBoolean("HasTimeValue", timeBox.Value.HasValue);
                    return timeState;
                case TextBox textBox:
                    var textState = new TuiElementState(nameof(TextBox));
                    textBox.ExportTextInputState(textState);
                    return textState;
                case TextArea textArea:
                    var textAreaState = new TuiElementState(nameof(TextArea));
                    textArea.ExportTextAreaState(textAreaState);
                    return textAreaState;
                case CheckBox checkBox:
                    var checkBoxState = new TuiElementState(nameof(CheckBox));
                    checkBoxState.SetBoolean("Value", checkBox.Value);
                    return checkBoxState;
                case RadioButton radioButton:
                    var radioButtonState = new TuiElementState(nameof(RadioButton));
                    radioButtonState.SetBoolean("Value", radioButton.Value);
                    return radioButtonState;
                case ComboBox comboBox:
                    var comboState = new TuiElementState(nameof(ComboBox));
                    comboBox.ExportComboBoxState(comboState);
                    return comboState;
                case ListBox listBox:
                    var listState = new TuiElementState(nameof(ListBox));
                    listBox.ExportListBoxState(listState);
                    return listState;
                case RadioGroup radioGroup:
                    var radioGroupState = new TuiElementState(nameof(RadioGroup));
                    radioGroupState.SetInteger("SelectedIndex", radioGroup.SelectedIndex);
                    if (radioGroup.SelectedOption is not null)
                    {
                        radioGroupState.SetString("SelectedOption", radioGroup.SelectedOption.Name);
                        radioGroupState.SetString("SelectedValue", radioGroup.SelectedOption.Value);
                    }
                    return radioGroupState;
                case Breadcrumb breadcrumb:
                    var breadcrumbState = new TuiElementState(nameof(Breadcrumb));
                    breadcrumbState.SetInteger("SelectedIndex", breadcrumb.SelectedIndex);
                    if (breadcrumb.SelectedItem is not null)
                    {
                        breadcrumbState.SetString("SelectedItem", breadcrumb.SelectedItem.Name);
                        breadcrumbState.SetString("SelectedValue", breadcrumb.SelectedItem.Value);
                    }
                    return breadcrumbState;
                case TreeView treeView:
                    var treeState = new TuiElementState(nameof(TreeView));
                    treeView.ExportTreeViewState(treeState);
                    return treeState;
                case Slider slider:
                    var sliderState = new TuiElementState(nameof(Slider));
                    sliderState.SetInteger("Value", slider.Value);
                    return sliderState;
                case GridView gridView:
                    var gridState = new TuiElementState(nameof(GridView));
                    gridView.ExportGridViewState(gridState);
                    return gridState;
                case CommandPalette commandPalette:
                    var commandState = new TuiElementState(nameof(CommandPalette));
                    commandPalette.ExportCommandPaletteState(commandState);
                    return commandState;
                case ColorPicker colorPicker:
                    var colorState = new TuiElementState(nameof(ColorPicker));
                    colorState.SetInteger("ColorArgb", colorPicker.Color.ToArgb());
                    colorState.SetString("ColorName", colorPicker.Color.Name);
                    return colorState;
                case StatusBar statusBar:
                    var statusState = new TuiElementState(nameof(StatusBar));
                    statusState.SetString("Message", statusBar.Message);
                    foreach (StatusBarItem item in statusBar.Items)
                        statusState.SetString($"Item:{item.Name}:Text", item.Text);
                    return statusState;
                case ProgressBar progressBar:
                    var progressState = new TuiElementState(nameof(ProgressBar));
                    progressState.SetString("Value", progressBar.Value.ToString("R", CultureInfo.InvariantCulture));
                    progressState.SetString("Maximum", progressBar.Maximum.ToString("R", CultureInfo.InvariantCulture));
                    return progressState;
                case Label label:
                    var labelState = new TuiElementState(nameof(Label));
                    labelState.SetString("Text", label.Text);
                    return labelState;
                default:
                    return null;
            }
        }

        private static void RestoreContainerState(Container container, TuiElementState state)
        {
            switch (container)
            {
                case TabControl tabControl:
                    tabControl.RestoreTabControlState(state);
                    break;
                case SplitPanel splitPanel:
                    if (state.TryGetInteger("SplitterPosition", out int splitterPosition) &&
                        splitterPosition >= short.MinValue &&
                        splitterPosition <= short.MaxValue)
                    {
                        try
                        {
                            splitPanel.SplitterPosition = (short)splitterPosition;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }
                    break;
            }
        }

        private static void RestoreControlState(Control control, TuiElementState state)
        {
            switch (control)
            {
                case PasswordBox passwordBox:
                    passwordBox.RestoreTextInputState(state);
                    if (state.TryGetBoolean("IsRevealed", out bool isRevealed))
                        passwordBox.IsRevealed = isRevealed;
                    break;
                case NumericBox numericBox:
                    numericBox.RestoreTextInputState(state);
                    if (state.TryGetBoolean("HasNumericValue", out bool hasNumericValue) && !hasNumericValue)
                    {
                        numericBox.Value = null;
                    }
                    else if (state.TryGetString("NumericValue", out string numericValue) &&
                        double.TryParse(numericValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedNumber))
                    {
                        numericBox.Value = parsedNumber;
                    }
                    break;
                case DateBox dateBox:
                    dateBox.RestoreTextInputState(state);
                    if (state.TryGetBoolean("HasDateValue", out bool hasDateValue) && !hasDateValue)
                    {
                        dateBox.Value = null;
                    }
                    else if (state.TryGetString("DateValue", out string dateValue) &&
                        DateOnly.TryParseExact(dateValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedDate))
                    {
                        dateBox.Value = parsedDate;
                    }
                    break;
                case Calendar calendar:
                    calendar.RestoreCalendarState(state);
                    break;
                case DatePicker datePicker:
                    datePicker.RestoreDatePickerState(state);
                    break;
                case DateRangePicker dateRangePicker:
                    dateRangePicker.RestoreDateRangePickerState(state);
                    break;
                case MonthPicker monthPicker:
                    monthPicker.RestoreMonthPickerState(state);
                    break;
                case TimeBox timeBox:
                    timeBox.RestoreTextInputState(state);
                    if (state.TryGetBoolean("HasTimeValue", out bool hasTimeValue) && !hasTimeValue)
                    {
                        timeBox.Value = null;
                    }
                    else if (state.TryGetString("TimeValue", out string timeValue) &&
                        TimeOnly.TryParseExact(timeValue, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
                    {
                        timeBox.Value = parsedTime;
                    }
                    break;
                case TextBox textBox:
                    textBox.RestoreTextInputState(state);
                    break;
                case TextArea textArea:
                    textArea.RestoreTextAreaState(state);
                    break;
                case CheckBox checkBox:
                    if (state.TryGetBoolean("Value", out bool checkBoxValue))
                        checkBox.Value = checkBoxValue;
                    break;
                case RadioButton radioButton:
                    if (state.TryGetBoolean("Value", out bool radioButtonValue))
                        radioButton.Value = radioButtonValue;
                    break;
                case ComboBox comboBox:
                    comboBox.RestoreComboBoxState(state);
                    break;
                case ListBox listBox:
                    listBox.RestoreListBoxState(state);
                    break;
                case RadioGroup radioGroup:
                    RestoreRadioGroupState(radioGroup, state);
                    break;
                case Breadcrumb breadcrumb:
                    RestoreBreadcrumbState(breadcrumb, state);
                    break;
                case TreeView treeView:
                    treeView.RestoreTreeViewState(state);
                    break;
                case Slider slider:
                    if (state.TryGetInteger("Value", out int sliderValue) &&
                        sliderValue >= slider.Minimum &&
                        sliderValue <= slider.Maximum)
                    {
                        slider.Value = sliderValue;
                    }
                    break;
                case GridView gridView:
                    gridView.RestoreGridViewState(state);
                    break;
                case CommandPalette commandPalette:
                    commandPalette.RestoreCommandPaletteState(state);
                    break;
                case ColorPicker colorPicker:
                    RestoreColorPickerState(colorPicker, state);
                    break;
                case StatusBar statusBar:
                    RestoreStatusBarState(statusBar, state);
                    break;
                case ProgressBar progressBar:
                    RestoreProgressBarState(progressBar, state);
                    break;
                case Label label:
                    if (state.TryGetString("Text", out string text))
                        label.Text = text;
                    break;
            }
        }

        private static void RestoreRadioGroupState(RadioGroup radioGroup, TuiElementState state)
        {
            if (state.TryGetString("SelectedOption", out string selectedOption) &&
                radioGroup.Options.Any(option => option.Name == selectedOption))
            {
                radioGroup.SelectOption(selectedOption);
                return;
            }

            if (state.TryGetString("SelectedValue", out string selectedValue) &&
                radioGroup.Options.Any(option => option.Value == selectedValue))
            {
                radioGroup.SelectValue(selectedValue);
                return;
            }

            if (state.TryGetInteger("SelectedIndex", out int selectedIndex) &&
                selectedIndex >= -1 &&
                selectedIndex < radioGroup.Options.Count)
            {
                radioGroup.SelectedIndex = selectedIndex;
            }
        }

        private static void RestoreBreadcrumbState(Breadcrumb breadcrumb, TuiElementState state)
        {
            if (state.TryGetString("SelectedItem", out string selectedItem) &&
                breadcrumb.Items.Any(item => item.Name == selectedItem))
            {
                breadcrumb.SelectItem(selectedItem);
                return;
            }

            if (state.TryGetString("SelectedValue", out string selectedValue) &&
                breadcrumb.Items.Any(item => item.Value == selectedValue))
            {
                breadcrumb.SelectValue(selectedValue);
                return;
            }

            if (state.TryGetInteger("SelectedIndex", out int selectedIndex) &&
                selectedIndex >= -1 &&
                selectedIndex < breadcrumb.Items.Count)
            {
                breadcrumb.SelectedIndex = selectedIndex;
            }
        }

        private static void RestoreColorPickerState(ColorPicker colorPicker, TuiElementState state)
        {
            if (state.TryGetInteger("ColorArgb", out int argb))
            {
                colorPicker.Color = Color.FromArgb(argb);
                return;
            }

            if (state.TryGetString("ColorName", out string colorName))
                colorPicker.Color = Color.FromName(colorName);
        }

        private static void RestoreStatusBarState(StatusBar statusBar, TuiElementState state)
        {
            if (state.TryGetString("Message", out string message))
                statusBar.Message = message;

            foreach (StatusBarItem item in statusBar.Items)
            {
                if (state.TryGetString($"Item:{item.Name}:Text", out string itemText))
                    item.Text = itemText;
            }
        }

        private static void RestoreProgressBarState(ProgressBar progressBar, TuiElementState state)
        {
            if (state.TryGetString("Maximum", out string maximum) &&
                double.TryParse(maximum, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMaximum))
            {
                progressBar.Maximum = parsedMaximum;
            }

            if (state.TryGetString("Value", out string value) &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
            {
                progressBar.Value = parsedValue;
            }
        }

        private static string? GetCurrentFocusedControlName(Screen screen)
            => screen.Dialogs.Count == 0
                ? screen.TopContainer.GetCurrentFocusControl()?.Name
                : screen.Dialogs[^1].GetCurrentFocusControl()?.Name;

        private static Control? TryFindControl(Screen screen, string name)
        {
            Control? control = screen.TopContainer.GetControl(name);
            if (control is not null)
                return control;

            foreach (Dialog dialog in screen.Dialogs)
            {
                control = dialog.GetControl(name);
                if (control is not null)
                    return control;
            }

            return null;
        }
    }
}
