using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Main.Apps
{
	/// <summary>
	/// Main page for viewing apps.
	/// </summary>
	public partial class AppsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="AppsPage"/> class.
		/// </summary>
		/// <param name="ViewModel"></param>
		public AppsPage(AppsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;

			if (this.BindingContext is AppsViewModel Model)
				Model.PropertyChanged += this.Vm_PropertyChanged;
		}

		private async void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AppsViewModel.ShowingComingSoonPopup))
			{
				if (this.BindingContext is AppsViewModel Vm)
				{
					this.ComingSoonPopup.CancelAnimations();
					this.MainContent.CancelAnimations();

					if (Vm.ShowingComingSoonPopup)
					{
						this.ComingSoonPopup.Opacity = 0;
						this.ComingSoonPopup.Scale = 0.8;
						this.ComingSoonPopup.IsVisible = true;

						this.MainContent.Opacity = 1;

						_ = this.ComingSoonPopup.FadeTo(1, 500, Easing.CubicIn);
						_ = this.ComingSoonPopup.ScaleTo(1, 500, Easing.CubicIn);

						await this.MainContent.FadeTo(0.6, 500, Easing.CubicOut);
					}
					else
					{
						this.MainContent.Opacity = 0.6;

						this.ComingSoonPopup.Opacity = 1;
						this.ComingSoonPopup.Scale = 1;
						this.ComingSoonPopup.IsVisible = true;

						_ = this.MainContent.FadeTo(1, 500, Easing.CubicIn);

						_ = this.ComingSoonPopup.FadeTo(0, 500, Easing.CubicOut);
						await this.ComingSoonPopup.ScaleTo(0.8, 500, Easing.CubicOut);

						this.ComingSoonPopup.IsVisible = false;
					}
				}
			}
		}
	}
}
