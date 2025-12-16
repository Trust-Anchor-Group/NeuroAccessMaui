using System;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Disposable handle used to remove a notification filter.
	/// </summary>
	public sealed class NotificationFilterHandle : IDisposable
	{
		private readonly Action onDispose;
		private bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationFilterHandle"/> class.
		/// </summary>
		/// <param name="OnDispose">Callback to remove the filter.</param>
		public NotificationFilterHandle(Action OnDispose)
		{
			this.onDispose = OnDispose;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.disposed)
				return;

			this.disposed = true;
			this.onDispose();
		}
	}
}
