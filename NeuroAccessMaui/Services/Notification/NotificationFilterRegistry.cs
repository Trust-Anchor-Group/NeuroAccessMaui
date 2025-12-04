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
		private readonly List<Func<NotificationIntent, NotificationFilterDecision>> filters = [];

		/// <inheritdoc/>
		public IDisposable AddFilter(Func<NotificationIntent, NotificationFilterDecision> Predicate)
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
		public NotificationFilterDecision ShouldIgnore(NotificationIntent Intent, bool FromUserInteraction, CancellationToken CancellationToken)
		{
			List<Func<NotificationIntent, NotificationFilterDecision>> snapshot;
			lock (this.filters)
			{
				snapshot = new List<Func<NotificationIntent, NotificationFilterDecision>>(this.filters);
			}

			NotificationFilterDecision decision = NotificationFilterDecision.None;

			foreach (Func<NotificationIntent, NotificationFilterDecision> filter in snapshot)
			{
				CancellationToken.ThrowIfCancellationRequested();
				NotificationFilterDecision result = filter(Intent);
				decision = decision.Merge(result);
				if (decision.IgnoreRender && decision.IgnoreStore && decision.IgnoreRoute)
					return decision;
			}

			return decision;
		}
	}
}
