using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Represents one read-only status row in the KYC application status page.
	/// </summary>
	public sealed class KycApplicationStatusRow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationStatusRow"/> class.
		/// </summary>
		/// <param name="Title">Row title.</param>
		/// <param name="Description">Row description.</param>
		/// <param name="State">Visual state for the row.</param>
		public KycApplicationStatusRow(string Title, string Description, IdentityFlowPhaseState State)
		{
			this.Title = Title;
			this.Description = Description;
			this.State = State;
		}

		/// <summary>
		/// Gets the row title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the row description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets the visual state for the row.
		/// </summary>
		public IdentityFlowPhaseState State { get; }

		/// <summary>
		/// Gets a value indicating whether the row is complete.
		/// </summary>
		public bool IsComplete => this.State == IdentityFlowPhaseState.Complete;

		/// <summary>
		/// Gets a value indicating whether the row is current.
		/// </summary>
		public bool IsCurrent => this.State == IdentityFlowPhaseState.Current;
	}
}
