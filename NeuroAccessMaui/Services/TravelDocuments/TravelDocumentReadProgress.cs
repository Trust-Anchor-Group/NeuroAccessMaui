using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Converts low-level chip readout states into a monotonic, stage-based progress value for display.
	/// </summary>
	/// <remarks>
	/// The chip protocol does not reveal the total amount of data up front, so each stage fills
	/// asymptotically with the number of chip events received. Progress therefore moves only when the
	/// chip responds, and never reaches completion before <see cref="Complete"/> is called.
	/// Instances are not thread-safe and should be used from a single thread.
	/// </remarks>
	public sealed class TravelDocumentReadProgress
	{
		/// <summary>
		/// The number of display stages, from detection to verification.
		/// </summary>
		public const int StageCount = 4;

		private const double maxStageFill = 0.94;
		private const double secureEventScale = 8;
		private const double readEventScale = 45;
		private const double verifyStartFill = 0.5;

		private int stageEvents;
		private bool authenticationStarted;
		private double progress;

		/// <summary>
		/// Gets the current display stage.
		/// </summary>
		public TravelDocumentReadStage Stage { get; private set; } = TravelDocumentReadStage.Detect;

		/// <summary>
		/// Gets the overall progress, from 0 (waiting for the chip) to 1 (readout complete).
		/// </summary>
		public double Progress => this.progress;

		/// <summary>
		/// Gets a value indicating whether the chip has been detected during the current attempt.
		/// </summary>
		public bool IsDetected { get; private set; }

		/// <summary>
		/// Clears all progress so a new readout attempt starts from the beginning.
		/// </summary>
		public void Reset()
		{
			this.Stage = TravelDocumentReadStage.Detect;
			this.IsDetected = false;
			this.authenticationStarted = false;
			this.stageEvents = 0;
			this.progress = 0;
		}

		/// <summary>
		/// Applies a chip readout state reported by the travel-document client.
		/// </summary>
		/// <param name="State">The reported chip state.</param>
		/// <returns>True if this state is the first sign of the chip during the current attempt.</returns>
		public bool Apply(TravelDocumentsState State)
		{
			bool FirstDetection = !this.IsDetected;
			if (FirstDetection)
			{
				// Any chip state implies the chip was found, even if the detection event itself was missed.
				this.IsDetected = true;
				this.EnterStage(TravelDocumentReadStage.Secure);
			}

			switch (State)
			{
				case TravelDocumentsState.Detected:
					break;

				case TravelDocumentsState.FindingCipher:
				case TravelDocumentsState.SelectingCipher:
				case TravelDocumentsState.GettingNonce:
				case TravelDocumentsState.GettingPublicKey:
				case TravelDocumentsState.GettingEphemeralPublicKey:
				case TravelDocumentsState.GettingVerificationToken:
				case TravelDocumentsState.GettingChallenge:
				case TravelDocumentsState.RespondingToChallenge:
					this.authenticationStarted = true;
					this.CountEvent(TravelDocumentReadStage.Secure, secureEventScale);
					break;

				case TravelDocumentsState.ValidatingCertificate:
					if (this.authenticationStarted)
						this.EnterStage(TravelDocumentReadStage.Read);
					break;

				case TravelDocumentsState.Idle:
					this.EnterStage(TravelDocumentReadStage.Verify);
					this.SetStageFill(verifyStartFill);
					break;

				default:
					// File selection and binary reads happen both before authentication (EF.CardAccess)
					// and after it (document data), so authentication decides which stage they belong to.
					if (this.authenticationStarted)
						this.CountEvent(TravelDocumentReadStage.Read, readEventScale);
					else
						this.CountEvent(TravelDocumentReadStage.Secure, secureEventScale);
					break;
			}

			return FirstDetection;
		}

		/// <summary>
		/// Marks the readout as complete after it has been saved.
		/// </summary>
		public void Complete()
		{
			this.IsDetected = true;
			this.Stage = TravelDocumentReadStage.Verify;
			this.progress = 1;
		}

		private void CountEvent(TravelDocumentReadStage EventStage, double Scale)
		{
			this.EnterStage(EventStage);
			if (EventStage != this.Stage)
				return;

			this.stageEvents++;
			this.SetStageFill(maxStageFill * (1 - Math.Exp(-this.stageEvents / Scale)));
		}

		private void EnterStage(TravelDocumentReadStage NewStage)
		{
			if (NewStage <= this.Stage)
				return;

			this.Stage = NewStage;
			this.stageEvents = 0;
			this.SetStageFill(0);
		}

		private void SetStageFill(double Fill)
		{
			double Value = ((int)this.Stage + Math.Clamp(Fill, 0, maxStageFill)) / StageCount;
			this.progress = Math.Max(this.progress, Value);
		}
	}
}
