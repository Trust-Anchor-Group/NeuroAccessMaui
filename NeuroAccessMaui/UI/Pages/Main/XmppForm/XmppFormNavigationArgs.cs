using NeuroAccessMaui.Services.UI;
using Waher.Networking.XMPP.DataForms;

namespace NeuroAccessMaui.UI.Pages.Main.XmppForm
{
	/// <summary>
	/// Holds navigation parameters for an XMPP Form.
	/// </summary>
	/// <param name="Form">XMPP Data Form</param>
	public class XmppFormNavigationArgs(DataForm? Form) : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for an XMPP Form.
		/// </summary>
		public XmppFormNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// XMPP Data Form
		/// </summary>
		public DataForm? Form { get; } = Form;
	}
}
