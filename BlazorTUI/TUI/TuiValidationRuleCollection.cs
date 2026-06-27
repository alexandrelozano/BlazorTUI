using System.Collections.ObjectModel;

namespace BlazorTUI.TUI
{
    public sealed class TuiValidationRuleCollection : Collection<TuiValidationRule>
    {
        public TuiValidationRule Add(Func<object?, bool> validate, string message)
        {
            var rule = new TuiValidationRule(validate, message);
            Add(rule);
            return rule;
        }

        public TuiValidationRule Add(string message, Func<object?, bool> validate)
        {
            var rule = new TuiValidationRule(validate, message);
            Add(rule);
            return rule;
        }

        protected override void InsertItem(int index, TuiValidationRule item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, TuiValidationRule item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
        }
    }
}
