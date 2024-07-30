using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// An observable object that wraps a <see cref="Waher.Networking.XMPP.Contracts.Parameter"/> object.
	/// This allows for easier binding in the UI.
	/// </summary>
	public class ParameterInfo2 : ObservableObject
	{
		public ParameterInfo2(Parameter parameter)
		{
			this.Parameter = parameter;
		}

		/// <summary>
		/// Initializes the parameter in regards to a contract.
		/// E.g Sets the description of the parameter, with the contract language.
		/// </summary>
		/// <param name="contract"></param>
		/// <returns></returns>
		public async Task InitializeWithContractAsync(Contract contract)
		{
			this.Description = await contract.ToPlainText(this.Parameter.Descriptions, contract.DeviceLanguage());
		}

		/// <summary>
		/// The wrapped parameter object
		/// </summary>
		public Parameter Parameter { get; }
		/// <summary>
		/// The name of the parameter
		/// </summary>
		public string Name => this.Parameter.Name;

		/// <summary>
		/// The label of the parameter
		/// </summary>
		public string Guide => string.IsNullOrEmpty(this.Parameter.Guide) ? this.Name : this.Parameter.Guide;

		private string description = string.Empty;
		/// <summary>
		/// The localized description of the parameter
		/// Has to be initialized with <see cref="InitializeWithContractAsync"/>
		/// </summary>
		public string Description
		{
			get => this.description;
			private set => this.SetProperty(ref this.description, value);
		}

		private bool error;
		/// <summary>
		/// If the parameter has an validation error
		/// </summary>
		public bool Error
		{
			get => this.error;
			set => this.SetProperty(ref this.error, value);
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
		}


		public virtual object Value { get; set; }
	}

	public class BooleanParameterInfo : ParameterInfo2
	{
		private bool value;

		public BooleanParameterInfo(BooleanParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue is true;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is bool boolValue)
				{
					this.Parameter.SetValue(boolValue);
					this.SetProperty(ref this.value, boolValue);
				}
			}
		}

		public bool BooleanValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}

	public class DateParameterInfo : ParameterInfo2
	{
		private DateTime value;

		public DateParameterInfo(DateParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue is DateTime dateTime ? dateTime : DateTime.Now;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is DateTime dateTime)
				{
					this.Parameter.SetValue(dateTime);
					this.SetProperty(ref this.value, dateTime);
				}
			}
		}

		public DateTime DateValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}

	public class NumericalParameterInfo : ParameterInfo2
	{
		private decimal value;

		public NumericalParameterInfo(NumericalParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue is decimal decimalValue ? decimalValue : 0;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is decimal decimalValue)
				{
					this.Parameter.SetValue(decimalValue);
					this.SetProperty(ref this.value, decimalValue);
				}
			}
		}

		public decimal DecimalValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}

	public class StringParameterInfo : ParameterInfo2
	{
		private string value;

		public StringParameterInfo(StringParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue as string ?? string.Empty;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is string stringValue)
				{
					this.Parameter.SetValue(stringValue);
					this.SetProperty(ref this.value, stringValue);
				}
			}
		}

		public string StringValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}

	public class TimeParameterInfo : ParameterInfo2
	{
		private TimeSpan value;

		public TimeParameterInfo(TimeParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue is TimeSpan timeSpan ? timeSpan : TimeSpan.Zero;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is TimeSpan timeSpan)
				{
					this.Parameter.SetValue(timeSpan);
					this.SetProperty(ref this.value, timeSpan);
				}
			}
		}

		public TimeSpan TimeSpanValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}

	public class DurationParameterInfo : ParameterInfo2
	{
		private Duration value;

		public DurationParameterInfo(DurationParameter parameter) : base(parameter)
		{
			this.value = parameter.ObjectValue is Duration duration ? duration : Duration.Zero;
		}

		public override object Value
		{
			get => this.value;
			set
			{
				if (value is Duration duration)
				{
					this.Parameter.SetValue(duration);
					this.SetProperty(ref this.value, duration);
				}
			}
		}

		public Duration DurationParameterValue
		{
			get => this.value;
			set => this.Value = value;
		}
	}
}
