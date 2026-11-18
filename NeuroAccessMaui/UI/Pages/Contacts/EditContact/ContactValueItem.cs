using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contacts.EditContact
{
	/// <summary>
	/// A phone number or email address being written on the edit page. Its types, such as mobile or work, are kept from the
	/// card.
	/// </summary>
	public partial class ContactValueItem : ObservableObject
	{
		/// <summary>
		/// A phone number or email address being written.
		/// </summary>
		/// <param name="Text">The value.</param>
		/// <param name="Types">Its types, such as "cell", "home" or "work".</param>
		public ContactValueItem(string Text, IReadOnlyList<string> Types)
		{
			this.text = Text;
			this.Types = Types;
		}

		/// <summary>
		/// The value.
		/// </summary>
		[ObservableProperty]
		private string text;

		/// <summary>
		/// Its types, such as "cell", "home" or "work".
		/// </summary>
		public IReadOnlyList<string> Types { get; }
	}
}
