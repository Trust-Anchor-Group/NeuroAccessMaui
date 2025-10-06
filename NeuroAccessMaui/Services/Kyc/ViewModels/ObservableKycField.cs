using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;

namespace NeuroAccessMaui.Services.Kyc.ViewModels
{
    /// <summary>
    /// Enumeration of all supported field types in the KYC process.
    /// </summary>
    public enum FieldType
    {
        Text,
        Date,
        Boolean,
        Picker,
        File,
        Image,
        Phone,
        Email,
        Integer,
        Decimal,
        Country,
        Checkbox,
        Radio,
        Gender,
        Label,   // non-input
        Info     // non-input
    }

    /// <summary>
    /// The base KYC field model. Use as-is for generic fields, or subclass for custom logic.
    /// </summary>
    public abstract class ObservableKycField : ObservableObject
    {
        private WeakReference<KycProcess>? ownerProcess;
        private readonly List<IKycRule> rules = new();
        public ReadOnlyCollection<IKycRule> ValidationRules => this.rules.AsReadOnly();

        public ObservableTask<int> ValidationTask { get; } = new();

        private bool hasAsyncRules;

        // Debounce support for synchronous validation implemented via ObservableTask (avoids manual CTS & CA1001 warning)
        private readonly ObservableTask<int> syncValidationTask;
        private static readonly TimeSpan SyncValidationDebounce = TimeSpan.FromMilliseconds(500);

        public ObservableKycField()
        {
            this.Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            this.rules.Add(new RequiredRule());
            this.SelectedOptions.CollectionChanged += this.SelectedOptions_CollectionChanged;

            this.ValidationTask = new ObservableTaskBuilder<int>()
                                    .Named("KYC Field Validation (Async Rules)")
                                    .WithPolicy(Policies.Debounce(TimeSpan.FromMilliseconds(500)))
                                    .Run(this.ValidateAsync)
                                    .AutoStart(false)
                                    .UseTaskRun(true)
                                    .Build();

            this.syncValidationTask = new ObservableTaskBuilder<int>()
                                    .Named("KYC Field Sync Validation")
                                    .WithPolicy(Policies.Debounce(SyncValidationDebounce))
                                    .Run(async ctx =>
                                    {
                                        // Run sync validation on UI thread
                                        await MainThread.InvokeOnMainThreadAsync(() =>
                                        {
                                            this.RunSynchronousValidation();
                                            if (this.hasAsyncRules)
                                                this.ValidationTask.Run();
                                        });
                                    })
                                    .AutoStart(false)
                                    .UseTaskRun(false) // lightweight
                                    .Build();
        }

        /// <summary>
        /// The unique identifier for the field.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The field type (input, picker, info, etc).
        /// </summary>
        public FieldType FieldType { get; set; } = FieldType.Text;

        /// <summary>
        /// Whether the field is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Field label.
        /// </summary>
        public KycLocalizedText? Label { get; set; }

        /// <summary>
        /// Placeholder text for the field input.
        /// </summary>
        public KycLocalizedText? Placeholder { get; set; }

        /// <summary>
        /// Help/hint text for the field.
        /// </summary>
        public KycLocalizedText? Hint { get; set; }

        /// <summary>
        /// Field description.
        /// </summary>
        public KycLocalizedText? Description { get; set; }

        /// <summary>
        /// Optional special type for custom UI/logic.
        /// </summary>
        public string? SpecialType { get; set; }

        /// <summary>
        /// Conditional display of this field.
        /// </summary>
        public KycCondition? Condition { get; set; }

        /// <summary>
        /// All possible options (for picker, country, checkbox, radio).
        /// </summary>
        public ObservableCollection<KycOption> Options { get; } = new();

        /// <summary>
        /// Mappings to apply output.
        /// </summary>
        public ObservableCollection<KycMapping> Mappings { get; } = new();

        /// <summary>
        /// For Checkbox type: multi-selection of options.
        /// </summary>
        public ObservableCollection<KycOption> SelectedOptions { get; set; } = new();

        private void SelectedOptions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.FieldType == FieldType.Checkbox)
            {
                this.RawValue = this.SelectedOptions.ToList();
            }
        }

        /// <summary>
        /// Arbitrary metadata for field-specific extensions.
        /// Use e.g. Metadata["TargetWidth"] or Metadata["MaxFileSizeMB"].
        /// </summary>
        public Dictionary<string, object?> Metadata { get; }

        private object? rawValue;
        public object? RawValue
        {
            get => this.rawValue;
            set
            {
                if (this.SetProperty(ref this.rawValue, value))
                {
                    // QUICK required check so navigation buttons disable immediately when user clears content
                    if (this.Required && string.IsNullOrEmpty(this.StringValue))
                    {
                        this.IsValid = false;
                        string Label = this.Label?.Get(null) ?? this.Id;
                        this.ValidationText = string.IsNullOrEmpty(Label) ? "Required" : $"{Label} is required";
                    }

                    // Debounced synchronous validation + chaining async validation (if any)
                    this.syncValidationTask.Run();
                }
            }
        }

        private bool isVisible = true;
        public bool IsVisible
        {
            get => this.isVisible;
            set => this.SetProperty(ref this.isVisible, value);
        }

        private bool isValid = true;
        public bool IsValid
        {
            get => this.isValid;
            set => this.SetProperty(ref this.isValid, value);
        }

        private bool isValidating;
        public bool IsValidating
        {
            get => this.isValidating;
            set => this.SetProperty(ref this.isValidating, value);
        }

        private string? validationText;
        public string? ValidationText
        {
            get => this.validationText;
            set => this.SetProperty(ref this.validationText, value);
        }

        // Value helpers for different field types:
        /// <summary>
        /// Gets or sets the value as a string, parsing or formatting as needed for the field type.
        /// </summary>
        public abstract string? StringValue { get; set; }

        /// <summary>
        /// Add a validation rule to this field.
        /// </summary>
        public void AddRule(IKycRule Rule)
        {
            this.rules.Add(Rule);
            if (Rule is IAsyncKycRule)
                this.hasAsyncRules = true;
        }

        /// <summary>
        /// Forces immediate synchronous validation (used when advancing pages or explicit full validation is required).
        /// </summary>
        public void ForceSynchronousValidation()
        {
            try
            {
                this.syncValidationTask.Cancel();
                this.RunSynchronousValidation();
                if (this.hasAsyncRules)
                    this.ValidationTask.Run();
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
        }

        private void RunSynchronousValidation()
        {
            this.TryGetOwnerProcess(out KycProcess? process);

            foreach (IKycRule rule in this.rules)
            {
                if (rule is IAsyncKycRule)
                    continue;

                if (!rule.Validate(this, process, out string error))
                {
                    this.IsValid = false;
                    this.ValidationText = error;
                    return;
                }
            }

            this.IsValid = true;
            this.ValidationText = null;
        }

        protected virtual async Task<bool> ValidateAsync(TaskContext<int> context)
        {
            if (!this.IsValid)
                return false;

            this.TryGetOwnerProcess(out KycProcess? process);
            bool asyncFailed = false;
            string? asyncError = null;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => this.IsValidating = true);

                foreach (IKycRule rule in this.rules)
                {
                    if (rule is IAsyncKycRule asyncRule)
                    {
                        (bool Ok, string? Error) result = await asyncRule.ValidateAsync(this, process);
                        if (!result.Ok)
                        {
                            asyncFailed = true;
                            asyncError = result.Error;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex);
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() => this.IsValidating = false);
            }

            if (asyncFailed)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    this.IsValid = false;
                    this.ValidationText = asyncError;
                });
                return false;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (this.IsValid)
                    this.ValidationText = null;
            });
            return this.IsValid;
        }

        /// <summary>
        /// Map the value of this field to your application viewmodel.
        /// </summary>
        public virtual void MapToApplyModel()
        {
        }

        internal void SetOwnerProcess(KycProcess Process) => this.ownerProcess = new WeakReference<KycProcess>(Process);

        public bool TryGetOwnerProcess([MaybeNullWhen(false), NotNullWhen(true)] out KycProcess? Process)
        {
            if (this.ownerProcess is not null && this.ownerProcess.TryGetTarget(out KycProcess? p))
            {
                Process = p;
                return true;
            }

            Process = null;
            return false;
        }
    }
}
