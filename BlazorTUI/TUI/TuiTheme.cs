using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class TuiTheme
    {
        public TuiTheme(
            string name,
            TuiColorPair normal,
            TuiColorPair? focus = null,
            TuiColorPair? disabled = null,
            TuiColorPair? error = null,
            TuiColorPair? selected = null,
            TuiColorPair? surface = null,
            TuiColorPair? border = null,
            TuiColorPair? input = null,
            TuiColorPair? action = null,
            TuiColorPair? selection = null,
            TuiColorPair? status = null,
            TuiColorPair? dialog = null,
            TuiColorPair? accent = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(normal);

            Name = name;
            Normal = normal.Clone();
            Focus = (focus ?? new TuiColorPair(normal.BackgroundColor, normal.ForeColor)).Clone();
            Disabled = (disabled ?? new TuiColorPair(Color.Gray, normal.BackgroundColor)).Clone();
            Error = (error ?? new TuiColorPair(Color.White, Color.DarkRed)).Clone();
            Selected = (selected ?? Focus).Clone();
            Surface = (surface ?? normal).Clone();
            Border = (border ?? normal).Clone();
            Input = (input ?? normal).Clone();
            Action = (action ?? normal).Clone();
            Selection = (selection ?? normal).Clone();
            Status = (status ?? normal).Clone();
            Dialog = (dialog ?? normal).Clone();
            Accent = (accent ?? normal).Clone();
        }

        public string Name { get; }

        public TuiColorPair Normal { get; }

        public TuiColorPair Focus { get; }

        public TuiColorPair Disabled { get; }

        public TuiColorPair Error { get; }

        public TuiColorPair Selected { get; }

        public TuiColorPair Surface { get; }

        public TuiColorPair Border { get; }

        public TuiColorPair Input { get; }

        public TuiColorPair Action { get; }

        public TuiColorPair Selection { get; }

        public TuiColorPair Status { get; }

        public TuiColorPair Dialog { get; }

        public TuiColorPair Accent { get; }

        public static TuiTheme Classic => new(
            "Classic",
            new TuiColorPair(Color.Yellow, Color.Blue),
            focus: new TuiColorPair(Color.Blue, Color.Yellow),
            disabled: new TuiColorPair(Color.Gray, Color.Blue),
            error: new TuiColorPair(Color.White, Color.DarkRed),
            selected: new TuiColorPair(Color.Blue, Color.Yellow),
            surface: new TuiColorPair(Color.Yellow, Color.Blue),
            border: new TuiColorPair(Color.Yellow, Color.Blue),
            input: new TuiColorPair(Color.Yellow, Color.Black),
            action: new TuiColorPair(Color.White, Color.DarkGreen),
            selection: new TuiColorPair(Color.Yellow, Color.Blue),
            status: new TuiColorPair(Color.Black, Color.Cyan),
            dialog: new TuiColorPair(Color.White, Color.DarkGreen),
            accent: new TuiColorPair(Color.Cyan, Color.Blue));

        public static TuiTheme Dark => new(
            "Dark",
            new TuiColorPair(Color.Gainsboro, Color.Black),
            focus: new TuiColorPair(Color.Black, Color.Gold),
            disabled: new TuiColorPair(Color.DimGray, Color.Black),
            error: new TuiColorPair(Color.White, Color.DarkRed),
            selected: new TuiColorPair(Color.Black, Color.DeepSkyBlue),
            surface: new TuiColorPair(Color.Gainsboro, Color.Black),
            border: new TuiColorPair(Color.DeepSkyBlue, Color.Black),
            input: new TuiColorPair(Color.Gold, Color.Black),
            action: new TuiColorPair(Color.White, Color.DarkSlateBlue),
            selection: new TuiColorPair(Color.Gold, Color.Black),
            status: new TuiColorPair(Color.Black, Color.DeepSkyBlue),
            dialog: new TuiColorPair(Color.White, Color.DarkSlateGray),
            accent: new TuiColorPair(Color.DeepSkyBlue, Color.Black));

        public static TuiTheme Light => new(
            "Light",
            new TuiColorPair(Color.Black, Color.White),
            focus: new TuiColorPair(Color.White, Color.RoyalBlue),
            disabled: new TuiColorPair(Color.Gray, Color.White),
            error: new TuiColorPair(Color.White, Color.Firebrick),
            selected: new TuiColorPair(Color.White, Color.SteelBlue),
            surface: new TuiColorPair(Color.Black, Color.White),
            border: new TuiColorPair(Color.SteelBlue, Color.White),
            input: new TuiColorPair(Color.Black, Color.WhiteSmoke),
            action: new TuiColorPair(Color.White, Color.SeaGreen),
            selection: new TuiColorPair(Color.Black, Color.White),
            status: new TuiColorPair(Color.White, Color.SteelBlue),
            dialog: new TuiColorPair(Color.Black, Color.WhiteSmoke),
            accent: new TuiColorPair(Color.SteelBlue, Color.White));

        public static TuiTheme HighContrast => new(
            "HighContrast",
            new TuiColorPair(Color.White, Color.Black),
            focus: new TuiColorPair(Color.Black, Color.Yellow),
            disabled: new TuiColorPair(Color.Gray, Color.Black),
            error: new TuiColorPair(Color.White, Color.Red),
            selected: new TuiColorPair(Color.Black, Color.Cyan),
            surface: new TuiColorPair(Color.White, Color.Black),
            border: new TuiColorPair(Color.Yellow, Color.Black),
            input: new TuiColorPair(Color.White, Color.Black),
            action: new TuiColorPair(Color.Black, Color.Yellow),
            selection: new TuiColorPair(Color.White, Color.Black),
            status: new TuiColorPair(Color.Black, Color.White),
            dialog: new TuiColorPair(Color.White, Color.Black),
            accent: new TuiColorPair(Color.Cyan, Color.Black));

        public TuiColorPair Resolve(TuiThemeRole role, TuiThemeState state = TuiThemeState.Normal)
        {
            ValidateRole(role);
            ValidateState(state);

            return state switch
            {
                TuiThemeState.Focus => Focus,
                TuiThemeState.Disabled => Disabled,
                TuiThemeState.Error => Error,
                TuiThemeState.Selected => Selected,
                _ => role switch
                {
                    TuiThemeRole.Surface => Surface,
                    TuiThemeRole.Border => Border,
                    TuiThemeRole.Input => Input,
                    TuiThemeRole.Action => Action,
                    TuiThemeRole.Selection => Selection,
                    TuiThemeRole.Status => Status,
                    TuiThemeRole.Dialog => Dialog,
                    TuiThemeRole.Accent => Accent,
                    _ => Normal
                }
            };
        }

        public TuiTheme Clone()
            => new(
                Name,
                Normal,
                Focus,
                Disabled,
                Error,
                Selected,
                Surface,
                Border,
                Input,
                Action,
                Selection,
                Status,
                Dialog,
                Accent);

        private static void ValidateRole(TuiThemeRole role)
        {
            if (!Enum.IsDefined(role))
                throw new ArgumentOutOfRangeException(nameof(role));
        }

        private static void ValidateState(TuiThemeState state)
        {
            if (!Enum.IsDefined(state))
                throw new ArgumentOutOfRangeException(nameof(state));
        }
    }
}
