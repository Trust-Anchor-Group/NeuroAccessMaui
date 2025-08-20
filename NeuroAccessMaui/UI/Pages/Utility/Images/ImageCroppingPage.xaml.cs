using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using System;

namespace NeuroAccessMaui.UI.Pages.Utility.Images
{
	public partial class ImageCroppingPage
	{
		/// <summary>
		/// Initializes a new instance of <see cref="ImageCroppingPage"/>.
		/// </summary>
		public ImageCroppingPage()
		{
			this.InitializeComponent();
			// 1. Retrieve the nav args
			ImageCroppingNavigationArgs? NavArgs = ServiceRef.NavigationService.PopLatestArgs<ImageCroppingNavigationArgs>();
			// 2. Create the ViewModel
			ImageCroppingViewModel ViewModel = new ImageCroppingViewModel(NavArgs);
			// 3. Assign the view model as the binding context
			this.BindingContext = ViewModel;
			// 4. Set the ViewModel's reference to the ImageCropperView
			ViewModel.ImageCropperView = this.ImageCropperView;
		}
	}
}
