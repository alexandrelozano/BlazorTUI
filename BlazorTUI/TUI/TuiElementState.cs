namespace BlazorTUI.TUI
{
    public sealed class TuiElementState
    {
        public TuiElementState()
        {
        }

        public TuiElementState(string elementType)
        {
            ElementType = elementType ?? "";
        }

        public string ElementType { get; set; } = "";

        public Dictionary<string, string> Strings { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Integers { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, bool> Booleans { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, string[]> StringLists { get; set; } = new(StringComparer.Ordinal);

        public void SetString(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Strings ??= new Dictionary<string, string>(StringComparer.Ordinal);

            if (value is null)
                Strings.Remove(name);
            else
                Strings[name] = value;
        }

        public void SetInteger(string name, int value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Integers ??= new Dictionary<string, int>(StringComparer.Ordinal);
            Integers[name] = value;
        }

        public void SetBoolean(string name, bool value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Booleans ??= new Dictionary<string, bool>(StringComparer.Ordinal);
            Booleans[name] = value;
        }

        public void SetStringList(string name, IEnumerable<string> values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(values);

            StringLists ??= new Dictionary<string, string[]>(StringComparer.Ordinal);
            StringLists[name] = values.Select(value => value ?? "").ToArray();
        }

        public bool TryGetString(string name, out string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (Strings is not null && Strings.TryGetValue(name, out string? storedValue))
            {
                value = storedValue;
                return true;
            }

            value = "";
            return false;
        }

        public bool TryGetInteger(string name, out int value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (Integers is not null && Integers.TryGetValue(name, out int storedValue))
            {
                value = storedValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetBoolean(string name, out bool value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (Booleans is not null && Booleans.TryGetValue(name, out bool storedValue))
            {
                value = storedValue;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetStringList(string name, out IReadOnlyList<string> values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (StringLists is not null && StringLists.TryGetValue(name, out string[]? storedValues))
            {
                values = storedValues;
                return true;
            }

            values = Array.Empty<string>();
            return false;
        }
    }
}
