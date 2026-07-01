namespace BlazorTUI.TUI
{
    public sealed class TuiCommandRegistry
    {
        private readonly Dictionary<string, TuiCommand> commands = new(StringComparer.Ordinal);

        public IReadOnlyList<TuiCommand> Commands => commands.Values.ToList();

        public IReadOnlyList<TuiCommand> VisibleCommands
            => commands.Values.Where(command => command.Visible).ToList();

        public TuiCommand AddCommand(
            string id,
            string label,
            string description = "",
            Action<TuiCommand>? handler = null)
        {
            var command = new TuiCommand(id, label, description, handler);
            AddCommand(command);
            return command;
        }

        public void AddCommand(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            if (commands.ContainsKey(command.Id))
                throw new InvalidOperationException($"A command named '{command.Id}' already exists.");

            EnsureShortcutsAreAvailable(command);
            commands.Add(command.Id, command);
        }

        public TuiCommand GetCommand(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return commands.TryGetValue(id, out TuiCommand? command)
                ? command
                : throw new ArgumentException($"A command named '{id}' does not exist.", nameof(id));
        }

        public bool TryGetCommand(string id, out TuiCommand? command)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return commands.TryGetValue(id, out command);
        }

        public bool RemoveCommand(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return commands.Remove(id);
        }

        public void Clear()
            => commands.Clear();

        public bool Execute(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return commands.TryGetValue(id, out TuiCommand? command) && command.Execute();
        }

        private void EnsureShortcutsAreAvailable(TuiCommand command)
        {
            foreach (TuiKeyGesture shortcut in command.Shortcuts)
            {
                foreach (TuiCommand existingCommand in commands.Values)
                {
                    if (existingCommand.Shortcuts.Contains(shortcut))
                    {
                        throw new InvalidOperationException(
                            $"The key gesture '{shortcut}' is already bound to command '{existingCommand.Id}'.");
                    }
                }
            }
        }
    }
}
