using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Parameter"/> object.
	/// This allows for easier binding in the UI.
	/// Must be either Initialized with <see cref="CreateAsync"/> or <see cref="InitializeAsync"/>
	/// </summary>
	public class ParameterInfo2 : ObservableObject
	{
		protected ParameterInfo2(Parameter parameter)
		{
			this.Parameter = parameter;
		}

		/// <summary>
		/// Initializes the parameter in regards to a contract.
		/// </summary>
		public async Task InitializeAsync(Contract contract)
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
		///  Creates a new instance of <see cref="ParameterInfo2"/> based on the type of the parameter.
		/// </summary>
		/// <param name="parameter">The parameter to wrap</param>
		/// <param name="contract">The contract that</param>
		/// <returns></returns>
		public static async Task<ParameterInfo2> CreateAsync(Parameter parameter, Contract contract)
		{
			
			ParameterInfo2 parameterInfo = parameter switch
			{
				BooleanParameter booleanParameter => new BooleanParameterInfo(booleanParameter),
				DateParameter dateParameter => new DateParameterInfo(dateParameter),
				NumericalParameter numericalParameter => new NumericalParameterInfo(numericalParameter),
				StringParameter stringParameter => new StringParameterInfo(stringParameter),
				TimeParameter timeParameter => new TimeParameterInfo(timeParameter),
				DurationParameter durationParameter => new DurationParameterInfo(durationParameter),
				_ => new ParameterInfo2(parameter)

			};
			await parameterInfo.InitializeAsync(contract);
			return parameterInfo;
		}

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

		public string Label { 
			get {
				if (string.IsNullOrEmpty(this.Description))
				{
					if(string.IsNullOrEmpty(this.Guide))
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
				if(this.SetProperty(ref this.description, value))
					this.OnPropertyChanged(nameof(this.Label));

			}
		}
		private string description = string.Empty;

		/// <summary>
		/// If the parameter has an validation error
		/// </summary>
		public bool Error
		{
			get => this.error;
			set => this.SetProperty(ref this.error, value);
		}
		private bool error;

		private string errorText = string.Empty;

		/// <summary>
		/// Error text to display if the parameter has an validation error
		/// </summary>
		public string ErrorText
		{
			get => this.errorText;
			set => this.SetProperty(ref this.errorText, value);
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
		}


		private object? value;
		public object? Value 
		{ 
			get => this.value; 
			set			
			{
					this.SetProperty(ref this.value, value);
					try
					{
						if(value is not null)
							this.Parameter.SetValue(value);
					}
					catch (Exception e)
					{
						ServiceRef.LogService.LogException(e);
					}
			}
		}
	}

	public sealed class BooleanParameterInfo : ParameterInfo2
	{

		public BooleanParameterInfo(BooleanParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue is true;
		}
		public bool BooleanValue
		{
			get => this.Value as bool? ?? false;
			set => this.Value = value;
		}
	}

	public class DateParameterInfo : ParameterInfo2
	{
		public DateParameterInfo(DateParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue as DateTime?;
		}

		public DateTime? DateValue
		{
			get => this.Value as DateTime?;
			set => this.Value = value;
		}
		
	}

	public class NumericalParameterInfo : ParameterInfo2
	{
		public NumericalParameterInfo(NumericalParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue is decimal decimalValue ? decimalValue : 0;
		}


		public decimal DecimalValue
		{
			get => this.Value as decimal? ?? 0;
			set => this.Value = value;
		}
	}

	public class StringParameterInfo : ParameterInfo2
	{

		public StringParameterInfo(StringParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue as string ?? string.Empty;
		}

		public string StringValue
		{
			get => this.Value as string ?? string.Empty;
			set => this.Value = value;
		}
	}

	public class TimeParameterInfo : ParameterInfo2
	{
		public TimeParameterInfo(TimeParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue is TimeSpan timeSpan ? timeSpan : TimeSpan.Zero;
		}



		public TimeSpan TimeSpanValue
		{
			get => this.Value as TimeSpan? ?? TimeSpan.Zero;
			set => this.Value = value;
		}
	}

	public class DurationParameterInfo : ParameterInfo2
	{
		public DurationParameterInfo(DurationParameter parameter) : base(parameter)
		{
			this.Value = parameter.ObjectValue is Duration duration ? duration : Duration.Zero;
		}

		public Duration DurationValue
		{
			get => this.Value as Duration? ?? Duration.Zero;
			set => this.Value = value;
		}
	}
}
