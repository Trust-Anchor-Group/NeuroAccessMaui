using CommunityToolkit.Mvvm.ComponentModel;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel
{
	/// <summary>
	/// Contains information about a parameter.
	/// </summary>
	/// <param name="Parameter">Contract parameter.</param>
	/// <param name="Control">Generated control.</param>
	public class ParameterInfo(Parameter Parameter, View? Control) : ObservableObject
    {
		private Duration durationValue = Duration.Zero;

		/// <summary>
		/// Contract parameter.
		/// </summary>
		public Parameter Parameter { get; internal set; } = Parameter;

		/// <summary>
		/// Generated control.
		/// </summary>
		public View? Control { get; internal set; } = Control;

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
	}
}
