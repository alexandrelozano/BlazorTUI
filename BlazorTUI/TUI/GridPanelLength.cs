using System.Globalization;

namespace BlazorTUI.TUI
{
    public readonly struct GridPanelLength : IEquatable<GridPanelLength>
    {
        private GridPanelLength(GridPanelUnitType unitType, int value)
        {
            if (!Enum.IsDefined(unitType))
                throw new ArgumentOutOfRangeException(nameof(unitType));
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

            UnitType = unitType;
            Value = value;
        }

        public GridPanelUnitType UnitType { get; }

        public int Value { get; }

        public static GridPanelLength Auto { get; } = new(GridPanelUnitType.Auto, 1);

        public static GridPanelLength Fixed(int cells)
            => new(GridPanelUnitType.Fixed, cells);

        public static GridPanelLength Star(int weight = 1)
            => new(GridPanelUnitType.Star, weight);

        public bool Equals(GridPanelLength other)
            => UnitType == other.UnitType && Value == other.Value;

        public override bool Equals(object? obj)
            => obj is GridPanelLength other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(UnitType, Value);

        public override string ToString()
            => UnitType switch
            {
                GridPanelUnitType.Fixed => Value.ToString(CultureInfo.InvariantCulture),
                GridPanelUnitType.Auto => "Auto",
                GridPanelUnitType.Star => $"{Value.ToString(CultureInfo.InvariantCulture)}*",
                _ => Value.ToString(CultureInfo.InvariantCulture)
            };
    }
}
