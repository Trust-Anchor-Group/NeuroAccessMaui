using System.Globalization;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Combines MRZ reads from consecutive camera frames and decides when a read is trustworthy enough to accept.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A read that passes every check digit, including the composite check, without any repairs is accepted at once.
	/// A read that only validates after check-digit repairs is accepted once a read from a later frame agrees on the
	/// document number, date of birth and expiry date, since those fields derive the chip access key.
	/// </para>
	/// <para>
	/// Reads that do not validate are kept per layout and combined by a confidence-weighted vote on every character
	/// position. The voted text is decoded again and accepted under the same rules, so errors that differ from frame to
	/// frame cancel out. The state is cleared once the MRZ has been out of view for a while.
	/// </para>
	/// </remarks>
	public sealed class MrzReadConsensus
	{
		private const int maximumReadsPerFormat = 8;
		private const int minimumVotingReads = 2;
		private const float minimumVoteWeight = 0.05f;
		private static readonly TimeSpan trackLossResetInterval = TimeSpan.FromMilliseconds(1500);

		private readonly object syncRoot = new object();
		private readonly Dictionary<MrzDocumentFormat, List<MrzFrameRead>> readsByFormat = new Dictionary<MrzDocumentFormat, List<MrzFrameRead>>();
		private PendingRead? pendingRead;
		private DateTimeOffset lastDetectionTimestamp = DateTimeOffset.MinValue;
		private long frameSequence;
		private int acceptedCount;
		private string lastAcceptanceReason = string.Empty;

		/// <summary>
		/// Adds the analysis of one frame and reports whether an MRZ read can now be accepted.
		/// </summary>
		/// <param name="Analysis">The frame analysis.</param>
		/// <param name="Timestamp">The capture time of the frame.</param>
		/// <param name="Result">The accepted scan result, when the method returns <see langword="true"/>.</param>
		/// <returns><see langword="true"/> if a read was accepted; otherwise, <see langword="false"/>.</returns>
		public bool TryAccept(MrzPreviewAnalysisResult Analysis, DateTimeOffset Timestamp, out OcrScanResult? Result)
		{
			ArgumentNullException.ThrowIfNull(Analysis);

			lock (this.syncRoot)
			{
				Result = null;
				this.frameSequence++;

				if (Analysis.MrzQuad is null)
				{
					if (this.lastDetectionTimestamp != DateTimeOffset.MinValue
						&& Timestamp - this.lastDetectionTimestamp > trackLossResetInterval)
					{
						this.ClearState();
					}

					return false;
				}

				this.lastDetectionTimestamp = Timestamp;
				MrzFrameRead? Read = Analysis.Read;
				if (Read is null || Read.Lines.Count == 0)
					return false;

				MrzFrameRead? Accepted = this.Consider(Read, out string Reason);
				int VotingReadCount = 0;
				if (Accepted is null)
				{
					// A validated read that needed repairs carries its repaired text. Letting it vote would reproduce that
					// text as a "clean" voted read and skip the confirmation from a later frame, so only failed reads vote.
					if (!Read.IsStrictSuccess)
						this.AddVote(Read);

					VotingReadCount = this.readsByFormat.TryGetValue(Read.Format, out List<MrzFrameRead>? FormatReads) ? FormatReads.Count : 0;
					MrzFrameRead? Voted = this.CreateVotedRead(Read.Format);
					if (Voted is not null)
					{
						Accepted = this.Consider(Voted, out Reason);
						if (Accepted is not null)
							Reason = "Voted" + Reason;
					}
				}

				if (Accepted is null)
					return false;

				Dictionary<string, string> Metadata = new Dictionary<string, string>(Analysis.Metadata);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, Accepted.Metadata);
				Metadata["MrzConsensusReason"] = Reason;
				Metadata["MrzConsensusVotingReadCount"] = VotingReadCount.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzConsensusRepairCount"] = Accepted.RepairCount.ToString(CultureInfo.InvariantCulture);
				Result = MrzReadDecoder.CreateSuccessResult(Accepted, OcrScanTargetKind.Mrz, OcrScanSourceKind.PreviewFrame, Metadata);

				this.acceptedCount++;
				this.lastAcceptanceReason = Reason;
				this.ClearState();
				return true;
			}
		}

		/// <summary>
		/// Discards all collected reads, for example after an accepted read was rejected by the caller.
		/// </summary>
		public void Reset()
		{
			lock (this.syncRoot)
			{
				this.ClearState();
			}
		}

		/// <summary>
		/// Creates a diagnostic snapshot of the collected reads. The snapshot contains counts only, never MRZ content.
		/// </summary>
		/// <returns>A dictionary of diagnostic values.</returns>
		public IReadOnlyDictionary<string, string> CreateDiagnostics()
		{
			lock (this.syncRoot)
			{
				Dictionary<string, string> Diagnostics = new Dictionary<string, string>
				{
					["ConsensusFrameCount"] = this.frameSequence.ToString(CultureInfo.InvariantCulture),
					["ConsensusAcceptedCount"] = this.acceptedCount.ToString(CultureInfo.InvariantCulture),
					["ConsensusLastAcceptanceReason"] = this.lastAcceptanceReason,
					["ConsensusHasPendingRead"] = (this.pendingRead is not null).ToString()
				};

				foreach (KeyValuePair<MrzDocumentFormat, List<MrzFrameRead>> Pair in this.readsByFormat)
					Diagnostics["ConsensusReadCount" + Pair.Key.ToString()] = Pair.Value.Count.ToString(CultureInfo.InvariantCulture);

				return Diagnostics;
			}
		}

		/// <summary>
		/// Applies the acceptance rules to a single candidate read.
		/// </summary>
		private MrzFrameRead? Consider(MrzFrameRead Read, out string Reason)
		{
			Reason = string.Empty;
			if (!Read.IsStrictSuccess || Read.Document is null)
				return null;

			if (Read.RepairCount == 0)
			{
				Reason = "CleanRead";
				return Read;
			}

			string Key = CreateConfirmationKey(Read);
			if (this.pendingRead is not null
				&& this.pendingRead.Sequence < this.frameSequence
				&& string.Equals(this.pendingRead.Key, Key, StringComparison.Ordinal))
			{
				Reason = "ConfirmedRepairedRead";
				return Read.RepairCount <= this.pendingRead.Read.RepairCount ? Read : this.pendingRead.Read;
			}

			if (this.pendingRead is null || this.pendingRead.Sequence < this.frameSequence)
				this.pendingRead = new PendingRead(Read, Key, this.frameSequence);

			return null;
		}

		private int AddVote(MrzFrameRead Read)
		{
			if (!this.readsByFormat.TryGetValue(Read.Format, out List<MrzFrameRead>? Reads))
			{
				Reads = new List<MrzFrameRead>();
				this.readsByFormat[Read.Format] = Reads;
			}

			Reads.Add(Read);
			if (Reads.Count > maximumReadsPerFormat)
				Reads.RemoveAt(0);

			return Reads.Count;
		}

		/// <summary>
		/// Votes on every character position across the collected reads of one layout, weighting each read by its
		/// recognition confidence, and decodes the result.
		/// </summary>
		private MrzFrameRead? CreateVotedRead(MrzDocumentFormat Format)
		{
			if (!this.readsByFormat.TryGetValue(Format, out List<MrzFrameRead>? Reads) || Reads.Count < minimumVotingReads)
				return null;

			IReadOnlyList<string> Reference = Reads[^1].Lines;
			List<MrzFrameRead> Compatible = Reads
				.Where(Read => HasSameShape(Read.Lines, Reference))
				.ToList();
			if (Compatible.Count < minimumVotingReads)
				return null;

			List<string> VotedLines = new List<string>(Reference.Count);
			Dictionary<char, float> Weights = new Dictionary<char, float>();
			for (int LineIndex = 0; LineIndex < Reference.Count; LineIndex++)
			{
				char[] Line = new char[Reference[LineIndex].Length];
				for (int Position = 0; Position < Line.Length; Position++)
				{
					Weights.Clear();
					foreach (MrzFrameRead Read in Compatible)
					{
						char Character = Read.Lines[LineIndex][Position];
						Weights.TryGetValue(Character, out float Weight);
						Weights[Character] = Weight + Math.Max(Read.RecognitionConfidence, minimumVoteWeight);
					}

					Line[Position] = Weights.MaxBy(static Pair => Pair.Value).Key;
				}

				VotedLines.Add(new string(Line));
			}

			float Confidence = Compatible.Average(static Read => Read.RecognitionConfidence);
			return MrzReadDecoder.DecodeLines(VotedLines, Format, Confidence);
		}

		private static bool HasSameShape(IReadOnlyList<string> Lines, IReadOnlyList<string> Reference)
		{
			if (Lines.Count != Reference.Count)
				return false;

			for (int Index = 0; Index < Lines.Count; Index++)
			{
				if (Lines[Index].Length != Reference[Index].Length)
					return false;
			}

			return true;
		}

		private static string CreateConfirmationKey(MrzFrameRead Read)
		{
			return string.Join(
				"|",
				Read.Format.ToString(),
				Read.Document?.DocumentNumber ?? string.Empty,
				Read.Document?.DateOfBirth ?? string.Empty,
				Read.Document?.ExpiryDate ?? string.Empty);
		}

		private void ClearState()
		{
			this.readsByFormat.Clear();
			this.pendingRead = null;
			this.lastDetectionTimestamp = DateTimeOffset.MinValue;
		}

		private sealed record PendingRead(MrzFrameRead Read, string Key, long Sequence);
	}
}
