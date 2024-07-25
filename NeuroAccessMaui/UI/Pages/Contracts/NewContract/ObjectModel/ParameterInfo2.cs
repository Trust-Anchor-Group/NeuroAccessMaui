using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// Contains information about a parameter.
	/// </summary>
	public class ParameterInfo2 : ObservableObject
	{
		protected ParameterInfo2(Parameter parameter)
		{
			this.Parameter = parameter;
		}

		public Parameter Parameter { get; }
		public string Name => this.Parameter.Name;
		public string Guide => this.Parameter.Guide;

		private bool error;
		public bool Error
		{
			get => error;
			set => SetProperty(ref error, value);
		}

		private Duration durationValue = Duration.Zero;

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
		}

		/// <summary>
		/// Duration object
		/// </summary>
		public Duration DurationValue
		{
			get => this.durationValue;
			set
			{
				this.durationValue = value;
				this.OnPropertyChanged();
			}
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
				Console.WriteLine("SET BOOL");
				if (value is bool boolValue)
				{
					Console.WriteLine("SET BOOL 2");

					this.Parameter.SetValue(boolValue);
					this.SetProperty(ref this.value, boolValue);
				}
			}
		}

		public bool BooleanValue
		{
			get => (bool)Value;
			set => Value = value;
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
			get => (DateTime)Value;
			set => Value = value;
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
			get => (decimal)Value;
			set => Value = value;
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
			get => (string)Value;
			set => Value = value;
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
			get => (TimeSpan)Value;
			set => Value = value;
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
			get => (Duration)Value;
			set => Value = value;
		}
	}
}
