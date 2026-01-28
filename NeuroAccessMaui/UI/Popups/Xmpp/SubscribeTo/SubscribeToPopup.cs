using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo
{
	/// <summary>
	/// Asks the user if it wants to remove an existing presence subscription request as well.
	/// </summary>
	public partial class SubscribeToPopup : SimplePopup
	{
		private readonly SubscribeToViewModel viewModel;

		public SubscribeToPopup(SubscribeToViewModel viewModel)
		{
			this.InitializeComponent();
			this.viewModel = viewModel;
			this.BindingContext = viewModel;
		}

		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
