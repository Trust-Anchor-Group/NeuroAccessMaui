using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Services.Nfc;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Default implementation of <see cref="INfcScanHistoryService"/>.
	/// </summary>
	[Singleton]
	public sealed class NfcScanHistoryService : INfcScanHistoryService
	{
		private const int DefaultRetentionCount = 200;
		private readonly SemaphoreSlim gate = new(1, 1);
		private readonly INfcTagSnapshotService nfcTagSnapshotService;

		private string? lastTagId;
		private DateTimeOffset? lastDetectedAtUtc;
		private string? lastRecordObjectId;

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcScanHistoryService"/> class.
		/// </summary>
		public NfcScanHistoryService()
		{
			this.nfcTagSnapshotService = ServiceRef.Provider.GetRequiredService<INfcTagSnapshotService>();
			this.nfcTagSnapshotService.SnapshotChanged += this.NfcTagSnapshotService_SnapshotChanged;
		}

		/// <inheritdoc />
		public event EventHandler? HistoryChanged;

		/// <inheritdoc />
		public async Task<IReadOnlyList<NfcScanHistoryRecord>> GetRecentAsync(int MaxCount, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			IEnumerable<NfcScanHistoryRecord> Records = await Database.Find<NfcScanHistoryRecord>();
			List<NfcScanHistoryRecord> Ordered = Records
				.OrderByDescending(r => r.DetectedAtUtc)
				.Take(Math.Max(0, MaxCount))
				.ToList();

			return Ordered;
		}

		/// <inheritdoc />
		public async Task ClearAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			await this.gate.WaitAsync(CancellationToken);
			try
			{
				IEnumerable<NfcScanHistoryRecord> Records = await Database.Find<NfcScanHistoryRecord>();
				await Database.StartBulk();
				try
				{
					foreach (NfcScanHistoryRecord Record in Records)
						await Database.Delete(Record);
				}
				finally
				{
					await Database.EndBulk();
				}

				await Database.Provider.Flush();
			}
			finally
			{
				this.gate.Release();
			}

			this.HistoryChanged?.Invoke(this, EventArgs.Empty);
		}

		private void NfcTagSnapshotService_SnapshotChanged(object? Sender, EventArgs e)
		{
			_ = this.UpsertFromSnapshotAsync();
		}

		private async Task UpsertFromSnapshotAsync()
		{
			NfcTagSnapshot? Snapshot = this.nfcTagSnapshotService.LastSnapshot;
			if (Snapshot is null)
				return;

			await this.gate.WaitAsync();
			try
			{
				bool IsNew =
					!string.Equals(this.lastTagId, Snapshot.TagIdHex, StringComparison.OrdinalIgnoreCase) ||
					this.lastDetectedAtUtc is null ||
					this.lastDetectedAtUtc.Value != Snapshot.DetectedAtUtc;

				if (IsNew)
				{
					NfcScanHistoryRecord Record = new()
					{
						DetectedAtUtc = Snapshot.DetectedAtUtc.UtcDateTime,
						TagId = Snapshot.TagIdHex,
						TagType = Snapshot.TagType,
						InterfacesSummary = Snapshot.InterfacesSummary,
						NdefSummary = Snapshot.NdefSummary,
						ExtractedUri = Snapshot.ExtractedUri,
						NdefRecords = Snapshot.NdefRecords is null ? null : Snapshot.NdefRecords.ToArray()
					};

					await Database.Insert(Record);
					await Database.Provider.Flush();

					this.lastTagId = Snapshot.TagIdHex;
					this.lastDetectedAtUtc = Snapshot.DetectedAtUtc;
					this.lastRecordObjectId = Record.ObjectId;

					await this.PruneAsync(DefaultRetentionCount);
				}
				else
				{
					NfcScanHistoryRecord? Record = await this.TryLoadLastRecordAsync();
					if (Record is null)
						return;

					Record.TagType = Snapshot.TagType;
					Record.InterfacesSummary = Snapshot.InterfacesSummary;
					Record.NdefSummary = Snapshot.NdefSummary;
					Record.ExtractedUri = Snapshot.ExtractedUri;
					Record.NdefRecords = Snapshot.NdefRecords is null ? null : Snapshot.NdefRecords.ToArray();

					await Database.Update(Record);
					await Database.Provider.Flush();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.gate.Release();
			}

			this.HistoryChanged?.Invoke(this, EventArgs.Empty);
		}

		private async Task<NfcScanHistoryRecord?> TryLoadLastRecordAsync()
		{
			if (string.IsNullOrEmpty(this.lastRecordObjectId))
				return null;

			try
			{
				return await Database.FindFirstIgnoreRest<NfcScanHistoryRecord>(new FilterFieldEqualTo(nameof(NfcScanHistoryRecord.ObjectId), this.lastRecordObjectId));
			}
			catch
			{
				return null;
			}
		}

		private async Task PruneAsync(int RetentionCount)
		{
			IEnumerable<NfcScanHistoryRecord> Records = await Database.Find<NfcScanHistoryRecord>();
			List<NfcScanHistoryRecord> Ordered = Records.OrderByDescending(r => r.DetectedAtUtc).ToList();
			if (Ordered.Count <= RetentionCount)
				return;

			IEnumerable<NfcScanHistoryRecord> ToDelete = Ordered.Skip(RetentionCount);

			await Database.StartBulk();
			try
			{
				foreach (NfcScanHistoryRecord Record in ToDelete)
				{
					try
					{
						await Database.Delete(Record);
					}
					catch
					{
						// Ignore (concurrent delete or already removed).
					}
				}
			}
			finally
			{
				await Database.EndBulk();
			}

			await Database.Provider.Flush();
		}
	}
}
