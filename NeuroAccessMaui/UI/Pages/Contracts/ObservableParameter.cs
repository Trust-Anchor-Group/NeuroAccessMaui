using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
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
			ObservableParameter ParameterInfo = parameter switch
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

			await ParameterInfo.InitializeAsync(contract);
			return ParameterInfo;
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

		/// <summary>
		/// The value of the parameter
		/// </summary>
		private object? value;
		public object? Value
		{
			get => this.value;
			set
			{
				if (value is null)
					return;
				try
				{
					this.Parameter.SetValue(value);
				}
				catch (Exception E)
				{
					ServiceRef.LogService.LogException(E);
				}
				this.value = value;
				this.OnPropertyChanged(nameof(this.Value));
				this.OnPropertyChanged(nameof(this.CanReadValue));
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
		#endregion

		public bool CanShowError => !string.IsNullOrEmpty(this.ValidationText);
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
		public ObservableBooleanParameter(BooleanParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue is true;
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
				this.OnPropertyChanged(nameof(this.BooleanValue));
			});
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
			this.Value = parameter.ObjectValue is decimal DecimalValue ? DecimalValue : null;
		}

		public decimal? DecimalValue
		{
			get => this.Value as decimal?;
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
			this.Value = parameter.ObjectValue is TimeSpan TimeSpan ? TimeSpan : TimeSpan.Zero;
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
			this.Value = parameter.ObjectValue is Duration Duration ? Duration : Duration.Zero;
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

		public Duration DurationValue
		{
			get => this.Value as Duration? ?? Duration.Zero;
			set
			{
				if (Duration.TryParse(value.ToString(), out Duration DurationValue))
					this.Value = DurationValue;
				else
					this.Value = null;

			}
		}
	}

	public class ObservableRoleParameter : ObservableParameter
	{
		public ObservableRoleParameter(RoleParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue as string ?? string.Empty;
		}

		public string RoleValue
		{
			get => this.Value as string ?? string.Empty;
			set => this.Value = value;
		}
	}

	public class ObservableCalcParameter : ObservableParameter
	{
		public ObservableCalcParameter(CalcParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue;
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
				if (this.Value is null)
					this.Value = this.Parameter.StringValue;

				CultureInfo Culture = CultureInfo.CurrentCulture;

				Console.WriteLine(this.Value.GetType().Name);

				return this.Value switch
				{
					decimal DecimalValue => DecimalValue.ToString("N", Culture), // Number format with localization
					DateTime DateTimeValue => DateTimeValue.ToString("G", Culture), // Localized date format
					TimeSpan TimeSpanValue => TimeSpanValue.ToString(@"hh\:mm\:ss", Culture), // Time format
					string StringValue => string.IsNullOrEmpty(StringValue) ? "-" : StringValue,
					_ => this.Value.ToString() ?? string.Empty // Fallback for unknown types
				};
			}
		}
	}

	public class ObservableDateTimeParameter : ObservableParameter
	{
		public ObservableDateTimeParameter(DateTimeParameter parameter) : base(parameter)
		{
			// Extract initial value from parameter
			if (parameter.ObjectValue is DateTime Dt)
				this.Value = Dt;
			else
				this.Value = parameter.Min;  // or DateTime.MinValue as a fallback
		}

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
				if (value.HasValue)
				{
					DateTime Current = this.DateTimeValue ?? DateTime.MinValue;
					this.DateTimeValue = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, Current.Hour, Current.Minute, Current.Second);
				}
				else
				{
					this.DateTimeValue = null;
				}
			}
		}

		/// <summary>
		/// Helper to get or set just the Time portion.
		/// </summary>
		public TimeSpan SelectedTime
		{
			get => this.DateTimeValue?.TimeOfDay ?? TimeSpan.Zero;
			set
			{
				DateTime Current = this.DateTimeValue ?? DateTime.MinValue;
				this.DateTimeValue = Current.Date + value;
			}
		}
	}

	public partial class ObservableContractReferenceParameter : ObservableParameter
	{
		public ObservableContractReferenceParameter(ContractReferenceParameter parameter) : base(parameter)
		{
			ServiceRef.LogService.LogDebug($"{this.Value} - {this.Parameter.ObjectValue}");
			this.Value = this.Parameter.ObjectValue;
			//this.Value = parameter.ObjectValue as string ?? string.Empty;
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
				await ServiceRef.ContractOrchestratorService.OpenContract(this.ContractReferenceValue, ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)], null);
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
				MyContractsNavigationArgs Args = new MyContractsNavigationArgs(ContractsListMode.Contracts, TaskCompletionSource);
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args);
				Contract? Contract = await TaskCompletionSource.Task;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (Contract is null)
						return;
					this.ContractReferenceValue = Contract?.ContractId ?? string.Empty;
					this.OnPropertyChanged(nameof(this.ContractReferenceValue));
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
		public ObservableGeoParameter(GeoParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue;
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
						this.OnPropertyChanged(nameof(this.GeoString));
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
