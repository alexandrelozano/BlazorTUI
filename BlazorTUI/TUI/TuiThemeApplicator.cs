namespace BlazorTUI.TUI
{
    internal static class TuiThemeApplicator
    {
        public static void ApplyToScreen(Screen screen, TuiTheme theme)
        {
            ApplyToContainer(screen.TopContainer, theme);

            foreach (Dialog dialog in screen.Dialogs)
                ApplyToContainer(dialog, theme);

            if (screen.MenuBar is not null)
            {
                TuiColorPair menuColors = theme.Resolve(TuiThemeRole.Accent);
                screen.MenuBar.ForeColor = menuColors.ForeColor;
                screen.MenuBar.BackgroundColor = menuColors.BackgroundColor;
            }
        }

        private static void ApplyToContainer(Container container, TuiTheme theme)
        {
            switch (container)
            {
                case Dialog dialog:
                    ApplyColorPair(dialog, theme.Resolve(TuiThemeRole.Dialog));
                    break;
                case Frame frame:
                    ApplyColorPair(frame, theme.Resolve(TuiThemeRole.Border));
                    frame.BackgroundColor = theme.Surface.BackgroundColor;
                    break;
                case SplitPanel splitPanel:
                    ApplyColorPair(splitPanel, theme.Resolve(TuiThemeRole.Border));
                    splitPanel.BackgroundColor = theme.Surface.BackgroundColor;
                    break;
                case LayoutPanel layoutPanel:
                    ApplyColorPair(layoutPanel, theme.Resolve(TuiThemeRole.Surface));
                    break;
            }

            foreach (Control control in container.Controls)
                ApplyToControl(control, theme);

            foreach (Container child in container.Containers)
                ApplyToContainer(child, theme);
        }

        private static void ApplyToControl(Control control, TuiTheme theme)
        {
            TuiThemeRole role = control.ThemeRole == TuiThemeRole.Default
                ? GetDefaultRole(control)
                : control.ThemeRole;
            TuiColorPair colors = theme.Resolve(role, control.ThemeState);

            control.ForeColor = colors.ForeColor;
            control.BackgroundColor = colors.BackgroundColor;

        }

        private static TuiThemeRole GetDefaultRole(Control control)
            => control switch
            {
                TextBox or PasswordBox or TextArea or NumericBox or DateBox or Calendar or DatePicker or DateRangePicker or MonthPicker or TimeBox or ComboBox or ListBox or SearchBox or AutoCompleteBox or MaskedTextBox or MultiSelectComboBox => TuiThemeRole.Input,
                Button or CommandPalette or ContextMenu => TuiThemeRole.Action,
                CheckBox or ToggleSwitch or RadioButton or RadioGroup or TreeView or Slider or ColorPicker or Breadcrumb => TuiThemeRole.Selection,
                StatusBar or Toast or ValidationSummary => TuiThemeRole.Status,
                ProgressBar or Spinner or PictureBox or Sparkline or BarChart or Gauge => TuiThemeRole.Accent,
                GridView or Timeline or KeyValueList or Tooltip or Popover => TuiThemeRole.Surface,
                _ => TuiThemeRole.Surface
            };

        private static void ApplyColorPair(Frame frame, TuiColorPair colors)
        {
            frame.ForeColor = colors.ForeColor;
            frame.BackgroundColor = colors.BackgroundColor;
        }

        private static void ApplyColorPair(Dialog dialog, TuiColorPair colors)
        {
            dialog.ForeColor = colors.ForeColor;
            dialog.BackgroundColor = colors.BackgroundColor;
        }

        private static void ApplyColorPair(SplitPanel splitPanel, TuiColorPair colors)
        {
            splitPanel.ForeColor = colors.ForeColor;
            splitPanel.BackgroundColor = colors.BackgroundColor;
        }

        private static void ApplyColorPair(LayoutPanel layoutPanel, TuiColorPair colors)
        {
            layoutPanel.ForeColor = colors.ForeColor;
            layoutPanel.BackgroundColor = colors.BackgroundColor;
        }
    }
}
