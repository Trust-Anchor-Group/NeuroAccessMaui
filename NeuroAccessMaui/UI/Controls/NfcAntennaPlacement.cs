namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Identifies where the phone's NFC antenna is located, so placement guidance shows the right contact point.
	/// </summary>
	public enum NfcAntennaPlacement
	{
		/// <summary>
		/// The antenna is near the middle of the phone's back, which is typical for Android phones.
		/// </summary>
		BackCenter,

		/// <summary>
		/// The antenna is along the top edge of the phone, which is typical for iPhones.
		/// </summary>
		TopEdge
	}
}
