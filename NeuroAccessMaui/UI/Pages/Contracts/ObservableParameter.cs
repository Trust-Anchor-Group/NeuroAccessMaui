using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Popups.Geo;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Geo;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Parameter"/> object.
	/// This allows for easier binding in the UI. Must be instantiated with <see cref="CreateAsync"/>.
	/// </summary>
	public partial class ObservableParameter : ObservableObject
	{
		#region Constructor
		protected ObservableParameter(Parameter parameter)
		{
			this.Parameter = parameter;
			this.value = parameter.ObjectValue;
		}
		#endregion

		#region Initialization
		/// <summary>
		/// Applies a non-null initial editor value while preserving protected parameter data.
		/// </summary>
		/// <param name="Value">The initial value or editor default.</param>
		protected void InitializeValue(object? Value)
		{
			if (Value is not null && this.Parameter.ProtectedValue is null)
				this.Value = Value;
		}

		/// <summary>
		/// Initializes the parameter in regards to a contract.
		/// </summary>
		private async Task InitializeAsync(Contract contract)
		{
			try
			{
				string UntrimmedDescription = await contract.ToPlainText(this.Parameter.Descriptions, contract.DeviceLanguage());
				this.Description = UntrimmedDescription.Trim();
			}
			catch (Exception E)
			{
				ServiceRef.LogService.LogException(E);
			}
		}

		/// <summary>
		///  Creates a new instance of <see cref="ObservableParameter"/> based on the type of the parameter.
		/// </summary>
		/// <param name="parameter">The parameter object to wrap</param>
		/// <param name="contract">The contract of which the parameter is part of</param>
		/// <returns></returns>
		public static async Task<ObservableParameter> CreateAsync(Parameter parameter, Contract contract)
		{
			ObservableParameter ParameterInfo = Wrap(parameter);

			await ParameterInfo.InitializeAsync(contract);

			if(ParameterInfo.Parameter is RoleParameter RP)
			{
				if(RP.Value is not null && RP.Value is string RoleValue && string.IsNullOrEmpty(RoleValue))
				{
					RP.SetValue(null);
				}
			}
			else if (ParameterInfo.Parameter is CalcParameter CP)
			{
				if (string.IsNullOrEmpty(CP.StringValue))
					CP.SetValue(null);
			}
			else if (ParameterInfo is ObservableNumericalParameter Numerical)
				Numerical.FindUnit(contract);

			return ParameterInfo;
		}

		/// <summary>
		/// Creates a new instance of <see cref="ObservableParameter"/> for a parameter that isn't part of a contract, such
		/// as a parameter of a token's note command. The label is the parameter's description in the user's language,
		/// without the colon desktop clients put at the end. When it has none, the label is the parameter's guide, or its
		/// name.
		/// </summary>
		/// <param name="parameter">The parameter object to wrap</param>
		/// <returns>The wrapped parameter.</returns>
		public static async Task<ObservableParameter> CreateAsync(Parameter parameter)
		{
			ObservableParameter ParameterInfo = Wrap(parameter);

			if (parameter.Descriptions is not null && parameter.Descriptions.Length > 0)
			{
				try
				{
					// There is no contract, so the description is rendered with an empty one.
					string? UntrimmedDescription = await new Contract().ToPlainText(parameter.Descriptions,
						CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

					ParameterInfo.Description = (UntrimmedDescription ?? string.Empty).Trim().TrimEnd(':').TrimEnd();
				}
				catch (Exception E)
				{
					ServiceRef.LogService.LogException(E);
				}
			}

			return ParameterInfo;
		}

		/// <summary>
		/// Wraps a parameter in the observable type that matches its type.
		/// </summary>
		/// <param name="parameter">The parameter object to wrap</param>
		/// <returns>The wrapped parameter.</returns>
		private static ObservableParameter Wrap(Parameter parameter)
		{
			return parameter switch
			{
				BooleanParameter BooleanParameter => new ObservableBooleanParameter(BooleanParameter),
				DateParameter DateParameter => new ObservableDateParameter(DateParameter),
				DateTimeParameter DateTimeParameter => new ObservableDateTimeParameter(DateTimeParameter),
				NumericalParameter NumericalParameter => new ObservableNumericalParameter(NumericalParameter),
				StringParameter StringParameter => new ObservableStringParameter(StringParameter),
				TimeParameter TimeParameter => new ObservableTimeParameter(TimeParameter),
				DurationParameter DurationParameter => new ObservableDurationParameter(DurationParameter),
				RoleParameter RoleParameter => new ObservableRoleParameter(RoleParameter),
				CalcParameter CalcParameter => new ObservableCalcParameter(CalcParameter),
				ContractReferenceParameter ContractReferenceParameter => new ObservableContractReferenceParameter(ContractReferenceParameter),
				GeoParameter GeoParameter => new ObservableGeoParameter(GeoParameter),
				_ => new ObservableParameter(parameter)
			};
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
		/// If the parameter is avlid
		/// </summary>
		public bool IsValid
		{
			get => this.isValid;
			set => this.SetProperty(ref this.isValid, value);
		}
		private bool isValid = true;

		/// <summary>
		/// Validation error text
		/// </summary>
		public string ValidationText
		{
			get => this.validationText;
			set
			{
				this.SetProperty(ref this.validationText, value);
				this.OnPropertyChanged(nameof(this.CanShowError));
			}
		}
		private string validationText = string.Empty;

		private object? value;

		/// <summary>
		/// Gets or sets the parameter value, retaining its converted type after a successful assignment.
		/// </summary>
		public object? Value
		{
			get => this.value;
			set
			{
				try
				{
					if (value is null)
						this.ClearValue();
					else if (value is string Text && this.Parameter is
						DateParameter or DateTimeParameter or TimeParameter or DurationParameter or GeoParameter)
						this.Parameter.StringValue = Text;
					else
						this.Parameter.SetValue(value);

					value = this.Parameter.ObjectValue;
				}
				catch (Exception E)
				{
					ServiceRef.LogService.LogException(E);
					return;
				}
				this.SetProperty(ref this.value, value);
				this.OnPropertyChanged(nameof(this.CanReadValue));
				this.OnPropertyChanged(nameof(this.IsValueMissing));
			}
		}

		/// <summary>
		/// If nobody has entered a value yet, and the template didn't provide one.
		/// </summary>
		public bool IsValueMissing => this.value is null;

		/// <summary>
		/// Shows the value the parameter holds now, after it was worked out somewhere else, such as a calculated value
		/// during validation.
		/// </summary>
		public void RefreshValue()
		{
			if (this.SetProperty(ref this.value, this.Parameter.ObjectValue, nameof(this.Value)))
			{
				this.OnPropertyChanged(nameof(this.CanReadValue));
				this.OnPropertyChanged(nameof(this.IsValueMissing));
			}
		}

		/// <summary>
		/// Clears values through nullable properties for parameter types whose SetValue rejects null.
		/// </summary>
		private void ClearValue()
		{
			switch (this.Parameter)
			{
				case BooleanParameter BooleanParameter:
					BooleanParameter.Value = null;
					break;
				case DateParameter DateParameter:
					DateParameter.Value = null;
					break;
				case DateTimeParameter DateTimeParameter:
					DateTimeParameter.Value = null;
					break;
				case NumericalParameter NumericalParameter:
					NumericalParameter.Value = null;
					break;
				case TimeParameter TimeParameter:
					TimeParameter.Value = null;
					break;
				case DurationParameter DurationParameter:
					DurationParameter.Value = null;
					break;
				case GeoParameter GeoParameter:
					GeoParameter.Value = null;
					break;
				case ContractReferenceParameter ContractReferenceParameter:
					ContractReferenceParameter.Value = null;
					break;
				default:
					this.Parameter.SetValue(null);
					break;
			}
		}

		/// <summary>
		/// If the parameter is transient
		/// </summary>
		public bool IsTransient => this.Parameter.Protection == ProtectionLevel.Transient;

		/// <summary>
		/// If the parameter is encrypted
		/// </summary>
		public bool IsEncrypted => this.Parameter.Protection == ProtectionLevel.Encrypted;

		/// <summary>
		/// If the parameter is protected, either encrypted or transient
		/// </summary>
		public bool IsProtected => this.IsTransient || this.IsEncrypted;

		/// <summary>
		/// If the parameter can be read, for example encrypted parameters cannot be read if you don't have the key.
		/// </summary>
		/// 
		public bool CanReadValue => this.IsProtected ? this.Parameter.ObjectValue is not null : this.Parameter.CanSerializeValue;

		/// <summary>
		/// If the value may still arrive, because the contract page is showing a saved copy while it reads the current one.
		/// </summary>
		public bool IsLoadingValue
		{
			get => this.isLoadingValue;
			set
			{
				if (this.SetProperty(ref this.isLoadingValue, value))
					this.OnPropertyChanged(nameof(this.EncryptedValueText));
			}
		}
		private bool isLoadingValue;

		/// <summary>
		/// What to show instead of an encrypted value that can't be read.
		/// </summary>
		public string EncryptedValueText => this.IsLoadingValue
			? ServiceRef.Localizer[nameof(AppResources.ParameterLoadingValue)]
			: ServiceRef.Localizer[nameof(AppResources.ParameterEncryptedNotAvailable)];
		#endregion

		/// <summary>
		/// If there is a text that says why the value isn't accepted.
		/// </summary>
		public bool CanShowError => !string.IsNullOrEmpty(this.ValidationText);

		/// <summary>
		/// A short hint about which values are allowed, such as "0 to 100", or an empty string.
		/// </summary>
		public string Hint => this.hint ??= ParameterMessages.GetHint(this.Parameter);
		private string? hint;

		/// <summary>
		/// If there is a hint about which values are allowed.
		/// </summary>
		public bool HasHint => !string.IsNullOrEmpty(this.Hint);

		#region Formatting
		/// <summary>
		/// Formats a number for the current culture, keeping every significant decimal and dropping trailing zeros.
		/// </summary>
		/// <param name="Value">The number to format.</param>
		/// <returns>The localized text.</returns>
		public static string FormatNumber(decimal Value)
		{
			decimal Normalized = Value / 1.000000000000000000000000000000000m;
			return Normalized.ToString("N" + Normalized.Scale.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Formats a time span for the current culture, keeping its days.
		/// </summary>
		/// <param name="Value">The time span to format.</param>
		/// <returns>The localized text.</returns>
		public static string FormatTimeSpan(TimeSpan Value)
		{
			CultureInfo Culture = CultureInfo.CurrentCulture;
			string Sign = Value < TimeSpan.Zero ? "-" : string.Empty;
			TimeSpan Length = Value.Duration();
			string Time = Length.ToString(@"hh\:mm\:ss", Culture);

			if (Length.Days == 0)
				return Sign + Time;

			string Unit = Length.Days == 1
				? ServiceRef.Localizer[nameof(AppResources.Day)]
				: ServiceRef.Localizer[nameof(AppResources.Days)];

			return Sign + Length.Days.ToString(Culture) + " " + Unit + ", " + Time;
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
	public partial class ObservableBooleanParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.BooleanValue));
				this.OnPropertyChanged(nameof(this.BooleanText));
			}
		}

		public ObservableBooleanParameter(BooleanParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as bool?);
		}

		public bool BooleanValue
		{
			get => this.Value as bool? ?? false;
			set
			{
				// The switch reports "off" while nothing is chosen; that isn't a choice.
				if (this.Value is null && !value)
					return;

				this.Value = value;
			}
		}

		/// <summary>
		/// "Yes" or "No", or that nothing is chosen yet.
		/// </summary>
		public string BooleanText => this.Value switch
		{
			true => ServiceRef.Localizer[nameof(AppResources.Yes)],
			false => ServiceRef.Localizer[nameof(AppResources.No)],
			_ => ServiceRef.Localizer[nameof(AppResources.ParameterNotSet)]
		};

		[RelayCommand]
		private void ToggleBooleanValue()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.BooleanValue = !this.BooleanValue;
			});
		}
	}

	public class ObservableDateParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.DateValue));
				this.OnPropertyChanged(nameof(this.DateText));
			}
		}

		public ObservableDateParameter(DateParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as DateTime?);
		}

		public DateTime? DateValue
		{
			get => this.Value as DateTime?;
			set => this.Value = value;
		}

		/// <summary>
		/// The date in the user's short format, or "-".
		/// </summary>
		public string DateText => this.DateValue?.ToString("d", CultureInfo.CurrentCulture) ?? "-";
	}

	/// <summary>
	/// Exposes a numerical contract parameter for binding to a numeric editor.
	/// </summary>
	public class ObservableNumericalParameter : ObservableParameter
	{
		/// <summary>
		/// Initializes the observable value from a numerical contract parameter.
		/// </summary>
		/// <param name="Parameter">The numerical parameter to wrap.</param>
		public ObservableNumericalParameter(NumericalParameter Parameter) : base(Parameter)
		{
			this.InitializeValue(Parameter.ObjectValue is decimal DecimalValue ? DecimalValue : null);
		}

		/// <summary>
		/// Gets or sets the decimal value displayed by the numeric editor.
		/// </summary>
		public decimal? DecimalValue
		{
			get => this.Value as decimal?;
			set => this.Value = value;
		}

		/// <summary>
		/// The value as localized text, with its full precision.
		/// </summary>
		public string DecimalText => this.DecimalValue is decimal Number ? FormatNumber(Number) : "-";

		/// <summary>
		/// The value with its unit or currency, such as "250 EUR" or "5 %".
		/// </summary>
		public string DecimalTextWithUnit => this.HasUnit && this.DecimalValue.HasValue
			? this.DecimalText + " " + this.Unit
			: this.DecimalText;

		/// <summary>
		/// The unit or currency of the number, or an empty string when it isn't known.
		/// </summary>
		public string Unit => this.isPercent ? "%" : this.currencyParameter?.StringValue ?? string.Empty;

		/// <summary>
		/// If the number has a known unit or currency.
		/// </summary>
		public bool HasUnit => !string.IsNullOrEmpty(this.Unit);

		/// <summary>
		/// If the number is a percentage.
		/// </summary>
		private bool isPercent;

		/// <summary>
		/// The parameter that holds the currency of the number, if any.
		/// </summary>
		private Parameter? currencyParameter;

		/// <summary>
		/// Words in parameter names that mean an amount of money.
		/// </summary>
		private static readonly string[] amountWords = ["Amount", "Price", "Cost", "Fee", "Total", "Sum", "Value", "Payment", "Deposit"];

		/// <summary>
		/// Works out the number's unit. A number whose name or guide speaks of percent is a percentage. An amount, judged by
		/// its name, is in the currency of a parameter named "Currency", when the contract has one.
		/// </summary>
		/// <param name="Contract">The contract the parameter belongs to.</param>
		public void FindUnit(Contract Contract)
		{
			string Name = this.Parameter.Name ?? string.Empty;
			string Guide = this.Parameter.Guide ?? string.Empty;

			if (Name.Contains("Percent", StringComparison.OrdinalIgnoreCase) || Guide.Contains('%'))
			{
				this.isPercent = true;
				return;
			}

			if (!amountWords.Any(Word => Name.Contains(Word, StringComparison.OrdinalIgnoreCase)))
				return;

			this.currencyParameter = Contract.Parameters?.FirstOrDefault(P =>
				P is StringParameter && string.Equals(P.Name, "Currency", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Shows the unit again, for example after the currency was changed.
		/// </summary>
		public void RefreshUnit()
		{
			this.OnPropertyChanged(nameof(this.Unit));
			this.OnPropertyChanged(nameof(this.HasUnit));
			this.OnPropertyChanged(nameof(this.DecimalTextWithUnit));
		}

		/// <summary>
		/// Notifies the numeric editor when the underlying observable value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.DecimalValue));
				this.OnPropertyChanged(nameof(this.DecimalText));
				this.OnPropertyChanged(nameof(this.DecimalTextWithUnit));
			}
		}
	}

	public class ObservableStringParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.StringValue));
				this.OnPropertyChanged(nameof(this.LengthCounter));
			}
		}

		public ObservableStringParameter(StringParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as string);
		}

		public string StringValue
		{
			get => this.Value as string ?? string.Empty;
			set => this.Value = value;
		}

		/// <summary>
		/// If the contract limits the length of the text, so a counter is shown.
		/// </summary>
		public bool HasLengthCounter => ((StringParameter)this.Parameter).MaxLength.HasValue;

		/// <summary>
		/// How long the text is, out of the longest length allowed, such as "12/40".
		/// </summary>
		public string LengthCounter => this.HasLengthCounter
			? this.StringValue.Length.ToString(CultureInfo.CurrentCulture) + "/" +
				((StringParameter)this.Parameter).MaxLength!.Value.ToString(CultureInfo.CurrentCulture)
			: string.Empty;

		/// <summary>
		/// If there is a hint or a length counter to show under the input.
		/// </summary>
		public bool HasHintOrCounter => this.HasHint || this.HasLengthCounter;
	}

	public class ObservableTimeParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.TimeSpanValue));
				this.OnPropertyChanged(nameof(this.TimeText));
			}
		}

		public ObservableTimeParameter(TimeParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as TimeSpan?);
		}

		/// <summary>
		/// The time in the user's short format, without seconds, or "-".
		/// </summary>
		public string TimeText => this.TimeSpanValue is TimeSpan Time
			? DateTime.Today.Add(Time).ToString("t", CultureInfo.CurrentCulture)
			: "-";

		/// <summary>
		/// The time, or null while nobody has chosen one.
		/// </summary>
		public TimeSpan? TimeSpanValue
		{
			get => this.Value as TimeSpan?;
			set
			{
				if (value == this.TimeSpanValue)
					return;

				this.Value = value;
			}
		}
	}

	public class ObservableDurationParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.DurationValue));
				this.OnPropertyChanged(nameof(this.NullableDurationValue));
				this.OnPropertyChanged(nameof(this.StringValue));
			}
		}

		public ObservableDurationParameter(DurationParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue is Duration Duration ? Duration : null);
		}

		/// <summary>
		/// The duration, or null while nobody has entered one.
		/// </summary>
		public Duration? NullableDurationValue
		{
			get => this.Value as Duration?;
			set
			{
				if (Nullable.Equals(value, this.NullableDurationValue))
					return;

				this.Value = value;
			}
		}

		/// <summary>
		/// If the contract allows a duration that goes backwards in time.
		/// </summary>
		public bool AllowsNegative => ((DurationParameter)this.Parameter).Min is Duration Min && Min.Negation;

		public string StringValue
		{
			get => this.Value?.ToString() ?? string.Empty;
			set
			{
				if (Duration.TryParse(value, out Duration DurationValue))
					this.Value = DurationValue;
				else
					this.Value = null;
			}
		}

		/// <summary>
		/// The duration shown by the picker, which shows an unset duration as empty.
		/// </summary>
		public Duration DurationValue
		{
			get => this.Value as Duration? ?? Duration.Zero;
			set
			{
				// The picker reports an empty duration while nothing is set; that isn't a value someone chose.
				if (this.Value is null && value.Equals(Duration.Zero))
					return;

				if (Duration.TryParse(value.ToString(), out Duration DurationValue))
					this.Value = DurationValue;
				else
					this.Value = null;

			}
		}
	}

	public class ObservableRoleParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.RoleValue));
				this.OnPropertyChanged(nameof(this.HasRoleValue));
				this.OnPropertyChanged(nameof(this.RoleDisplayText));
			}
		}

		public ObservableRoleParameter(RoleParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as string);
		}

		public string RoleValue
		{
			get => this.Value as string ?? string.Empty;
			set => this.Value = value;
		}

		/// <summary>
		/// The role whose signer's ID the value is taken from.
		/// </summary>
		public string RoleName => ((RoleParameter)this.Parameter).Role ?? string.Empty;

		/// <summary>
		/// Where the value comes from, such as "From the Buyer's ID".
		/// </summary>
		public string SourceText => ServiceRef.Localizer[nameof(AppResources.ParameterFromId), this.RoleName];

		/// <summary>
		/// If the value is known, because someone has signed in the role.
		/// </summary>
		public bool HasRoleValue => !string.IsNullOrEmpty(this.RoleValue);

		/// <summary>
		/// The value, or when it will be known, such as "When the Buyer signs".
		/// </summary>
		public string RoleDisplayText => this.HasRoleValue
			? this.RoleValue
			: ServiceRef.Localizer[nameof(AppResources.ParameterAddedOnSigning), this.RoleName];
	}

	public class ObservableCalcParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.CalcValue));
				this.OnPropertyChanged(nameof(this.CalcString));
				this.OnPropertyChanged(nameof(this.HasCalcValue));
				this.OnPropertyChanged(nameof(this.CalcDisplayText));
			}
		}

		/// <summary>
		/// If the value could be worked out from the values filled in so far.
		/// </summary>
		public bool HasCalcValue => this.Value is not null;

		/// <summary>
		/// The calculated value, or that it can't be worked out yet.
		/// </summary>
		public string CalcDisplayText => this.HasCalcValue
			? this.CalcString
			: ServiceRef.Localizer[nameof(AppResources.ParameterCantCalculate)];

		public ObservableCalcParameter(CalcParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue);
		}

		public object? CalcValue
		{
			get => this.Value;
			set => this.Value = value;
		}

		public string CalcString
		{
			get
			{
				object? Value = this.Value ?? this.Parameter.StringValue;
				CultureInfo Culture = CultureInfo.CurrentCulture;

				return Value switch
				{
					decimal DecimalValue => FormatNumber(DecimalValue),
					double DoubleValue => DoubleValue.ToString("G17", Culture),
					DateTime DateTimeValue => (DateTimeValue.Kind == DateTimeKind.Utc ? DateTimeValue.ToLocalTime() : DateTimeValue).ToString("G", Culture),
					TimeSpan TimeSpanValue => FormatTimeSpan(TimeSpanValue),
					Duration DurationValue => DurationToString.ToString(DurationValue),
					bool BooleanValue => ServiceRef.Localizer[BooleanValue ? nameof(AppResources.Yes) : nameof(AppResources.No)].Value,
					string StringValue => string.IsNullOrEmpty(StringValue) ? "-" : StringValue,
					_ => Value?.ToString() ?? string.Empty
				};
			}
		}
	}

	public class ObservableDateTimeParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.DateTimeValue));
				this.OnPropertyChanged(nameof(this.SelectedDate));
				this.OnPropertyChanged(nameof(this.SelectedTime));
				this.OnPropertyChanged(nameof(this.DateTimeText));
			}
		}

		/// <summary>
		/// The date and time in the user's short format, in local time and without seconds, or "-".
		/// </summary>
		public string DateTimeText => this.DateTimeValue is DateTime Value
			? (Value.Kind == DateTimeKind.Utc ? Value.ToLocalTime() : Value).ToString("g", CultureInfo.CurrentCulture)
			: "-";

		public ObservableDateTimeParameter(DateTimeParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as DateTime?);
		}

		/// <summary>
		/// A time chosen before the date, kept until a date is chosen.
		/// </summary>
		private TimeSpan? pendingTime;

		/// <summary>
		/// The DateTime value of the parameter.
		/// When changed, updates the underlying parameter value.
		/// </summary>
		public DateTime? DateTimeValue
		{
			get => this.Value as DateTime?;
			set => this.Value = value;
		}

		/// <summary>
		/// Helper to get or set just the Date portion.
		/// </summary>
		public DateTime? SelectedDate
		{
			get => this.DateTimeValue?.Date;
			set
			{
				if (value == this.SelectedDate)
					return;

				if (value.HasValue)
				{
					TimeSpan Time = this.SelectedTime ?? TimeSpan.Zero;
					this.pendingTime = null;
					this.DateTimeValue = value.Value.Date + new TimeSpan(Time.Hours, Time.Minutes, Time.Seconds);
				}
				else
				{
					this.pendingTime = this.DateTimeValue?.TimeOfDay;
					this.DateTimeValue = null;
				}
			}
		}

		/// <summary>
		/// Helper to get or set just the Time portion. Without a date, the time waits for one.
		/// </summary>
		public TimeSpan? SelectedTime
		{
			get => this.DateTimeValue?.TimeOfDay ?? this.pendingTime;
			set
			{
				// Binding feedback must not create a date nobody chose.
				if (value == this.SelectedTime)
					return;

				if (this.DateTimeValue is DateTime Current && value.HasValue)
					this.DateTimeValue = Current.Date + value.Value;
				else
				{
					this.pendingTime = value;
					this.OnPropertyChanged(nameof(this.SelectedTime));
				}
			}
		}
	}

	public partial class ObservableContractReferenceParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.ContractReferenceValue));
				this.OnPropertyChanged(nameof(this.HasContract));
				this.OnPropertyChanged(nameof(this.CanPasteId));
				this.OnPropertyChanged(nameof(this.ContractTitle));
				this.OnPropertyChanged(nameof(this.ContractSubtitle));
				_ = this.LoadReferencedContractAsync();
			}
		}

		public ObservableContractReferenceParameter(ContractReferenceParameter parameter) : base(parameter)
		{
			// A value set here also looks up the contract, through the change of the value.
			this.InitializeValue(parameter.ObjectValue);
		}

		public string ContractReferenceValue
		{
			get => this.Value?.ToString() ?? string.Empty;
			set => this.Value = value;
		}

		/// <summary>
		/// The chosen contract, as the phone has it, or null when it isn't on the phone.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ContractTitle))]
		[NotifyPropertyChangedFor(nameof(ContractSubtitle))]
		private ContractModel? referencedContract;

		/// <summary>
		/// If the user chose to type or paste the contract's ID instead of choosing it from a list.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanPasteId))]
		private bool isPastingId;

		/// <summary>
		/// If a contract has been chosen.
		/// </summary>
		public bool HasContract => !string.IsNullOrEmpty(this.ContractReferenceValue);

		/// <summary>
		/// If "Paste ID" is offered: nothing is chosen, and the ID field isn't shown yet.
		/// </summary>
		public bool CanPasteId => !this.HasContract && !this.IsPastingId;

		/// <summary>
		/// The chosen contract's name, or its ID when the phone doesn't have it.
		/// </summary>
		public string ContractTitle => this.ReferencedContract is ContractModel Model && !string.IsNullOrEmpty(Model.NameOrCategory)
			? Model.NameOrCategory
			: this.ContractReferenceValue;

		/// <summary>
		/// The chosen contract's state and when it last changed, when the phone has it.
		/// </summary>
		public string ContractSubtitle
		{
			get
			{
				if (this.ReferencedContract is not ContractModel Model)
					return string.Empty;

				string[] Parts = [Model.StateText, Model.LastChangedText];
				return string.Join(" · ", Parts.Where(Text => !string.IsNullOrEmpty(Text)));
			}
		}

		/// <summary>
		/// Looks up the chosen contract on the phone, so it can be shown by its name and state.
		/// </summary>
		private async Task LoadReferencedContractAsync()
		{
			string Id = this.ContractReferenceValue;
			ContractModel? Model = null;

			if (!string.IsNullOrEmpty(Id))
			{
				try
				{
					ContractReference? Row = await ServiceRef.ContractStore.GetAsync(Id);
					if (Row is not null)
						Model = new ContractModel(Row);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				// A newer choice wins over a lookup that finished late.
				if (Id == this.ContractReferenceValue)
					this.ReferencedContract = Model;
			});
		}

		/// <summary>
		/// Shows the field where the user types or pastes the contract's ID.
		/// </summary>
		[RelayCommand]
		private void PasteId()
		{
			this.IsPastingId = true;
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenContract()
		{
			if (string.IsNullOrEmpty(this.ContractReferenceValue))
			{
				return;
			}
			try
			{
				await ServiceRef.ContractOrchestratorService.OpenContractAsync(this.ContractReferenceValue, null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task PickContractReferenceAsync()
		{
			try
			{
				TaskCompletionSource<Contract?> TaskCompletionSource = new();
				ContractPickerFilter Filter = new ContractPickerFilter((ContractReferenceParameter)this.Parameter);
				MyContractsNavigationArgs Args = new MyContractsNavigationArgs(ContractsListMode.Contracts, TaskCompletionSource, Filter);
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args);
				Contract? Contract = await TaskCompletionSource.Task;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (Contract is null)
						return;
					this.ContractReferenceValue = Contract?.ContractId ?? string.Empty;
				});
			}
			catch (Exception E)
			{
				ServiceRef.LogService.LogException(E);
			}
		}
	}

	// Class for ObservableGeoParameter
	public partial class ObservableGeoParameter : ObservableParameter
	{
		/// <summary>
		/// Notifies editor bindings when the underlying parameter value changes.
		/// </summary>
		/// <param name="Args">The property change notification.</param>
		protected override void OnPropertyChanged(PropertyChangedEventArgs Args)
		{
			base.OnPropertyChanged(Args);

			if (Args.PropertyName == nameof(this.Value))
			{
				this.OnPropertyChanged(nameof(this.GeoValue));
				this.OnPropertyChanged(nameof(this.GeoString));
				this.OnPropertyChanged(nameof(this.GeoText));
				this.OnPropertyChanged(nameof(this.HasGeoValue));
				this.OpenMapCommand.NotifyCanExecuteChanged();
			}
		}

		/// <summary>
		/// If a place has been chosen.
		/// </summary>
		public bool HasGeoValue => this.GeoValue is not null;

		/// <summary>
		/// The place as degrees north or south and east or west, such as "59.3293° N, 18.0686° E", or that it isn't
		/// filled in.
		/// </summary>
		public string GeoText
		{
			get
			{
				if (this.GeoValue is not GeoPosition Position)
					return ServiceRef.Localizer[nameof(AppResources.ParameterNotSet)];

				CultureInfo Culture = CultureInfo.CurrentCulture;
				string Latitude = Math.Abs(Position.Latitude).ToString("0.0000", Culture) + "° " + (Position.Latitude < 0 ? "S" : "N");
				string Longitude = Math.Abs(Position.Longitude).ToString("0.0000", Culture) + "° " + (Position.Longitude < 0 ? "W" : "E");

				return Latitude + ", " + Longitude;
			}
		}

		/// <summary>
		/// If the place is where the contract applies.
		/// </summary>
		public bool IsContractLocation => ((GeoParameter)this.Parameter).ContractLocation;

		/// <summary>
		/// Opens a map where the user picks the place by moving the map under a pin.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task PickOnMapAsync()
		{
			try
			{
				GeoMapViewModel ViewModel = new(this.GeoValue);
				await ServiceRef.PopupService.PushAsync(new GeoMapPopup(ViewModel));

				GeoPosition? Picked = await ViewModel.Result;
				if (Picked is not null)
					MainThread.BeginInvokeOnMainThread(() => this.GeoValue = Picked);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows the place on OpenStreetMap.
		/// </summary>
		[RelayCommand(CanExecute = nameof(HasGeoValue))]
		private async Task OpenMapAsync()
		{
			if (this.GeoValue is not GeoPosition Position)
				return;

			try
			{
				// Maps are OpenStreetMap throughout the app, so the place opens there rather than in the phone's map app.
				string Latitude = Position.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
				string Longitude = Position.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
				Uri Url = new("https://www.openstreetmap.org/?mlat=" + Latitude + "&mlon=" + Longitude + "#map=16/" + Latitude + "/" + Longitude);

				await Browser.Default.OpenAsync(Url, BrowserLaunchMode.SystemPreferred);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		public ObservableGeoParameter(GeoParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue);
		}

		public GeoPosition? GeoValue
		{
			get => this.Value as GeoPosition;
			set => this.Value = value;
		}

		public string GeoString
		{
			get => this.GeoValue?.ToString() ?? string.Empty;
			set
			{
				if (GeoPosition.TryParse(value, out GeoPosition? GeoValue))
					this.Value = GeoValue;
				else
					this.Value = null;
			}
		}

		[ObservableProperty]
		bool isCheckinglocation;

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task SetCurrentLocationAsync()
		{
			try
			{
				MainThread.BeginInvokeOnMainThread(() => this.IsCheckinglocation = true);

				GeolocationRequest Request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
				Location? Location = await Geolocation.Default.GetLocationAsync(Request);

				if (Location is not null)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						this.GeoValue = new GeoPosition(Location.Latitude, Location.Longitude);
					});
				}
			}
			catch
			{
				// Ignore this case
				// Display an alert requesting permission
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.LocationError)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]
				);
			}
			finally
			{
				MainThread.BeginInvokeOnMainThread(() => this.IsCheckinglocation = false);
			}
		}
	}
}
#endregion
