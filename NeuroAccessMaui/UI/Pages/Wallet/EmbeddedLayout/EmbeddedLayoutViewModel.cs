using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroFeatures;
using SkiaSharp;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	/// <summary>
	/// View model for displaying a rendered embedded layout associated with a token.
	/// </summary>
	public partial class EmbeddedLayoutViewModel : BaseViewModel
	{
		private readonly EmbeddedLayoutNavigationArgs? navigationArguments;

		/// <summary>
		/// Gets or sets the rendered embedded layout image.
		/// </summary>
		[ObservableProperty]
		private ImageSource? renderedLayout;

		/// <summary>
		/// Gets or sets a value indicating whether the embedded layout is available.
		/// </summary>
		[ObservableProperty]
		private bool loaded;

		/// <summary>
		/// Gets or sets a value indicating whether the embedded layout is rendering.
		/// </summary>
		[ObservableProperty]
		private bool isLoading;

		/// <summary>
		/// Gets or sets a value indicating whether the embedded layout is unavailable.
		/// </summary>
		[ObservableProperty]
		private bool hasError;

		/// <summary>
		/// Gets or sets the localized embedded-layout failure explanation.
		/// </summary>
		[ObservableProperty]
		private string errorMessage = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments, if any.</param>
		public EmbeddedLayoutViewModel(EmbeddedLayoutNavigationArgs? Args)
		{
			this.navigationArguments = Args;
		}

		/// <summary>
		/// Initializes the view model asynchronously.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			Token? Token = this.navigationArguments?.Token;
			if (Token is null || !Token.HasEmbeddedLayout)
			{
				await MainThread.InvokeOnMainThreadAsync(this.ShowUnavailable);
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsLoading = true;
				this.HasError = false;
				this.ErrorMessage = string.Empty;
			});

			try
			{
				using SKImage Image = await Token.RenderEmbeddedLayout();
				using SKData Data = Image.Encode(SKEncodedImageFormat.Png, 100);
				byte[] Buffer = Data.ToArray();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.RenderedLayout = ImageSource.FromStream(
						() => new MemoryStream(Buffer, false));
					this.Loaded = true;
				});
			}
			catch (Exception Ex)
			{
				// Embedded definitions are remotely supplied and may contain sensitive token data.
				ServiceRef.LogService.LogWarning(
					"Embedded token layout rendering failed.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				await MainThread.InvokeOnMainThreadAsync(this.ShowUnavailable);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		private void ShowUnavailable()
		{
			this.Loaded = false;
			this.HasError = true;
			this.ErrorMessage =
				ServiceRef.Localizer[nameof(AppResources.EmbeddedLayoutUnavailable)];
		}
	}
}
