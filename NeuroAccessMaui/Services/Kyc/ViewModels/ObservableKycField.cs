using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services.Kyc.Models;

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
        Label,   // non-input
        Info     // non-input
    }

    /// <summary>
    /// The base KYC field model. Use as-is for generic fields, or subclass for custom logic.
    /// </summary>
    public abstract class ObservableKycField : ObservableObject
    {
        private readonly List<IKycRule> rules = new();
        public ReadOnlyCollection<IKycRule> ValidationRules => this.rules.AsReadOnly();

        public ObservableKycField()
        {
            this.Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            this.rules.Add(new RequiredRule());
            this.SelectedOptions.CollectionChanged += this.SelectedOptions_CollectionChanged;
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
                    this.Validate();
                }

				ServiceRef.LogService.LogDebug(this.StringValue ?? string.Empty);
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
        public void AddRule(IKycRule Rule) => this.rules.Add(Rule);

        /// <summary>
        /// Validate this field using all attached rules.
        /// </summary>
        public virtual bool Validate(string? Lang = null)
        {
            foreach (IKycRule Rule in this.rules)
            {
                if (!Rule.Validate(this, out string Error, Lang))
                {
                    this.IsValid = false;
                    this.ValidationText = Error;
                    return false;
                }
            }
            this.IsValid = true;
            this.ValidationText = null;
            return true;
        }

        /// <summary>
        /// Map the value of this field to your application viewmodel.
        /// </summary>
        public virtual void MapToApplyModel()
        {
            // TODO: Implement mapping logic based on Mappings/Transforms
        }
    }
}
