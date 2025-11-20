using System;
using NeuroAccessMaui.Services.Identity;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Event arguments emitted when a KYC application review changes.
	/// </summary>
	public sealed class ApplicationReviewEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationReviewEventArgs"/> class.
		/// </summary>
		/// <param name="reference">Reference associated with the review.</param>
		/// <param name="review">Review payload, if any.</param>
		public ApplicationReviewEventArgs(KycReference reference, ApplicationReview? review)
		{
			this.Reference = reference ?? throw new ArgumentNullException(nameof(reference));
			this.Review = review;
		}

		/// <summary>
		/// Gets the reference associated with the update.
		/// </summary>
		public KycReference Reference { get; }

		/// <summary>
		/// Gets the review payload.
		/// </summary>
		public ApplicationReview? Review { get; }
	}
}
