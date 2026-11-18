using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// A chip above the contacts list that filters it: all contacts, requests, verified, blocked, or a label.
	/// </summary>
	/// <param name="Key">What the chip filters by: a fixed key, or a label prefixed with "label:".</param>
	/// <param name="Text">What the chip says.</param>
	public sealed partial class ContactFilterChip(string Key, string Text) : ObservableObject
	{
		/// <summary>
		/// The chip that shows all contacts.
		/// </summary>
		public const string AllKey = "all";

		/// <summary>
		/// The chip that shows only requests.
		/// </summary>
		public const string RequestsKey = "requests";

		/// <summary>
		/// The chip that shows only verified contacts.
		/// </summary>
		public const string VerifiedKey = "verified";

		/// <summary>
		/// The chip that shows only blocked people.
		/// </summary>
		public const string BlockedKey = "blocked";

		/// <summary>
		/// The prefix of a label chip's key.
		/// </summary>
		public const string LabelPrefix = "label:";

		/// <summary>
		/// What the chip filters by.
		/// </summary>
		public string Key { get; } = Key;

		/// <summary>
		/// What the chip says.
		/// </summary>
		public string Text { get; } = Text;

		/// <summary>
		/// If the chip is the selected filter.
		/// </summary>
		[ObservableProperty]
		private bool isSelected;
	}
}
