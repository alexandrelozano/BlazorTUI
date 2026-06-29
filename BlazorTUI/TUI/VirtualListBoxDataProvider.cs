using System.Globalization;

namespace BlazorTUI.TUI
{
    public sealed class VirtualListBoxDataProvider : IVirtualListBoxDataProvider
    {
        private readonly Func<int, string> getItem;
        private readonly Func<int, string>? getKey;

        public VirtualListBoxDataProvider(int count, Func<int, string> getItem, Func<int, string>? getKey = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentNullException.ThrowIfNull(getItem);

            Count = count;
            this.getItem = getItem;
            this.getKey = getKey;
        }

        public int Count { get; }

        public string GetItem(int index)
        {
            ValidateIndex(index);
            return getItem(index) ?? "";
        }

        public string GetKey(int index)
        {
            ValidateIndex(index);
            return getKey?.Invoke(index) ?? index.ToString(CultureInfo.InvariantCulture);
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
