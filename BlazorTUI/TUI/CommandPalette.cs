using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class CommandPalette : Control, IPopupControl
    {
        private readonly List<CommandPaletteItem> commands;
        private readonly List<CommandPaletteItem> filteredCommands = new();
        private readonly IVirtualCommandPaletteDataProvider? virtualCommands;
        private string searchText = "";
        private string title = "Command";
        private string placeholder = "Type a command";
        private short maxVisibleCommands;
        private int highlightedIndex;
        private int scrollIndex;
        private int filteredCommandCount;

        public IReadOnlyList<CommandPaletteItem> Commands => commands;

        public IReadOnlyList<CommandPaletteItem> FilteredCommands => filteredCommands;

        public bool IsVirtualized => virtualCommands is not null;

        public int CommandCount => virtualCommands?.Count ?? commands.Count;

        public int FilteredCommandCount => filteredCommandCount;

        public bool IsOpen { get; private set; }

        public bool IsPaletteOpen => IsOpen;

        public bool ResetSearchOnClose { get; set; } = true;

        public string SearchText
        {
            get => searchText;
            set => SetSearchText(value);
        }

        public string Title
        {
            get => title;
            set => title = ValidateText(value);
        }

        public string Placeholder
        {
            get => placeholder;
            set => placeholder = ValidateText(value);
        }

        public short MaxVisibleCommands
        {
            get => maxVisibleCommands;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, (short)1);
                maxVisibleCommands = value;
                EnsureHighlightedCommandVisible();
            }
        }

        public int HighlightedIndex => highlightedIndex;

        public CommandPaletteItem? HighlightedCommand
            => highlightedIndex >= 0 && highlightedIndex < filteredCommandCount
                ? GetFilteredCommand(highlightedIndex)
                : null;

        public event EventHandler<CommandPaletteExecutedEventArgs>? CommandExecuted;

        bool IPopupControl.IsPopupOpen => IsOpen;

        public CommandPalette(
            string name,
            IEnumerable<CommandPaletteItem> commands,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            short maxVisibleCommands = 5)
        {
            ArgumentNullException.ThrowIfNull(commands);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)12);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxVisibleCommands, (short)1);

            this.commands = commands.ToList();
            ValidateUniqueCommandNames(this.commands);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.maxVisibleCommands = maxVisibleCommands;

            RefreshFilteredCommands();
            LostFocus += (_, _) => Close();
        }

        public CommandPalette(
            string name,
            IVirtualCommandPaletteDataProvider commands,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            short maxVisibleCommands = 5)
        {
            ArgumentNullException.ThrowIfNull(commands);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)12);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxVisibleCommands, (short)1);

            this.commands = new List<CommandPaletteItem>();
            virtualCommands = commands;
            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.maxVisibleCommands = maxVisibleCommands;

            RefreshFilteredCommands();
            LostFocus += (_, _) => Close();
        }

        public CommandPaletteItem AddCommand(
            string name,
            string title,
            string description = "",
            Action<CommandPaletteItem>? action = null)
        {
            var command = new CommandPaletteItem(name, title, description, action);
            AddCommand(command);
            return command;
        }

        public void AddCommand(CommandPaletteItem command)
        {
            EnsureStaticCommands();
            ArgumentNullException.ThrowIfNull(command);
            if (commands.Any(existing => existing.Name == command.Name))
                throw new InvalidOperationException($"A command named '{command.Name}' already exists.");

            commands.Add(command);
            RefreshFilteredCommands();
        }

        public bool RemoveCommand(string name)
        {
            EnsureStaticCommands();
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = commands.FindIndex(command => command.Name == name);
            if (index < 0)
                return false;

            commands.RemoveAt(index);
            RefreshFilteredCommands();
            return true;
        }

        public bool RemoveCommand(CommandPaletteItem command)
        {
            ArgumentNullException.ThrowIfNull(command);
            return RemoveCommand(command.Name);
        }

        public void ClearCommands()
        {
            EnsureStaticCommands();
            if (commands.Count == 0)
                return;

            commands.Clear();
            RefreshFilteredCommands();
        }

        public CommandPaletteItem? GetCommand(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return virtualCommands?.GetCommand(name) ?? commands.FirstOrDefault(command => command.Name == name);
        }

        public void Open()
        {
            if (!Visible || IsOpen)
                return;

            IsOpen = true;
            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }

            RefreshFilteredCommands();
        }

        public void OpenPalette()
            => Open();

        public void Close()
        {
            IsOpen = false;
            if (ResetSearchOnClose)
                SetSearchText("");
        }

        public void ClosePalette()
            => Close();

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void TogglePalette()
            => Toggle();

        public bool ExecuteCommand(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            CommandPaletteItem? command = GetCommand(name);
            if (command is null)
                return false;

            ExecuteCommand(command);
            return true;
        }

        public void ClearSearch()
            => SetSearchText("");

        internal void ExportCommandPaletteState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            state.SetString("SearchText", searchText);
            state.SetInteger("HighlightedIndex", highlightedIndex);
            state.SetInteger("ScrollIndex", scrollIndex);
            state.SetBoolean("IsOpen", IsOpen);
        }

        internal void RestoreCommandPaletteState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetString("SearchText", out string restoredSearchText))
                searchText = ValidateText(restoredSearchText);

            RefreshFilteredCommands();

            highlightedIndex = state.TryGetInteger("HighlightedIndex", out int restoredHighlightedIndex)
                ? Math.Clamp(restoredHighlightedIndex, -1, Math.Max(-1, filteredCommandCount - 1))
                : highlightedIndex;
            scrollIndex = state.TryGetInteger("ScrollIndex", out int restoredScrollIndex)
                ? Math.Max(0, restoredScrollIndex)
                : 0;
            IsOpen = state.TryGetBoolean("IsOpen", out bool restoredOpen) && restoredOpen && Visible;
            EnsureHighlightedCommandVisible();
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (!Visible)
                return false;

            if (!IsOpen)
            {
                switch (key)
                {
                    case "Enter":
                    case "Space":
                    case " ":
                    case "F4":
                        Open();
                        NotifyClicked();
                        return true;
                    default:
                        return false;
                }
            }

            switch (key)
            {
                case "Escape":
                    Close();
                    return true;
                case "Enter":
                    ExecuteHighlightedCommand();
                    NotifyClicked();
                    return filteredCommandCount > 0;
                case "ArrowDown":
                    MoveHighlight(1);
                    return filteredCommandCount > 0;
                case "ArrowUp":
                    MoveHighlight(-1);
                    return filteredCommandCount > 0;
                case "Home":
                    MoveHighlightTo(0);
                    return filteredCommandCount > 0;
                case "End":
                    MoveHighlightTo(filteredCommandCount - 1);
                    return filteredCommandCount > 0;
                case "Backspace":
                    RemoveLastSearchCharacter();
                    return true;
                case "Delete":
                    ClearSearch();
                    return true;
                case "Space":
                case " ":
                    AppendSearchText(" ");
                    return true;
                default:
                    if (TuiText.TextElementCount(key) == 1)
                    {
                        AppendSearchText(shiftKey ? key.ToUpperInvariant() : key);
                        return true;
                    }

                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y < 0)
                return false;

            container.TopContainer().SetFocus(Name);
            if (Y == 0)
            {
                Toggle();
                NotifyClicked();
                return true;
            }

            if (!IsOpen)
                return false;

            int commandOffset = Y - 1;
            if (commandOffset < 0 || commandOffset >= VisibleCommandCount)
                return false;

            highlightedIndex = scrollIndex + commandOffset;
            ExecuteHighlightedCommand();
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            DrawInput(rows);
            if (IsOpen)
                DrawCommands(rows);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && X >= this.X && X < this.X + Width &&
                Y >= this.Y && Y < this.Y + 1 + VisibleCommandCount;

        void IPopupControl.ClosePopup()
            => Close();

        private void DrawInput(IList<Row> rows)
        {
            string text = IsOpen
                ? $">{SearchText}"
                : $"[{Title}]";

            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                PrepareCell(cell, Focus ? BackgroundColor : ForeColor, Focus ? ForeColor : BackgroundColor);
                string character = TuiText.CellAt(text, x);
                if (IsOpen && searchText.Length == 0 && x > 1 && x - 2 < TuiText.VisualWidth(placeholder))
                {
                    character = TuiText.CellAt(placeholder, x - 2);
                    cell.ForeColor = ForeColor;
                    cell.BackgroundColor = BackgroundColor;
                }

                cell.Character = character;
            }
        }

        private void DrawCommands(IList<Row> rows)
        {
            int visibleCount = VisibleCommandCount;
            if (visibleCount == 0)
            {
                DrawCommandText(rows, 1, 0, "No commands", false);
                return;
            }

            for (int row = 0; row < visibleCount; row++)
            {
                int commandIndex = scrollIndex + row;
                CommandPaletteItem command = GetFilteredCommand(commandIndex);
                string text = command.Description.Length == 0
                    ? command.Title
                    : $"{command.Title} - {command.Description}";
                DrawCommandText(rows, row + 1, commandIndex, text, commandIndex == highlightedIndex);
            }
        }

        private void DrawCommandText(IList<Row> rows, int localY, int commandIndex, string text, bool highlighted)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, localY, out Cell cell))
                    continue;

                PrepareCell(
                    cell,
                    highlighted ? BackgroundColor : ForeColor,
                    highlighted ? ForeColor : BackgroundColor);
                cell.Character = x == Width - 1
                    ? GetScrollCharacter(localY - 1, commandIndex)
                    : TuiText.CellAt(text, x);
            }
        }

        private string GetScrollCharacter(int visibleRow, int commandIndex)
        {
            int visibleCount = VisibleCommandCount;
            if (filteredCommandCount <= visibleCount)
                return " ";
            if (visibleCount <= 1)
                return "↕";
            if (visibleRow == 0)
                return scrollIndex > 0 ? "↑" : "│";
            if (visibleRow == visibleCount - 1)
                return commandIndex < filteredCommandCount - 1 ? "↓" : "│";

            return "│";
        }

        private void ExecuteHighlightedCommand()
        {
            if (HighlightedCommand is null)
                return;

            ExecuteCommand(HighlightedCommand);
            Close();
        }

        private void ExecuteCommand(CommandPaletteItem command)
        {
            command.Execute();
            CommandExecuted?.Invoke(this, new CommandPaletteExecutedEventArgs(command));
        }

        private void MoveHighlight(int direction)
        {
            if (filteredCommandCount == 0)
                return;

            MoveHighlightTo(Math.Clamp(highlightedIndex + direction, 0, filteredCommandCount - 1));
        }

        private void MoveHighlightTo(int index)
        {
            if (filteredCommandCount == 0)
                return;

            highlightedIndex = Math.Clamp(index, 0, filteredCommandCount - 1);
            EnsureHighlightedCommandVisible();
        }

        private void AppendSearchText(string text)
            => SetSearchText(searchText + text);

        private void RemoveLastSearchCharacter()
        {
            if (searchText.Length == 0)
                return;

            SetSearchText(TuiText.RemoveTextElements(searchText, TuiText.TextElementCount(searchText) - 1, 1));
        }

        private void SetSearchText(string? value)
        {
            searchText = ValidateText(value);
            RefreshFilteredCommands();
        }

        private void RefreshFilteredCommands()
        {
            filteredCommands.Clear();
            if (virtualCommands is null)
            {
                foreach (CommandPaletteItem command in commands.Where(CommandMatchesSearch))
                    filteredCommands.Add(command);
                filteredCommandCount = filteredCommands.Count;
            }
            else
            {
                filteredCommandCount = virtualCommands.GetFilteredCount(searchText);
            }

            highlightedIndex = filteredCommandCount == 0 ? -1 : Math.Clamp(highlightedIndex, 0, filteredCommandCount - 1);
            EnsureHighlightedCommandVisible();
        }

        private bool CommandMatchesSearch(CommandPaletteItem command)
        {
            if (searchText.Length == 0)
                return true;

            return command.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                command.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                command.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureHighlightedCommandVisible()
        {
            int visibleCount = VisibleCommandCount;
            if (highlightedIndex < 0 || visibleCount == 0)
            {
                scrollIndex = 0;
                return;
            }

            if (highlightedIndex < scrollIndex)
                scrollIndex = highlightedIndex;
            else if (highlightedIndex >= scrollIndex + visibleCount)
                scrollIndex = highlightedIndex - visibleCount + 1;

            scrollIndex = Math.Clamp(scrollIndex, 0, Math.Max(0, filteredCommandCount - visibleCount));
        }

        private int VisibleCommandCount
        {
            get
            {
                int availableRows = container is null ? maxVisibleCommands : Math.Max(0, container.Height - Y - 1);
                return Math.Min(filteredCommandCount, Math.Min(maxVisibleCommands, availableRows));
            }
        }

        private CommandPaletteItem GetFilteredCommand(int index)
            => virtualCommands is null
                ? filteredCommands[index]
                : virtualCommands.GetFilteredCommand(searchText, index);

        private void EnsureStaticCommands()
        {
            if (virtualCommands is not null)
                throw new InvalidOperationException("Commands cannot be mutated when CommandPalette uses a virtual data provider.");
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }

        private static void ValidateUniqueCommandNames(IEnumerable<CommandPaletteItem> commands)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (CommandPaletteItem command in commands)
            {
                ArgumentNullException.ThrowIfNull(command);
                if (!names.Add(command.Name))
                    throw new InvalidOperationException($"A command named '{command.Name}' already exists.");
            }
        }

        private static string ValidateText(string? value)
        {
            string text = value ?? "";
            if (text.Contains('\r') || text.Contains('\n'))
                throw new ArgumentException("Command palette text cannot contain line breaks.", nameof(value));

            return text;
        }
    }
}
