namespace BlazorTUI.TUI
{
    public sealed class TuiStateRestoreOptions
    {
        public static TuiStateRestoreOptions Default { get; } = new();

        public bool RestoreFocus { get; init; } = true;

        public bool RestoreControls { get; init; } = true;

        public bool RestoreContainers { get; init; } = true;

        public bool SuppressEvents { get; init; }

        public IReadOnlyCollection<string>? ControlNames { get; init; }

        public IReadOnlyCollection<string>? ContainerNames { get; init; }

        public IReadOnlyList<TuiStateMigration> Migrations { get; init; } = Array.Empty<TuiStateMigration>();

        internal bool ShouldRestoreControl(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return RestoreControls && (ControlNames is null || ControlNames.Contains(name));
        }

        internal bool ShouldRestoreContainer(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return RestoreContainers && (ContainerNames is null || ContainerNames.Contains(name));
        }

        internal TuiScreenState ApplyMigrations(TuiScreenState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (Migrations is null || Migrations.Count == 0)
                return state;

            TuiScreenState currentState = state;
            bool migrated;
            do
            {
                migrated = false;
                foreach (TuiStateMigration migration in Migrations)
                {
                    ArgumentNullException.ThrowIfNull(migration);
                    if (!migration.CanMigrate(currentState))
                        continue;

                    currentState = migration.Migrate(currentState);
                    migrated = true;
                    break;
                }
            }
            while (migrated);

            return currentState;
        }
    }
}
