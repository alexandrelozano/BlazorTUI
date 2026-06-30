namespace BlazorTUI.TUI
{
    public sealed class TuiStateMigration
    {
        private readonly Func<TuiScreenState, TuiScreenState> migrate;

        public TuiStateMigration(
            int fromVersion,
            int toVersion,
            Func<TuiScreenState, TuiScreenState> migrate)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(fromVersion);
            ArgumentOutOfRangeException.ThrowIfNegative(toVersion);
            ArgumentNullException.ThrowIfNull(migrate);

            if (toVersion <= fromVersion)
                throw new ArgumentException("The target schema version must be greater than the source schema version.", nameof(toVersion));

            FromVersion = fromVersion;
            ToVersion = toVersion;
            this.migrate = migrate;
        }

        public int FromVersion { get; }

        public int ToVersion { get; }

        public bool CanMigrate(TuiScreenState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.SchemaVersion == FromVersion;
        }

        public TuiScreenState Migrate(TuiScreenState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (!CanMigrate(state))
                return state;

            TuiScreenState migratedState = migrate(state) ??
                throw new InvalidOperationException("The TUI state migration returned null.");
            migratedState.SchemaVersion = ToVersion;
            return migratedState;
        }
    }
}
