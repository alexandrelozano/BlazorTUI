namespace BlazorTUI.TUI
{
    public sealed class TuiWizard
    {
        private readonly List<TuiWizardStep> steps = new();

        public IReadOnlyList<TuiWizardStep> Steps => steps;

        public int CurrentIndex { get; private set; } = -1;

        public TuiWizardStep? CurrentStep
            => CurrentIndex >= 0 && CurrentIndex < steps.Count ? steps[CurrentIndex] : null;

        public bool IsFirstStep => CurrentIndex <= 0;

        public bool IsLastStep => steps.Count == 0 || CurrentIndex >= steps.Count - 1;

        public event EventHandler<TuiWizardStepChangedEventArgs>? CurrentStepChanged;

        public TuiWizardStep AddStep(string name, Container container, string validationGroup = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(container);
            if (steps.Any(step => string.Equals(step.Name, name.Trim(), StringComparison.Ordinal)))
                throw new InvalidOperationException($"A wizard step named '{name}' already exists.");

            var step = new TuiWizardStep(name, container, validationGroup);
            step.Container.IsFocusScope = true;
            step.Container.Visible = steps.Count == 0;
            steps.Add(step);

            if (CurrentIndex < 0)
                CurrentIndex = 0;

            return step;
        }

        public bool MoveNext(bool validateCurrentStep = true)
        {
            if (CurrentStep is null || CurrentIndex >= steps.Count - 1)
                return false;

            if (validateCurrentStep && !ValidateCurrentStep())
                return false;

            ActivateStep(CurrentIndex + 1);
            return true;
        }

        public bool MovePrevious()
        {
            if (CurrentStep is null || CurrentIndex <= 0)
                return false;

            ActivateStep(CurrentIndex - 1);
            return true;
        }

        public bool MoveTo(string name, bool validateCurrentStep = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            int index = steps.FindIndex(step => string.Equals(step.Name, name.Trim(), StringComparison.Ordinal));
            return index >= 0 && MoveTo(index, validateCurrentStep);
        }

        public bool MoveTo(int index, bool validateCurrentStep = true)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, steps.Count);

            if (index == CurrentIndex)
                return true;

            if (validateCurrentStep && !ValidateCurrentStep())
                return false;

            ActivateStep(index);
            return true;
        }

        public bool ValidateCurrentStep(bool focusFirstInvalid = true)
        {
            TuiWizardStep? step = CurrentStep;
            if (step is null)
                return true;

            return string.IsNullOrEmpty(step.ValidationGroup)
                ? step.Container.Validate(focusFirstInvalid)
                : step.Container.Validate(step.ValidationGroup, focusFirstInvalid);
        }

        public bool FocusFirstControl()
            => CurrentStep?.Container.FocusFirstControl() == true;

        public bool FocusLastControl()
            => CurrentStep?.Container.FocusLastControl() == true;

        private void ActivateStep(int index)
        {
            TuiWizardStep? previousStep = CurrentStep;
            if (previousStep is not null)
                previousStep.Container.Visible = false;

            CurrentIndex = index;
            TuiWizardStep currentStep = steps[CurrentIndex];
            currentStep.Container.Visible = true;

            if (currentStep.AutoFocusFirstControl)
                currentStep.Container.FocusFirstControl();

            CurrentStepChanged?.Invoke(this, new TuiWizardStepChangedEventArgs(previousStep, currentStep, CurrentIndex));
        }
    }
}
