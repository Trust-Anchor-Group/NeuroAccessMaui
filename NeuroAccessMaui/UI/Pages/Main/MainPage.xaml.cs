using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainPage
	{
		public MainPage(MainViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
			this.BindingContext = ViewModel;

			this.BindingContextChanged += this.OnBindingContextChanged;

			// Subscribe for the initial BindingContext
			if (this.BindingContext is MainViewModel vm)
				vm.PropertyChanged += this.Vm_PropertyChanged;
		}

		private void OnBindingContextChanged(object? sender, EventArgs e)
		{
			if (this.BindingContext is MainViewModel vm)
				vm.PropertyChanged += this.Vm_PropertyChanged;
		}

		private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
		{
			try
			{
				if(this.ContentPageModel is MainViewModel ViewModel)
					await ViewModel.ViewId();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MainViewModel.ShowingNoWalletPopup))
			{
				if (this.BindingContext is MainViewModel Vm)
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
						this.NoIDPopup.IsVisible = true;

						_ = this.NoWalletPopup.FadeTo(1, 500, Easing.CubicIn);
						_ = this.NoWalletPopup.ScaleTo(1, 500, Easing.CubicIn);

						_ = this.NoIDPopup.FadeTo(0.8, 500, Easing.CubicOut);
						await this.NoIDPopup.ScaleTo(0.8, 500, Easing.CubicOut);

						this.NoIDPopup.IsVisible = false;
					}
					else
					{
						this.NoIDPopup.Opacity = 0;
						this.NoIDPopup.Scale = 0.8;
						this.NoIDPopup.IsVisible = true;

						this.NoWalletPopup.Opacity = 1;
						this.NoWalletPopup.Scale = 1;
						this.NoWalletPopup.IsVisible = true;

						_ = this.NoIDPopup.FadeTo(1, 1000, Easing.CubicIn);
						_ = this.NoIDPopup.ScaleTo(1, 1000, Easing.CubicIn);

						_ = this.NoWalletPopup.FadeTo(0, 1000, Easing.CubicOut);
						await this.NoWalletPopup.ScaleTo(0.8, 1000, Easing.CubicOut);

						this.NoWalletPopup.IsVisible = false;
					}
				}
			}
		}
	}
}
