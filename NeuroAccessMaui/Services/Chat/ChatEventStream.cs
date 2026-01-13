using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// In-memory event stream used to batch transport and repository events before they reach the UI thread.
	/// </summary>
	[Singleton]
	internal class ChatEventStream : LoadableService, IChatEventStream
	{
		private readonly Dictionary<string, Queue<ChatSessionEvent>> queues = new Dictionary<string, Queue<ChatSessionEvent>>(StringComparer.OrdinalIgnoreCase);
		private readonly object queueLock = new object();
		private event EventHandler<ChatEventsAvailableEventArgs>? eventsAvailable;

		public event EventHandler<ChatEventsAvailableEventArgs> EventsAvailable
		{
			add { this.eventsAvailable += value; }
			remove { this.eventsAvailable -= value; }
		}

		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				lock (this.queueLock)
				{
					this.queues.Clear();
				}

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		public void Publish(ChatSessionEvent Event)
		{
			if (Event is null)
				throw new ArgumentNullException(nameof(Event));

			lock (this.queueLock)
			{
				if (!this.queues.TryGetValue(Event.RemoteBareJid, out Queue<ChatSessionEvent>? Queue))
				{
					Queue = new Queue<ChatSessionEvent>();
					this.queues[Event.RemoteBareJid] = Queue;
				}

				Queue.Enqueue(Event);
			}

			this.OnEventsAvailable(Event.RemoteBareJid);
		}

		public Task<IReadOnlyList<ChatSessionEvent>> DrainAsync(string RemoteBareJid, CancellationToken CancellationToken)
		{
			List<ChatSessionEvent> Events = new List<ChatSessionEvent>();

			lock (this.queueLock)
			{
				if (this.queues.TryGetValue(RemoteBareJid, out Queue<ChatSessionEvent>? Queue))
				{
					while (Queue.Count > 0)
					{
						CancellationToken.ThrowIfCancellationRequested();
						ChatSessionEvent SessionEvent = Queue.Dequeue();
						Events.Add(SessionEvent);
					}

					if (Queue.Count == 0)
						this.queues.Remove(RemoteBareJid);
				}
			}

			return Task.FromResult<IReadOnlyList<ChatSessionEvent>>(Events);
		}

		public void Clear(string RemoteBareJid)
		{
			lock (this.queueLock)
			{
				this.queues.Remove(RemoteBareJid);
			}
		}

		private void OnEventsAvailable(string RemoteBareJid)
		{
			EventHandler<ChatEventsAvailableEventArgs>? Handler = this.eventsAvailable;
			if (Handler is not null)
			{
				ChatEventsAvailableEventArgs Args = new ChatEventsAvailableEventArgs(RemoteBareJid);
				Handler.Invoke(this, Args);
			}
		}
	}
}
