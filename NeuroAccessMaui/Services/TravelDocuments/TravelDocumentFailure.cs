namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Describes why a travel-document chip read failed and what the user should do next.
	/// </summary>
	public sealed class TravelDocumentFailure
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentFailure"/> class.
		/// </summary>
		/// <param name="FailureCode">The specific failure condition.</param>
		/// <param name="RecoveryAction">The preferred next action for the user.</param>
		public TravelDocumentFailure(TravelDocumentFailureCode FailureCode, TravelDocumentRecoveryAction RecoveryAction)
		{
			this.FailureCode = FailureCode;
			this.RecoveryAction = RecoveryAction;
		}

		/// <summary>
		/// Gets the specific failure condition.
		/// </summary>
		public TravelDocumentFailureCode FailureCode { get; }

		/// <summary>
		/// Gets the preferred next action for the user.
		/// </summary>
		public TravelDocumentRecoveryAction RecoveryAction { get; }

		/// <summary>
		/// Gets a value indicating whether the failure is a cancellation by the user rather than an error.
		/// </summary>
		public bool IsCancellation => this.FailureCode == TravelDocumentFailureCode.NfcSessionCancelled;
	}
}
