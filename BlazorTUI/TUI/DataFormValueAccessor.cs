namespace BlazorTUI.TUI
{
    internal static class DataFormValueAccessor
    {
        public static object? Read(Control control)
            => control switch
            {
                NumericBox numericBox => numericBox.Value,
                DateBox dateBox => dateBox.Value,
                TimeBox timeBox => timeBox.Value,
                MaskedTextBox maskedTextBox => maskedTextBox.Value,
                PasswordBox passwordBox => passwordBox.Value,
                TextArea textArea => textArea.Value,
                TextBox textBox => textBox.Value,
                DateRangePicker dateRangePicker => dateRangePicker.StartValue.HasValue && dateRangePicker.EndValue.HasValue
                    ? new DateRangePickerValue(dateRangePicker.StartValue.Value, dateRangePicker.EndValue.Value)
                    : null,
                DatePicker datePicker => datePicker.Value,
                MonthPicker monthPicker => monthPicker.Value,
                Calendar calendar => calendar.Value,
                CheckBox checkBox => checkBox.Value,
                RadioButton radioButton => radioButton.Value,
                ComboBox comboBox => comboBox.SelectedItem,
                MultiSelectComboBox multiSelectComboBox => multiSelectComboBox.SelectedItems,
                RadioGroup radioGroup => radioGroup.SelectedValue ?? radioGroup.SelectedItem,
                ListBox listBox => listBox.SelectedItems,
                TreeView treeView => treeView.SelectedNode,
                ToggleSwitch toggleSwitch => toggleSwitch.Value,
                Slider slider => slider.Value,
                ColorPicker colorPicker => colorPicker.Color,
                _ => null
            };
    }
}
