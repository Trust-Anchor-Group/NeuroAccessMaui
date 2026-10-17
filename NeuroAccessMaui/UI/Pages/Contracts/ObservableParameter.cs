using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Popups.Info;
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

			return ParameterInfo;
		}

		/// <summary>
		/// Creates a new instance of <see cref="ObservableParameter"/> for a parameter that isn't part of a contract, such
		/// as a parameter of a token's note command. Without a contract there is no description, so the label is the
		/// parameter's guide, or its name.
		/// </summary>
		/// <param name="parameter">The parameter object to wrap</param>
		/// <returns>The wrapped parameter.</returns>
		public static Task<ObservableParameter> CreateAsync(Parameter parameter)
		{
			return Task.FromResult(Wrap(parameter));
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
				this.ShowErrorCommand.NotifyCanExecuteChanged();
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

		public bool CanShowError => !string.IsNullOrEmpty(this.ValidationText);

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

		#region Commands


		[RelayCommand(CanExecute = nameof(CanShowError), AllowConcurrentExecutions = false)]
		private async Task ShowError()
		{
			if (string.IsNullOrEmpty(this.ValidationText))
				return;
			ShowInfoPopup Popup = new ShowInfoPopup(this.Parameter.ErrorReason?.ToString() ?? ServiceRef.Localizer[nameof(AppResources.Error)], this.ValidationText);
			await ServiceRef.PopupService.PushAsync(Popup);
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
				this.OnPropertyChanged(nameof(this.BooleanValue));
		}

		public ObservableBooleanParameter(BooleanParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as bool?);
		}

		public bool BooleanValue
		{
			get => this.Value as bool? ?? false;
			set => this.Value = value;
		}

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
				this.OnPropertyChanged(nameof(this.DateValue));
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
				this.OnPropertyChanged(nameof(this.StringValue));
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
				this.OnPropertyChanged(nameof(this.TimeSpanValue));
		}

		public ObservableTimeParameter(TimeParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue as TimeSpan?);
		}

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
				this.OnPropertyChanged(nameof(this.StringValue));
			}
		}

		public ObservableDurationParameter(DurationParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue is Duration Duration ? Duration : null);
		}

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
				this.OnPropertyChanged(nameof(this.RoleValue));
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
			}
		}

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
			}
		}

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
				this.OnPropertyChanged(nameof(this.ContractReferenceValue));
		}

		public ObservableContractReferenceParameter(ContractReferenceParameter parameter) : base(parameter)
		{
			this.InitializeValue(parameter.ObjectValue);
		}

		public string ContractReferenceValue
		{
			get => this.Value?.ToString() ?? string.Empty;
			set => this.Value = value;
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
