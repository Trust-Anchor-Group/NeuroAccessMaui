using IdApp.Controls.FlipView;
using System;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// A page that allows the user to view the contents of its wallet, pending payments and recent account events.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyWalletPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyWalletPage"/> class.
		/// </summary>
		public MyWalletPage()
		{
			this.ViewModel = new MyWalletViewModel(this);

			this.InitializeComponent();
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.WalletFrontTabBar.Show();
			await this.WalletBackTabBar.Show();
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnDisappearingAsync()
		{
			await this.WalletFrontTabBar.Hide();
			await this.WalletBackTabBar.Hide();
			await base.OnDisappearingAsync();
		}

		internal void WalletFlipView_Tapped(object? Sender, EventArgs e)
		{
			this.WalletFlipView.Flip();
		}

		private void WalletFlipView_BackViewShowing(object? Sender, EventArgs e)
		{
			if (this.ViewModel is MyWalletViewModel Model)
				Model.BindTokens();
		}

		private void WalletFlipView_Flipped(object? Sender, EventArgs e)
		{
			if (this.ViewModel is MyWalletViewModel Model && Sender is FlipView FlipView)
				Model.ViewsFlipped(FlipView.IsFrontViewShowing);
		}
	}
}
