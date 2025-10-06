using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription
{
	/// <summary>
	/// Asks the user if it wants to remove an existing presence subscription request as well.
	/// </summary>
	public partial class RemoveSubscriptionPopup : BasePopup
	{
		private readonly RemoveSubscriptionViewModel viewModel;

		/// <summary>
		/// Asks the user if it wants to remove an existing presence subscription request as well.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		public RemoveSubscriptionPopup(RemoveSubscriptionViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = this.viewModel = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
