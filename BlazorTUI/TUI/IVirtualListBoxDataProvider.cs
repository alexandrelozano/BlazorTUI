namespace BlazorTUI.TUI
{
    public interface IVirtualListBoxDataProvider
    {
        int Count { get; }

        string GetItem(int index);

        string GetKey(int index);
    }
}
