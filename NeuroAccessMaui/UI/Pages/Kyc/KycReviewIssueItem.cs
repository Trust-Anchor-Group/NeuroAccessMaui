namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Represents a detail that the reviewer asked the user to fix after an identity application was rejected.
	/// </summary>
	/// <remarks>
	/// When <see cref="Mapping"/> matches a field in the form, the summary lets the user open that field directly.
	/// </remarks>
	public sealed class KycReviewIssueItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycReviewIssueItem"/> class.
		/// </summary>
		/// <param name="Title">Issue title, usually the label of the affected detail.</param>
		/// <param name="Description">Issue description, usually the reviewer's reason. May be empty.</param>
		/// <param name="Mapping">Optional KYC mapping of the affected field.</param>
		/// <param name="CanOpen">Whether the affected field can be opened for editing.</param>
		public KycReviewIssueItem(string Title, string Description, string? Mapping, bool CanOpen)
		{
			this.Title = Title;
			this.Description = Description;
			this.Mapping = Mapping;
			this.CanOpen = CanOpen && !string.IsNullOrWhiteSpace(Mapping);
		}

		/// <summary>
		/// Gets the issue title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the issue description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets a value indicating whether the issue has a description to show.
		/// </summary>
		public bool HasDescription => !string.IsNullOrWhiteSpace(this.Description);

		/// <summary>
		/// Gets the optional KYC mapping of the affected field.
		/// </summary>
		public string? Mapping { get; }

		/// <summary>
		/// Gets a value indicating whether tapping the issue opens the affected field.
		/// </summary>
		public bool CanOpen { get; }
	}
}
