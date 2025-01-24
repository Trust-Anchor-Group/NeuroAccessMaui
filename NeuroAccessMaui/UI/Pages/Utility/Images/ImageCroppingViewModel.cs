using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.UI.Controls;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Pages.Utility.Images
{
	/// <summary>
	/// The ViewModel for <see cref="ImageCroppingPage"/>.
	/// Handles cropping commands and navigation logic.
	/// </summary>
	public partial class ImageCroppingViewModel : BaseViewModel
	{
		/// <summary>
		/// The navigation arguments passed when the page was opened.
		/// </summary>
		private readonly ImageCroppingNavigationArgs? args;

		/// <summary>
		/// A direct reference to the ImageCropperView control,
		/// so we can call PerformCropAsync.
		/// </summary>
		[ObservableProperty]
		private ImageCropperView? imageCropperView;

		/// <summary>
		/// Initializes a new instance of the <see cref="ImageCroppingViewModel"/> class.
		/// </summary>
		/// <param name="args">The navigation args containing the Source image, TCS, etc.</param>
		public ImageCroppingViewModel(ImageCroppingNavigationArgs? args)
		{
			this.args = args;
		}

		protected override Task OnAppearing()
		{
			base.OnAppearing();

			if (this.args?.Source is not null && this.ImageCropperView is not null)
			{
				// Assign the incoming source to the cropper control
				this.ImageCropperView.ImageSource = this.args.Source;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Command to perform the actual crop and return the result.
		/// </summary>
		[RelayCommand]
		private async Task CropAsync()
		{
			if (this.ImageCropperView is null || this.args is null)
				return;

			byte[]? croppedResult = await this.ImageCropperView.PerformCropAsync();

			// If a TaskCompletionSource was provided, set the result
			this.args.CompletionSource?.SetResult(croppedResult);

			// Then pop navigation. If using Shell:
			await this.GoBack();
		}

		/// <summary>
		/// Command to cancel cropping and return a null result.
		/// </summary>
		[RelayCommand]
		private async Task CancelAsync()
		{
			this.args?.CompletionSource?.SetResult(null);

			await this.GoBack();
		}
	}
}
