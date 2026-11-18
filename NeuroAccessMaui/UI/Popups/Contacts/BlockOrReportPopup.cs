using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// A sheet for blocking someone, or reporting them to the server as well.
	/// </summary>
	public sealed class BlockOrReportPopup : ContactSheetPopup
	{
		private const string blockIcon = "M12 3a9 9 0 1 1 0 18a9 9 0 1 1 0-18z M5.6 5.6l12.8 12.8";
		private const string reportIcon = "M5 21V4 M5 4h11l-2 4 2 4H5";

		private readonly TaskCompletionSource<BlockChoice?> result = new();

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Name">The person's name.</param>
		public BlockOrReportPopup(string Name)
		{
			this.Add(this.CreateHeader(ServiceRef.Localizer[nameof(AppResources.ContactBlockOrReportTitle), Name], null));

			this.Add(CreateCard(
				this.CreateChoice(blockIcon, ServiceRef.Localizer[nameof(AppResources.ContactBlockName), Name],
					ServiceRef.Localizer[nameof(AppResources.ContactBlockHint)], BlockChoice.Block),
				this.CreateChoice(reportIcon, ServiceRef.Localizer[nameof(AppResources.ContactReportSpam)],
					ServiceRef.Localizer[nameof(AppResources.ContactReportHint)], BlockChoice.ReportSpam),
				this.CreateChoice(reportIcon, ServiceRef.Localizer[nameof(AppResources.ContactReportAbuse)],
					ServiceRef.Localizer[nameof(AppResources.ContactReportHint)], BlockChoice.ReportAbuse),
				this.CreateChoice(reportIcon, ServiceRef.Localizer[nameof(AppResources.ContactReportOther)],
					ServiceRef.Localizer[nameof(AppResources.ContactReportHint)], BlockChoice.ReportOther)));
		}

		/// <summary>
		/// Shows the sheet, and waits for the user's choice.
		/// </summary>
		/// <param name="Name">The person's name.</param>
		/// <returns>What the user chose, or null if the sheet was closed.</returns>
		public static async Task<BlockChoice?> ShowAsync(string Name)
		{
			BlockOrReportPopup Sheet = new(Name);
			await ShowSheetAsync(Sheet);
			return await Sheet.result.Task;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.result.TrySetResult(null);
			return base.OnDisappearingAsync();
		}

		private View CreateChoice(string IconData, string Title, string Subtitle, BlockChoice Choice)
		{
			return this.CreateRow(IconData, Title, Subtitle, true, null, () => this.result.TrySetResult(Choice));
		}
	}
}
