namespace BlazorTUI.TUI
{
    public sealed class TuiKeyGesture : IEquatable<TuiKeyGesture>
    {
        public TuiKeyGesture(
            string key,
            bool control = false,
            bool shift = false,
            bool alt = false,
            bool meta = false)
        {
            ThrowIfInvalidKey(key);

            Key = NormalizeKey(key);
            Control = control;
            Shift = shift;
            Alt = alt;
            Meta = meta;
        }

        public string Key { get; }

        public bool Control { get; }

        public bool Shift { get; }

        public bool Alt { get; }

        public bool Meta { get; }

        public static TuiKeyGesture Parse(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            string[] parts = value
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                throw new ArgumentException("A key gesture must contain a key.", nameof(value));

            if (parts.Length == 1 && TryGetModifierKey(parts[0], out string? modifierKey))
                return new TuiKeyGesture(modifierKey!);

            bool control = false;
            bool shift = false;
            bool alt = false;
            bool meta = false;
            string? key = null;

            foreach (string part in parts)
            {
                switch (part.ToUpperInvariant())
                {
                    case "CTRL":
                    case "CONTROL":
                        control = true;
                        break;
                    case "SHIFT":
                        shift = true;
                        break;
                    case "ALT":
                        alt = true;
                        break;
                    case "CMD":
                    case "COMMAND":
                    case "META":
                    case "WIN":
                    case "WINDOWS":
                        meta = true;
                        break;
                    default:
                        if (key is not null)
                            throw new ArgumentException("A key gesture can contain only one non-modifier key.", nameof(value));

                        key = part;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A key gesture must contain a non-modifier key.", nameof(value));

            return new TuiKeyGesture(key, control, shift, alt, meta);
        }

        public static TuiKeyGesture FromKeyboardEvent(
            string key,
            bool control,
            bool shift,
            bool alt,
            bool meta)
        {
            string normalizedKey = NormalizeKey(key);
            return normalizedKey switch
            {
                "Control" => new TuiKeyGesture(normalizedKey, false, shift, alt, meta),
                "Shift" => new TuiKeyGesture(normalizedKey, control, false, alt, meta),
                "Alt" => new TuiKeyGesture(normalizedKey, control, shift, false, meta),
                "Meta" => new TuiKeyGesture(normalizedKey, control, shift, alt, false),
                _ => new TuiKeyGesture(normalizedKey, control, shift, alt, meta)
            };
        }

        public bool Equals(TuiKeyGesture? other)
            => other is not null &&
                string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) &&
                Control == other.Control &&
                Shift == other.Shift &&
                Alt == other.Alt &&
                Meta == other.Meta;

        public override bool Equals(object? obj)
            => obj is TuiKeyGesture other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Key), Control, Shift, Alt, Meta);

        public override string ToString()
        {
            var parts = new List<string>();
            if (Control)
                parts.Add("Control");
            if (Shift)
                parts.Add("Shift");
            if (Alt)
                parts.Add("Alt");
            if (Meta)
                parts.Add("Meta");

            parts.Add(DisplayKey(Key));
            return string.Join("+", parts);
        }

        internal static string NormalizeKey(string key)
        {
            ThrowIfInvalidKey(key);

            return key switch
            {
                "Ctrl" => "Control",
                "Cmd" or "Command" or "Win" or "Windows" => "Meta",
                "Space" or "Spacebar" => " ",
                "Esc" => "Escape",
                _ when key.Length == 1 => key.ToUpperInvariant(),
                _ => key
            };
        }

        private static void ThrowIfInvalidKey(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.Length == 0 || (key != " " && string.IsNullOrWhiteSpace(key)))
                throw new ArgumentException("A key gesture must contain a key.", nameof(key));
        }

        private static bool TryGetModifierKey(string key, out string? modifierKey)
        {
            modifierKey = key.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => "Control",
                "SHIFT" => "Shift",
                "ALT" => "Alt",
                "CMD" or "COMMAND" or "META" or "WIN" or "WINDOWS" => "Meta",
                _ => null
            };

            return modifierKey is not null;
        }

        private static string DisplayKey(string key)
            => key switch
            {
                " " => "Space",
                _ when key.Length == 1 => key.ToUpperInvariant(),
                _ => key
            };
    }
}
