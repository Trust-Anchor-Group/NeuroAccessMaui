using System;
using System.ComponentModel;
using Waher.Events;
using Waher.Things;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents an error from a thing.
	/// </summary>
	public class ErrorModel : INotifyPropertyChanged
	{
		private ThingError error;

		/// <summary>
		/// Represents an error from a thing.
		/// </summary>
		/// <param name="Error">Error information.</param>
		public ErrorModel(ThingError Error)
		{
			this.error = Error;
		}

		/// <summary>
		/// Error message
		/// </summary>
		public string ErrorMessage => this.error.ErrorMessage;

		/// <summary>
		/// Timestamp of field.
		/// </summary>
		public DateTime Timestamp => this.error.Timestamp;

		/// <summary>
		/// Underlying field object.
		/// </summary>
		public ThingError Error
		{
			get => this.error;
			set
			{
				bool ErrorMessageChanged = this.error.ErrorMessage != value.ErrorMessage;
				bool TimestampChanged = this.error.Timestamp != value.Timestamp;

				this.error = value;

				if (ErrorMessageChanged)
					this.RaisePropertyChanged(nameof(this.ErrorMessage));

				if (TimestampChanged)
					this.RaisePropertyChanged(nameof(this.Timestamp));
			}
		}

		private void RaisePropertyChanged(string Name)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
