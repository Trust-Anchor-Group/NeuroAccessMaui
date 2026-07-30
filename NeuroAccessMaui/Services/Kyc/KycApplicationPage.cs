namespace NeuroAccessMaui.Services.Kyc
{
	using System.Collections.Generic;
	using Waher.Networking.XMPP.ResultSetManagement;

	/// <summary>
	/// Represents a page of KYC application templates along with pagination metadata.
	/// </summary>
	public sealed class KycApplicationPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationPage"/> class.
		/// </summary>
		/// <param name="Templates">The templates included on this page.</param>
		/// <param name="ResultPage">The optional XEP-0059 pagination metadata.</param>
		/// <param name="UsedFallback">If the bundled fallback template was used.</param>
		public KycApplicationPage(IReadOnlyList<KycApplicationTemplate> Templates, ResultPage? ResultPage, bool UsedFallback)
		{
			this.Templates = Templates;
			this.ResultPage = ResultPage;
			this.UsedFallback = UsedFallback;
		}

		/// <summary>
		/// Gets the application templates on this page.
		/// </summary>
		public IReadOnlyList<KycApplicationTemplate> Templates { get; }

		/// <summary>
		/// Gets the underlying XEP-0059 pagination result metadata, if provided by the server.
		/// </summary>
		public ResultPage? ResultPage { get; }

		/// <summary>
		/// Gets a value indicating if the bundled fallback template was returned instead of remote data.
		/// </summary>
		public bool UsedFallback { get; }

		/// <summary>
		/// Gets the item identifier to use in an "after" query when requesting the next page.
		/// </summary>
		public string? NextAfter => this.ResultPage?.Last;

		/// <summary>
		/// Gets the item identifier to use in a "before" query when requesting the previous page.
		/// </summary>
		public string? PreviousBefore => this.ResultPage?.First;

		/// <summary>
		/// Gets the total item count if provided by the server.
		/// </summary>
		public int? TotalCount => this.ResultPage?.Count;

		/// <summary>
		/// Gets a value indicating whether more pages are likely available after the current one.
		/// </summary>
		public bool HasMoreAfter => !string.IsNullOrEmpty(this.ResultPage?.Last);
	}
}
