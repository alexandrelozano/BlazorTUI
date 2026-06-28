using System.Text.Json;

namespace BlazorTUI.TUI
{
    public sealed class TuiScreenState
    {
        public string? FocusedControlName { get; set; }

        public Dictionary<string, TuiElementState> Controls { get; set; } = new(StringComparer.Ordinal);

        public Dictionary<string, TuiElementState> Containers { get; set; } = new(StringComparer.Ordinal);

        public string ToJson(bool indented = false)
            => JsonSerializer.Serialize(this, CreateJsonOptions(indented));

        public static TuiScreenState FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            TuiScreenState? state = JsonSerializer.Deserialize<TuiScreenState>(json, CreateJsonOptions(indented: false));
            return state ?? throw new ArgumentException("The JSON payload does not contain a valid TUI screen state.", nameof(json));
        }

        private static JsonSerializerOptions CreateJsonOptions(bool indented)
            => new()
            {
                WriteIndented = indented
            };
    }
}
