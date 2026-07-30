namespace NeuroAccessMaui.UI.Pages.Main.NfcTester
{
	/// <summary>
	/// Represents a human-readable field returned by a travel-document scan.
	/// </summary>
	public sealed class NfcTesterResultItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcTesterResultItem"/> class.
		/// </summary>
		/// <param name="Label">The localized field label.</param>
		/// <param name="Value">The field value.</param>
		public NfcTesterResultItem(string Label, string Value)
		{
			this.Label = Label;
			this.Value = Value;
		}

		/// <summary>
		/// Gets the localized field label.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the field value.
		/// </summary>
		public string Value { get; }
	}
}
