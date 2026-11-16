using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Images
{
	/// <summary>
	/// Shows an image, such as a chart drawn by the server, on the whole screen, where it can be zoomed, moved and shared.
	/// </summary>
	public class ImageViewerPopup : BasicPopup
	{
		private readonly ImageSource source;
		private readonly string title;

		/// <summary>
		/// Creates a viewer for an image.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">A title shown above the image, if any.</param>
		public ImageViewerPopup(ImageSource Source, string? Title)
		{
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 1;
			this.PopupMargin = new Thickness(0);
			this.CardPadding = new Thickness(12, 12, 12, 20);

			this.source = Source;
			this.title = string.IsNullOrEmpty(Title) ? ServiceRef.Localizer[nameof(AppResources.HistoryValueChart)] : Title;

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star }
				},
				RowSpacing = 12
			};

			// The viewer takes the whole screen, apart from a little room for the system bars.
			DisplayInfo Display = DeviceDisplay.Current.MainDisplayInfo;
			if (Display.Density > 0)
				Layout.HeightRequest = Display.Height / Display.Density * 0.9;

			Layout.Add(this.CreateHeader(), 0, 0);
			Layout.Add(new ZoomImageView { Source = Source }, 0, 1);

			this.CardContent = Layout;
		}

		/// <summary>
		/// Shows an image in the viewer.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">A title shown above the image, if any.</param>
		public static Task ShowAsync(ImageSource Source, string? Title)
		{
			return ServiceRef.PopupService.PushAsync(new ImageViewerPopup(Source, Title));
		}

		/// <summary>
		/// Creates the top bar: close, the title with where the image is from, and share.
		/// </summary>
		/// <returns>The top bar.</returns>
		private Grid CreateHeader()
		{
			Label TitleLabel = new()
			{
				Text = this.title,
				FontFamily = "SpaceGroteskSemiBold",
				FontSize = 17,
				LineBreakMode = LineBreakMode.TailTruncation,
				MaxLines = 1
			};
			TitleLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");

			Label SourceLabel = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomFromServer), DateTime.Now.ToString("t", CultureInfo.CurrentCulture)],
				FontSize = 12
			};
			SourceLabel.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				ColumnSpacing = 12
			};
			Header.Add(ZoomImageView.CreateRoundButton(Geometries.CancelPath, ServiceRef.Localizer[nameof(AppResources.Close)],
				() => _ = ServiceRef.PopupService.PopAsync()), 0, 0);
			Header.Add(new VerticalStackLayout
			{
				VerticalOptions = LayoutOptions.Center,
				Children = { TitleLabel, SourceLabel }
			}, 1, 0);

			if (ImageShare.GetFile(this.source) is not null)
			{
				Header.Add(ZoomImageView.CreateRoundButton(Geometries.ShareIconPath, ServiceRef.Localizer[nameof(AppResources.Share)],
					() => _ = ImageShare.ShareAsync(this.source, this.title)), 2, 0);
			}

			return Header;
		}
	}
}
