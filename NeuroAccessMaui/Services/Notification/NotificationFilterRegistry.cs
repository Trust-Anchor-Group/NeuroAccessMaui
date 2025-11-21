using System;
using System.Collections.Generic;
using System.Threading;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Manages runtime notification filters (ignore rules).
	/// </summary>
	public sealed class NotificationFilterRegistry : INotificationFilterRegistry
	{
		private readonly List<Func<NotificationIntent, bool>> filters = [];

		/// <inheritdoc/>
		public IDisposable AddFilter(Func<NotificationIntent, bool> Predicate)
		{
			if (Predicate is null)
				throw new ArgumentNullException(nameof(Predicate));

			lock (this.filters)
			{
				this.filters.Add(Predicate);
			}

			return new NotificationFilterHandle(() =>
			{
				lock (this.filters)
				{
					this.filters.Remove(Predicate);
				}
			});
		}

		/// <inheritdoc/>
		public bool ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken)
		{
			List<Func<NotificationIntent, bool>> snapshot;
			lock (this.filters)
			{
				if (this.filters.Count == 0)
					return false;
				snapshot = new List<Func<NotificationIntent, bool>>(this.filters);
			}

			foreach (Func<NotificationIntent, bool> filter in snapshot)
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (filter(Intent))
					return true;
			}

			return false;
		}
	}
}
