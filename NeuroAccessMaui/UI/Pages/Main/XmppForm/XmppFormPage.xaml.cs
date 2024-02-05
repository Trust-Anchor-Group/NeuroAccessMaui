namespace NeuroAccessMaui.UI.Pages.Main.XmppForm
{
	/// <summary>
	/// A page that displays an XMPP Form to the user.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class XmppFormPage
	{
		/// <summary>
		/// A page that displays an XMPP Form to the user.
		/// </summary>
		public XmppFormPage()
		{
			this.ViewModel = new XmppFormViewModel();
			this.InitializeComponent();
		}
	}
}
