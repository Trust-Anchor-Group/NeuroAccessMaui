using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscriptionRequest
{
	/// <summary>
	/// Prompts the user for a response of a presence subscription request.
	/// </summary>
	public partial class SubscriptionRequestPopup : BasePopup
	{
		private readonly SubscriptionRequestViewModel viewModel;

		public SubscriptionRequestPopup(SubscriptionRequestViewModel viewModel)
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
