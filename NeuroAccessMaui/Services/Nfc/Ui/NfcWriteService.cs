using System;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Default implementation of <see cref="INfcWriteService"/>.
	/// </summary>
	[Singleton]
	public sealed class NfcWriteService : INfcWriteService
	{
		private readonly object gate = new();
		private TaskCompletionSource<bool>? pendingWrite;
		private object[]? pendingItems;

		/// <inheritdoc />
		public bool HasPendingWrite
		{
			get
			{
				lock (this.gate)
				{
					return this.pendingWrite is not null;
				}
			}
		}

		/// <inheritdoc />
		public Task<bool> WriteUriAsync(string Uri, CancellationToken CancellationToken)
		{
			if (!System.Uri.TryCreate(Uri?.Trim(), UriKind.Absolute, out System.Uri? Parsed))
				return Task.FromResult(false);

			TaskCompletionSource<bool> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

			lock (this.gate)
			{
				this.pendingWrite?.TrySetResult(false);
				this.pendingWrite = TaskSource;
				this.pendingItems = [Parsed];
			}

			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() =>
				{
					lock (this.gate)
					{
						if (this.pendingWrite == TaskSource)
						{
							this.pendingWrite = null;
							this.pendingItems = null;
							TaskSource.TrySetResult(false);
						}
					}
				});
			}

			return TaskSource.Task;
		}

		/// <inheritdoc />
		public async Task<bool> TryHandleWriteAsync(NfcService.WriteItems WriteCallback)
		{
			TaskCompletionSource<bool>? TaskSource;
			object[]? Items;

			lock (this.gate)
			{
				TaskSource = this.pendingWrite;
				Items = this.pendingItems;
				if (TaskSource is null || Items is null)
					return false;

				this.pendingWrite = null;
				this.pendingItems = null;
			}

			try
			{
				bool Ok = await WriteCallback(Items);
				TaskSource.TrySetResult(Ok);
				return true;
			}
			catch
			{
				TaskSource.TrySetResult(false);
				return true;
			}
		}

		/// <inheritdoc />
		public void CancelPendingWrite()
		{
			lock (this.gate)
			{
				this.pendingWrite?.TrySetResult(false);
				this.pendingWrite = null;
				this.pendingItems = null;
			}
		}
	}
}
