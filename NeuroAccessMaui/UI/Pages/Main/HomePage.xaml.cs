using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class HomePage
	{
		public HomePage(HomeViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;

			if (this.BindingContext is HomeViewModel Model) 
				Model.PropertyChanged += this.Vm_PropertyChanged;
		}

		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
		}

		private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
		{
			try
			{
				if(this.ContentPageModel is HomeViewModel ViewModel)
					await ViewModel.ViewId();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(HomeViewModel.ShowingNoWalletPopup))
			{
				if (this.BindingContext is HomeViewModel Vm)
				{
					this.NoWalletPopup.CancelAnimations();
					this.NoIDPopup.CancelAnimations();

					if (Vm.ShowingNoWalletPopup)
					{
						this.NoWalletPopup.Opacity = 0;
						this.NoWalletPopup.Scale = 0.8;
						this.NoWalletPopup.IsVisible = true;

						this.NoIDPopup.Opacity = 1;
						this.NoIDPopup.Scale = 1;
						this.NoIDPopup.IsVisible = !Vm.HasPersonalIdentity;

						_ = this.NoWalletPopup.FadeToAsync(1, 500, Easing.CubicIn);
						_ = this.NoWalletPopup.ScaleToAsync(1, 500, Easing.CubicIn);

						_ = this.NoIDPopup.FadeToAsync(0.8, 500, Easing.CubicOut);
						await this.NoIDPopup.ScaleToAsync(0.8, 500, Easing.CubicOut);

						this.NoIDPopup.IsVisible = false;
					}
					else
					{
						this.NoIDPopup.Opacity = 0;
						this.NoIDPopup.Scale = 0.8;
						this.NoIDPopup.IsVisible = !Vm.HasPersonalIdentity;

						this.NoWalletPopup.Opacity = 1;
						this.NoWalletPopup.Scale = 1;
						this.NoWalletPopup.IsVisible = true;

						_ = this.NoIDPopup.FadeToAsync(1, 1000, Easing.CubicIn);
						_ = this.NoIDPopup.ScaleToAsync(1, 1000, Easing.CubicIn);

						_ = this.NoWalletPopup.FadeToAsync(0, 1000, Easing.CubicOut);
						await this.NoWalletPopup.ScaleToAsync(0.8, 1000, Easing.CubicOut);

						this.NoWalletPopup.IsVisible = false;
					}
				}
			}
		}
	}
}
