using NeuroAccessMaui.Services.Contacts;

namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// A row for someone who wants to add the user, waiting at the top of the contacts list.
	/// </summary>
	/// <param name="Request">The request.</param>
	public sealed class ContactRequestItem(ContactRequest Request)
	{
		/// <summary>
		/// The request.
		/// </summary>
		public ContactRequest Request { get; } = Request;

		/// <summary>
		/// Their name.
		/// </summary>
		public string Name => this.Request.Name;

		/// <summary>
		/// If they shared an ID that the app has checked.
		/// </summary>
		public bool IsVerified => this.Request.IsVerified;

		/// <summary>
		/// The initials on the avatar.
		/// </summary>
		public string Initials
		{
			get
			{
				string[] Words = this.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				string Result = string.Concat(Words.Take(2).Select(Word => char.ToUpperInvariant(Word[0])));
				return Result.Length > 0 ? Result : "?";
			}
		}
	}
}
