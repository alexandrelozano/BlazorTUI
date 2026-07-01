using System.Linq;

namespace BlazorTUI.TUI
{
    public class Screen
    {
        private Row[] _renderedRows = Array.Empty<Row>();
        private long[] _renderedRowRevisions = Array.Empty<long>();

        internal short width { get; set; }

        public short Width => width;

        internal short height { get; set; }

        public short Height => height;

        internal IList<Row> rows;

        public IReadOnlyList<Row> Rows => rows as IReadOnlyList<Row> ?? rows.ToArray();

        internal Container topContainer { get; set; }

        public Container TopContainer => topContainer;

        internal IList<Dialog> dialogs;

        public IReadOnlyList<Dialog> Dialogs => dialogs as IReadOnlyList<Dialog> ?? dialogs.ToArray();

        internal MenuBar? menuBar;

        public MenuBar? MenuBar { get => menuBar; set => menuBar = value; }

        public TuiTheme Theme { get; private set; }

        public TuiShortcutMap Shortcuts { get; } = TuiShortcutMap.CreateDefault();

        public TuiDialogService DialogService { get; }

        public long Revision { get; private set; }

        public Screen(short width, short height)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            this.width = width;
            this.height = height;

            dialogs = new List<Dialog>();   

            rows = new List<Row>();

            for (short y = 0; y < height; y++)
            {
                Row row = new Row();
                var cells = new List<Cell>(width);
                for (short x = 0; x < width; x++)
                {
                    Cell cell = new Cell();
                    cell.x = x;
                    cell.y = y;
                    cell.character = " ";
                    cell.foreColor = System.Drawing.Color.Yellow;
                    cell.backgroundColor = System.Drawing.Color.Blue;
                    cell.textDecoration = Cell.TextDecoration.None;
                    cell.visible = true;
                    cell.scaleX = 1;
                    cell.scaleY = 1;    
                    cells.Add(cell);
                }
                row.Cells = cells;
                rows.Add(row);
            }

            Theme = TuiTheme.Classic;
            DialogService = new TuiDialogService(this);

            topContainer = new Frame("TopContainer", "", 0, 0, width, height, Frame.BorderStyle.None, Theme.Border.ForeColor, Theme.Surface.BackgroundColor);

            this.topContainer = topContainer;

            CaptureRenderedRows(incrementRevision: false);
        }

        public void ApplyTheme(TuiTheme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            Theme = theme.Clone();
            TuiThemeApplicator.ApplyToScreen(this, Theme);
        }

        public void SetFocus(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (dialogs.Count == 0)
                topContainer.SetFocus(name);
            else
                dialogs.ElementAt(dialogs.Count - 1).SetFocus(name);
        }

        public bool Validate(bool focusFirstInvalid = true)
            => dialogs.Count == 0
                ? topContainer.Validate(focusFirstInvalid)
                : dialogs.ElementAt(dialogs.Count - 1).Validate(focusFirstInvalid);

        public bool Validate(string validationGroup, bool focusFirstInvalid = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(validationGroup);

            return dialogs.Count == 0
                ? topContainer.Validate(validationGroup, focusFirstInvalid)
                : dialogs.ElementAt(dialogs.Count - 1).Validate(validationGroup, focusFirstInvalid);
        }

        public IReadOnlyList<Control> GetInvalidControls()
            => dialogs.Count == 0
                ? topContainer.GetInvalidControls()
                : dialogs.ElementAt(dialogs.Count - 1).GetInvalidControls();

        public IReadOnlyList<Control> GetInvalidControls(string validationGroup)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(validationGroup);

            return dialogs.Count == 0
                ? topContainer.GetInvalidControls(validationGroup)
                : dialogs.ElementAt(dialogs.Count - 1).GetInvalidControls(validationGroup);
        }

        public bool FocusFirstControl()
            => dialogs.Count == 0
                ? topContainer.FocusFirstControl()
                : dialogs.ElementAt(dialogs.Count - 1).FocusFirstControl();

        public bool FocusLastControl()
            => dialogs.Count == 0
                ? topContainer.FocusLastControl()
                : dialogs.ElementAt(dialogs.Count - 1).FocusLastControl();

        public TuiScreenState ExportState()
            => TuiStatePersistence.Export(this);

        public string ExportStateJson(bool indented = false)
            => ExportState().ToJson(indented);

        public void RestoreState(TuiScreenState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            TuiStatePersistence.Restore(this, state);
        }

        public void RestoreState(TuiScreenState state, TuiStateRestoreOptions options)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(options);
            TuiStatePersistence.Restore(this, state, options);
        }

        public void RestoreStateJson(string json)
            => RestoreState(TuiScreenState.FromJson(json));

        public void RestoreStateJson(string json, TuiStateRestoreOptions options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            ArgumentNullException.ThrowIfNull(options);
            RestoreState(TuiScreenState.FromJson(json), options);
        }

        public void KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            MenuBar? activeMenuBar = menuBar;

            if (key == "Alt" && activeMenuBar != null)
                activeMenuBar.showShortCutkeys = !activeMenuBar.showShortCutkeys;
            if (activeMenuBar != null && (activeMenuBar.showShortCutkeys || activeMenuBar.OpenedMenu() != null))
                activeMenuBar.KeyDown(key, shiftKey);
            else if (dialogs.Count == 0)
                topContainer.KeyDown(key, shiftKey);
            else
                dialogs.ElementAt(dialogs.Count - 1).KeyDown(key, shiftKey);    
        }

        public bool ExecuteShortcut(TuiShortcutAction action)
        {
            if (!Enum.IsDefined(action))
                throw new ArgumentOutOfRangeException(nameof(action));

            switch (action)
            {
                case TuiShortcutAction.FocusNext:
                    KeyDown("Tab", false);
                    return true;
                case TuiShortcutAction.FocusPrevious:
                    KeyDown("Tab", true);
                    return true;
                case TuiShortcutAction.ToggleMenuShortcuts:
                    KeyDown("Alt", false);
                    return true;
                case TuiShortcutAction.MenuMovePrevious:
                    KeyDown("ArrowLeft", false);
                    return true;
                case TuiShortcutAction.MenuMoveNext:
                    KeyDown("ArrowRight", false);
                    return true;
                case TuiShortcutAction.MenuMoveUp:
                    KeyDown("ArrowUp", false);
                    return true;
                case TuiShortcutAction.MenuMoveDown:
                    KeyDown("ArrowDown", false);
                    return true;
                case TuiShortcutAction.MenuActivate:
                    KeyDown("Enter", false);
                    return true;
                case TuiShortcutAction.ControlOpen:
                    KeyDown("F4", false);
                    return true;
                case TuiShortcutAction.ControlCancel:
                    KeyDown("Escape", false);
                    return true;
                case TuiShortcutAction.ControlActivate:
                    KeyDown("Enter", false);
                    return true;
                case TuiShortcutAction.ToggleCommandPalette:
                case TuiShortcutAction.SelectNextTab:
                case TuiShortcutAction.SelectPreviousTab:
                case TuiShortcutAction.SelectAll:
                case TuiShortcutAction.Copy:
                case TuiShortcutAction.Cut:
                case TuiShortcutAction.Paste:
                case TuiShortcutAction.Undo:
                case TuiShortcutAction.Redo:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public void Render()
        {
            topContainer.Render(rows);

            foreach(Dialog dialog in dialogs)
            {
                dialog.Render(rows);
            }

            menuBar?.Render(rows);

            CaptureRenderedRows(incrementRevision: true);
        }

        private void CaptureRenderedRows(bool incrementRevision)
        {
            bool changed = _renderedRows.Length != rows.Count;

            if (changed)
            {
                _renderedRows = new Row[rows.Count];
                _renderedRowRevisions = new long[rows.Count];
            }

            for (int index = 0; index < rows.Count; index++)
            {
                Row row = rows[index];
                row.CaptureChanges();
                long rowRevision = row.Revision;

                if (!ReferenceEquals(_renderedRows[index], row) || _renderedRowRevisions[index] != rowRevision)
                    changed = true;

                _renderedRows[index] = row;
                _renderedRowRevisions[index] = rowRevision;
            }

            if (changed && incrementRevision)
                Revision++;
        }
    }
}
