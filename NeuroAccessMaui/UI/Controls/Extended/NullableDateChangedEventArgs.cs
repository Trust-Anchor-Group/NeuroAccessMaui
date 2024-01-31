namespace NeuroAccessMaui.UI.Controls.Extended
{
	/// <summary>
	/// An ExtendedDatePicker event args .
	/// </summary>
	/// <param name="oldDate">The previous date</param>
	/// <param name="newDate">The selected date</param>
	public class NullableDateChangedEventArgs(DateTime? oldDate, DateTime? newDate) : EventArgs
	{
		/// <summary>
		/// The previous date
		/// </summary>
		public DateTime? NewDate { get; private set; } = newDate;

		/// <summary>
		/// The selected date
		/// </summary>
		public DateTime? OldDate { get; private set; } = oldDate;
	}
}
