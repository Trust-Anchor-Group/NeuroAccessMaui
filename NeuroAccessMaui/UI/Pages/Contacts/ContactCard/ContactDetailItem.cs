using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Pages.Contacts.ContactCard
{
	/// <summary>
	/// A row of contact details on the contact card: a phone number, an email address, an address, an organization or a
	/// birthday.
	/// </summary>
	/// <param name="Kind">What the row holds.</param>
	/// <param name="Label">What the value is, such as Mobile or Email.</param>
	/// <param name="Value">The value, as shown.</param>
	/// <param name="IsFromId">If the value is the same as in the contact's checked ID.</param>
	public sealed record ContactDetailItem(ContactDetailKind Kind, string Label, string Value, bool IsFromId)
	{
		private const string phoneIcon = "M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z";
		private const string emailIcon = "M5 5h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2z M3 7l9 6 9-6";
		private const string addressIcon = "M12 21s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z M12 6.5a2.5 2.5 0 1 1 0 5a2.5 2.5 0 1 1 0-5";
		private const string organizationIcon = "M4 21V5h10v16M14 9h6v12M8 9h2M8 13h2M8 17h2";
		private const string birthdayIcon = "M4 21h16v-8H4z M4 16c2 0 2-1.5 4-1.5s2 1.5 4 1.5 2-1.5 4-1.5 2 1.5 4 1.5 M12 13V9 M12 6h0.01";

		/// <summary>
		/// The row's icon.
		/// </summary>
		public Geometry Icon => GeometryConverter.ParseStringToPathGeometry(this.Kind switch
		{
			ContactDetailKind.Phone => phoneIcon,
			ContactDetailKind.Email => emailIcon,
			ContactDetailKind.Address => addressIcon,
			ContactDetailKind.Organization => organizationIcon,
			_ => birthdayIcon
		});

		/// <summary>
		/// If a tap on the row does something.
		/// </summary>
		public bool IsActionable => this.Kind is ContactDetailKind.Phone or ContactDetailKind.Email or ContactDetailKind.Address;
	}
}
