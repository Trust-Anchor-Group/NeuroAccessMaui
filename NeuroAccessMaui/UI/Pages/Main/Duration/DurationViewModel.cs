using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Main.Duration
{
	/// <summary>
	/// The view model to bind to for when displaying the duration.
	/// </summary>
	public partial class DurationViewModel : XmppViewModel
	{
		private bool skipEvaluations = false;

		/// <summary>
		/// Creates an instance of the <see cref="DurationViewModel"/> class.
		/// </summary>
		public DurationViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out DurationNavigationArgs? Args))
				this.Entry = Args.Entry;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.EvaluateDuration();

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Current entry
		/// </summary>
		[ObservableProperty]
		private Waher.Content.Duration value;

		/// <summary>
		/// Entry control, if available
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Value))]
		[NotifyPropertyChangedFor(nameof(Years))]
		[NotifyPropertyChangedFor(nameof(Months))]
		[NotifyPropertyChangedFor(nameof(Days))]
		[NotifyPropertyChangedFor(nameof(Hours))]
		[NotifyPropertyChangedFor(nameof(Minutes))]
		[NotifyPropertyChangedFor(nameof(Seconds))]
		[NotifyPropertyChangedFor(nameof(IsNegativeDuration))]
		private Entry? entry;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsNegativeDuration):
				case nameof(this.Years):
				case nameof(this.Months):
				case nameof(this.Days):
				case nameof(this.Hours):
				case nameof(this.Minutes):
				case nameof(this.Seconds):
					this.EvaluateDuration();
					break;

				case nameof(this.Entry):
					if ((this.Entry is not null) && (this.Entry.BindingContext is ParameterInfo ParameterInfo))
					{
						this.Value = ParameterInfo.DurationValue;
						this.SplitDuration();
					}
					break;

				case nameof(this.Value):
					if ((this.Entry is not null) && (this.Entry.BindingContext is ParameterInfo ParameterInfo2))
						ParameterInfo2.DurationValue = this.Value;
					break;
			}
		}

		/// <summary>
		/// IsNegativeDuration
		/// </summary>
		[ObservableProperty]
		private bool isNegativeDuration;

		/// <summary>
		/// Years
		/// </summary>
		[ObservableProperty]
		private string years = string.Empty;

		/// <summary>
		/// Months
		/// </summary>
		[ObservableProperty]
		private string months = string.Empty;

		/// <summary>
		/// Days
		/// </summary>
		[ObservableProperty]
		private string days = string.Empty;

		/// <summary>
		/// Hours
		/// </summary>
		[ObservableProperty]
		private string hours = string.Empty;

		/// <summary>
		/// Minutes
		/// </summary>
		[ObservableProperty]
		private string minutes = string.Empty;

		/// <summary>
		/// Seconds
		/// </summary>
		[ObservableProperty]
		private string seconds = string.Empty;

		/// <summary>
		/// If Years component is OK
		/// </summary>
		[ObservableProperty]
		private bool yearsOk = false;

		/// <summary>
		/// If Months component is OK
		/// </summary>
		[ObservableProperty]
		private bool monthsOk = false;

		/// <summary>
		/// If Days component is OK
		/// </summary>
		[ObservableProperty]
		private bool daysOk = false;

		/// <summary>
		/// If Hours component is OK
		/// </summary>
		[ObservableProperty]
		private bool hoursOk = false;

		/// <summary>
		/// If Minutes component is OK
		/// </summary>
		[ObservableProperty]
		private bool minutesOk = false;

		/// <summary>
		/// If Seconds component is OK
		/// </summary>
		[ObservableProperty]
		private bool secondsOk = false;

		#endregion

		#region Commands

		[RelayCommand]
		private void PlusMinus()
		{
			this.IsNegativeDuration = !this.IsNegativeDuration;
		}

		/// <summary>
		/// Split the current duration value into components.
		/// </summary>
		public void SplitDuration()
		{
			this.skipEvaluations = true;

			this.IsNegativeDuration = this.Value.Negation;
			this.Years = ComponentToString(this.Value.Years);
			this.Months = ComponentToString(this.Value.Months);
			this.Days = ComponentToString(this.Value.Days);
			this.Hours = ComponentToString(this.Value.Hours);
			this.Minutes = ComponentToString(this.Value.Minutes);
			this.Seconds = ComponentToString(this.Value.Seconds);

			this.skipEvaluations = false;
		}

		private static string ComponentToString(int Value)
		{
			if (Value == 0)
				return string.Empty;
			else
				return Value.ToString(CultureInfo.InvariantCulture);
		}

		private static string ComponentToString(double Value)
		{
			if (Value == 0)
				return string.Empty;
			else
				return Value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Evaluates the current duration.
		/// </summary>
		public void EvaluateDuration()
		{
			if (this.skipEvaluations)
				return;

			bool IsZero = true;
			StringBuilder sb = new();

			if (this.IsNegativeDuration)
				sb.Append("-P");
			else
				sb.Append('P');

			if (!string.IsNullOrEmpty(this.Years))
			{
				IsZero = false;
				sb.Append(this.Years);
				sb.Append('Y');
				this.YearsOk = int.TryParse(this.Years, out _);
			}
			else
				this.YearsOk = true;

			if (!string.IsNullOrEmpty(this.Months))
			{
				IsZero = false;
				sb.Append(this.Months);
				sb.Append('M');
				this.MonthsOk = int.TryParse(this.Months, out _);
			}
			else
				this.MonthsOk = true;

			if (!string.IsNullOrEmpty(this.Days))
			{
				IsZero = false;
				sb.Append(this.Days);
				sb.Append('D');
				this.DaysOk = int.TryParse(this.Days, out _);
			}
			else
				this.DaysOk = true;

			bool IsHours = !string.IsNullOrEmpty(this.Hours);
			bool IsMinutes = !string.IsNullOrEmpty(this.Minutes);
			bool IsSeconds = !string.IsNullOrEmpty(this.Seconds);

			if (IsHours || IsMinutes || IsSeconds)
			{
				IsZero = false;
				sb.Append('T');

				if (IsHours)
				{
					sb.Append(this.Hours);
					sb.Append('H');
					this.HoursOk = int.TryParse(this.Hours, out _);
				}
				else
					this.HoursOk = true;

				if (IsMinutes)
				{
					sb.Append(this.Minutes);
					sb.Append('M');
					this.MinutesOk = int.TryParse(this.Minutes, out _);
				}
				else
					this.MinutesOk = true;

				if (IsSeconds)
				{
					sb.Append(this.Seconds);
					sb.Append('S');
					this.SecondsOk = CommonTypes.TryParse(this.Seconds, out double _);
				}
				else
					this.SecondsOk = true;
			}
			else
			{
				this.HoursOk = true;
				this.MinutesOk = true;
				this.SecondsOk = true;
			}

			if (IsZero)
				sb.Append("PT0S");

			if (Waher.Content.Duration.TryParse(sb.ToString(), out Waher.Content.Duration Result))
				this.Value = Result;
		}

		#endregion
	}
}
