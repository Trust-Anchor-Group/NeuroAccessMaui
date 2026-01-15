using System;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Default implementation of <see cref="INfcTagSnapshotService"/>.
	/// </summary>
	[Singleton]
	public sealed class NfcTagSnapshotService : INfcTagSnapshotService
	{
		private readonly object gate = new();
		private NfcTagSnapshot? lastSnapshot;

		/// <inheritdoc />
		public event EventHandler? SnapshotChanged;

		/// <inheritdoc />
		public NfcTagSnapshot? LastSnapshot
		{
			get
			{
				lock (this.gate)
				{
					return this.lastSnapshot;
				}
			}
		}

		/// <inheritdoc />
		public void Publish(NfcTagSnapshot Snapshot)
		{
			lock (this.gate)
			{
				this.lastSnapshot = Snapshot;
			}

			this.SnapshotChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <inheritdoc />
		public void UpdateNdef(string TagIdHex, string? NdefSummary, string? ExtractedUri)
		{
			bool Changed = false;
			lock (this.gate)
			{
				if (this.lastSnapshot is null)
					return;

				if (!string.Equals(this.lastSnapshot.TagIdHex, TagIdHex, StringComparison.OrdinalIgnoreCase))
					return;

				this.lastSnapshot = this.lastSnapshot.WithNdef(NdefSummary, ExtractedUri);
				Changed = true;
			}

			if (Changed)
				this.SnapshotChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <inheritdoc />
		public void Clear()
		{
			lock (this.gate)
			{
				this.lastSnapshot = null;
			}

			this.SnapshotChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
