using System.Text.Json;

namespace BlazorTUI.TUI
{
    public sealed class TuiScreenState
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public string? FocusedControlName { get; set; }

        public Dictionary<string, TuiElementState> Controls { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, TuiElementState> Containers { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> Payloads { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> ProtectedPayloads { get; set; } = new(StringComparer.Ordinal);

        public void SetPayload(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Payloads ??= new Dictionary<string, string>(StringComparer.Ordinal);

            if (value is null)
                Payloads.Remove(name);
            else
                Payloads[name] = value;
        }

        public bool TryGetPayload(string name, out string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (Payloads is not null && Payloads.TryGetValue(name, out string? storedValue))
            {
                value = storedValue;
                return true;
            }

            value = "";
            return false;
        }

        public void SetProtectedPayload(
            string name,
            string? value,
            Func<string, string> protect)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(protect);
            ProtectedPayloads ??= new Dictionary<string, string>(StringComparer.Ordinal);

            if (value is null)
            {
                ProtectedPayloads.Remove(name);
                return;
            }

            ProtectedPayloads[name] = protect(value) ??
                throw new InvalidOperationException("The protected payload callback returned null.");
        }

        public bool TryGetProtectedPayload(
            string name,
            Func<string, string> unprotect,
            out string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(unprotect);

            if (ProtectedPayloads is not null && ProtectedPayloads.TryGetValue(name, out string? storedValue))
            {
                value = unprotect(storedValue) ??
                    throw new InvalidOperationException("The protected payload callback returned null.");
                return true;
            }

            value = "";
            return false;
        }

        public string ToJson(bool indented = false)
            => JsonSerializer.Serialize(this, CreateJsonOptions(indented));

        public static TuiScreenState FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            TuiScreenState? state = JsonSerializer.Deserialize<TuiScreenState>(json, CreateJsonOptions(indented: false));
            if (state is null)
                throw new ArgumentException("The JSON payload does not contain a valid TUI screen state.", nameof(json));

            state.Controls = new Dictionary<string, TuiElementState>(
                state.Controls ?? new Dictionary<string, TuiElementState>(),
                StringComparer.Ordinal);
            state.Containers = new Dictionary<string, TuiElementState>(
                state.Containers ?? new Dictionary<string, TuiElementState>(),
                StringComparer.Ordinal);
            state.Payloads = new Dictionary<string, string>(
                state.Payloads ?? new Dictionary<string, string>(),
                StringComparer.Ordinal);
            state.ProtectedPayloads = new Dictionary<string, string>(
                state.ProtectedPayloads ?? new Dictionary<string, string>(),
                StringComparer.Ordinal);
            return state;
        }

        private static JsonSerializerOptions CreateJsonOptions(bool indented)
            => new()
            {
                WriteIndented = indented
            };
    }
}
