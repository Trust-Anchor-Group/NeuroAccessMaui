using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
    /// <summary>
    /// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Parameter"/> object.
    /// This allows for easier binding in the UI.
    /// </summary>
    public class ObservableParameter : ObservableObject
    {
        #region Constructor
        protected ObservableParameter(Parameter parameter)
        {
            this.Parameter = parameter;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the parameter in regards to a contract.
        /// </summary>
        private async Task InitializeAsync(Contract contract)
        {
            try
            {
                this.Description = await contract.ToPlainText(this.Parameter.Descriptions, contract.DeviceLanguage());
            }
            catch (Exception e)
            {
                ServiceRef.LogService.LogException(e);
            }
        }

        /// <summary>
        ///  Creates a new instance of <see cref="ObservableParameter"/> based on the type of the parameter.
        /// </summary>
        /// <param name="parameter">The parameter to wrap</param>
        /// <param name="contract">The contract that</param>
        /// <returns></returns>
        public static async Task<ObservableParameter> CreateAsync(Parameter parameter, Contract contract)
        {
            ObservableParameter parameterInfo = parameter switch
            {
                BooleanParameter booleanParameter => new ObservableBooleanParameter(booleanParameter),
                DateParameter dateParameter => new ObservableDateParameter(dateParameter),
                NumericalParameter numericalParameter => new ObservableNumericalParameter(numericalParameter),
                StringParameter stringParameter => new ObservableStringParameter(stringParameter),
                TimeParameter timeParameter => new ObservableTimeParameter(timeParameter),
                DurationParameter durationParameter => new ObservableDurationParameter(durationParameter),
                _ => new ObservableParameter(parameter)
            };

            await parameterInfo.InitializeAsync(contract);
            return parameterInfo;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The wrapped parameter object
        /// </summary>
        public Parameter Parameter { get; private set; }

        /// <summary>
        /// The name of the parameter
        /// </summary>
        public string Name => this.Parameter.Name;

        /// <summary>
        /// The Guide of the parameter
        /// </summary>
        public string Guide => string.IsNullOrEmpty(this.Parameter.Guide) ? this.Name : this.Parameter.Guide;

        /// <summary>
        /// Label that determines the displayed text for the parameter.
        /// </summary>
        public string Label
        {
            get
            {
                if (string.IsNullOrEmpty(this.Description))
                {
                    if (string.IsNullOrEmpty(this.Guide))
                    {
                        return this.Name;
                    }
                    return this.Guide;
                }
                return this.Description;
            }
        }

        /// <summary>
        /// The localized description of the parameter
        /// </summary>
        public string Description
        {
            get => this.description;
            private set
            {
                if (this.SetProperty(ref this.description, value))
                    this.OnPropertyChanged(nameof(this.Label));
            }
        }
        private string description = string.Empty;

        /// <summary>
        /// If the parameter has a validation error
        /// </summary>
        public bool HasError
        {
            get => this.hasError;
            set => this.SetProperty(ref this.hasError, value);
        }
        private bool hasError;

        /// <summary>
        /// Error text to display if the parameter has a validation error
        /// </summary>
        public string ErrorText
        {
            get => this.errorText;
            set => this.SetProperty(ref this.errorText, value);
        }
        private string errorText = string.Empty;

        /// <summary>
        /// The value of the parameter
        /// </summary>
        private object? value;
        public object? Value
        {
            get => this.value;
            set
            {
                if (this.SetProperty(ref this.value, value))
                {
                    try
                    {
                        if (value is not null)
                            this.Parameter.SetValue(value);
                    }
                    catch (Exception e)
                    {
                        ServiceRef.LogService.LogException(e);
                    }
                }
            }
        }
        #endregion

        #region Property Change Handling
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }
        #endregion
    }

    #region ObservableParameter Subclasses
    public sealed class ObservableBooleanParameter : ObservableParameter
    {
        public ObservableBooleanParameter(BooleanParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue is true;
        }

        public bool BooleanValue
        {
            get => this.Value as bool? ?? false;
            set => this.Value = value;
        }
    }

    public class ObservableDateParameter : ObservableParameter
    {
        public ObservableDateParameter(DateParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue as DateTime?;
        }

        public DateTime? DateValue
        {
            get => this.Value as DateTime?;
            set => this.Value = value;
        }
    }

    public class ObservableNumericalParameter : ObservableParameter
    {
        public ObservableNumericalParameter(NumericalParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue is decimal decimalValue ? decimalValue : 0;
        }

        public decimal DecimalValue
        {
            get => this.Value as decimal? ?? 0;
            set => this.Value = value;
        }
    }

    public class ObservableStringParameter : ObservableParameter
    {
        public ObservableStringParameter(StringParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue as string ?? string.Empty;
        }

        public string StringValue
        {
            get => this.Value as string ?? string.Empty;
            set => this.Value = value;
        }
    }

    public class ObservableTimeParameter : ObservableParameter
    {
        public ObservableTimeParameter(TimeParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue is TimeSpan timeSpan ? timeSpan : TimeSpan.Zero;
        }

        public TimeSpan TimeSpanValue
        {
            get => this.Value as TimeSpan? ?? TimeSpan.Zero;
            set => this.Value = value;
        }
    }

    public class ObservableDurationParameter : ObservableParameter
    {
        public ObservableDurationParameter(DurationParameter parameter) : base(parameter)
        {
            this.Value = parameter.ObjectValue is Duration duration ? duration : Duration.Zero;
        }

        public Duration DurationValue
        {
            get => this.Value as Duration? ?? Duration.Zero;
            set => this.Value = value;
        }
    }
    #endregion
}
