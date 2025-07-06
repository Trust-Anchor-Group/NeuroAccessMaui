using System.Diagnostics.CodeAnalysis;
using Waher.Events;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Script.Functions.Strings;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Notification service
	/// </summary>
	[Singleton]
	public class NotificationService : LoadableService, INotificationService
	{
		private const int nrTypes = 4;

		private readonly SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>[] events;
		private readonly LinkedList<ExpectedEvent> expected;

		/// <summary>
		/// Notification service
		/// </summary>
		public NotificationService()
		{
			int i;

			this.events = new SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>[nrTypes];
			this.expected = new LinkedList<ExpectedEvent>();

			for (i = 0; i < nrTypes; i++)
				this.events[i] = [];
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="isResuming">Set to <c>true</c> to indicate the app is resuming as opposed to starting cold.</param>
		/// <param name="cancellationToken">Will stop the service load if the token is set.</param>
		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			SortedDictionary<CaseInsensitiveString, List<NotificationEvent>>? ByCategory = null;
			List<NotificationEvent>? Events = null;
			string? PrevCategory = null;
			int PrevType = -1;
			int Type;

			IEnumerable<NotificationEvent> LoadedEvents;

			try
			{
				LoadedEvents = await Database.Find<NotificationEvent>("Type", "Category");
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await Database.Clear("Notifications");
				LoadedEvents = [];
			}

			foreach (NotificationEvent Event in LoadedEvents)
			{
				if (Event.Type is null || Event.Category is null)
					continue;

				Type = (int)Event.Type;
				if (Type < 0 || Type >= nrTypes)
					continue;

				if (CaseInsensitiveString.IsNullOrEmpty(Event.Category))
				{
					Log.Debug("Notification event of type " + Event.GetType().FullName + " lacked Category.");
					await Database.Delete(Event);
					continue;
				}

				lock (this.events)
				{
					if (ByCategory is null || Type != PrevType)
					{
						ByCategory = this.events[Type];
						PrevType = Type;
					}

					if (Events is null || Event.Category != PrevCategory)
					{
						if (!ByCategory.TryGetValue(Event.Category, out Events))
						{
							Events = [];
							ByCategory[Event.Category] = Events;
						}

						PrevCategory = Event.Category;
					}

					Events.Add(Event);
				}
			}

			await base.Load(isResuming, cancellationToken);
		}

		/// <summary>
		/// Registers a type of notification as expected.
		/// </summary>
		/// <typeparam name="T">Type of event to expect.</typeparam>
		/// <param name="Before">If event is received before this time, it is opened automatically.</param>
		public void ExpectEvent<T>(DateTime Before, Predicate<T> Predicate)
			 where T : NotificationEvent
		{
			// First, look for an existing event of type T matching the predicate.
			NotificationEvent? MatchingEvent = null;
			int NrFound = 0;

			lock (this.events)
			{
				// Iterate over all types stored in the array.
				// (You might also restrict the search if you know a priori the type’s index.)
				for (int i = 0; i < nrTypes; i++)
				{
					SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
					foreach (List<NotificationEvent> EventsList in ByCategory.Values)
					{
						foreach (NotificationEvent Evt in EventsList)
						{
							if (Evt is T TypedEvent && Predicate(TypedEvent))
							{
								MatchingEvent = Evt;
								NrFound++;
								break;
							}
						}
						if (MatchingEvent is not null)
							break;
					}
					if (MatchingEvent is not null)
						break;
				}
			}

			if (MatchingEvent is not null)
			{
				// Optionally remove the event from the in-memory collection
				//RemoveEvent(matchingEvent);

				// Run the event immediately on the main thread.
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						await MatchingEvent.Open();
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				});
			}
			else
			{
				// Otherwise, add an expectation for a future event.
				lock (this.expected)
				{
					this.expected.AddLast(new ExpectedEvent(
						 typeof(T),
						 Before,
						 (NotificationEvent e) => e is T t && Predicate(t)
					));
				}
			}
		}

		/// <summary>
		/// Registers a new event and notifies the user.
		/// </summary>
		/// <param name="Event">Notification event.</param>
		public async Task NewEvent(NotificationEvent Event)
		{
			if (Event.Type is null || Event.Category is null)
				return;

			DateTime Now = DateTime.Now;
			bool IsExpected = false;

			lock (this.expected)
			{
				LinkedListNode<ExpectedEvent>? Node = this.expected.First;
				while (Node is not null)
				{
					LinkedListNode<ExpectedEvent>? Next = Node.Next;
					// Remove expired expectations.
					if (Node.Value.Before < Now)
					{
						this.expected.Remove(Node);
					}
					else if (Node.Value.EventType == Event.GetType() &&
								(Node.Value.Predicate is null || Node.Value.Predicate(Event)))
					{
						this.expected.Remove(Node);
						IsExpected = true;
						break;
					}
					Node = Next;
				}
			}

			if (IsExpected)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						await Event.Open();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				});
			}
			else
			{
				// Existing behavior: insert into database, add to events list, etc.
				await Database.Insert(Event);
				int Type = (int)Event.Type;
				if (Type >= 0 && Type < nrTypes)
				{
					lock (this.events)
					{
						SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[Type];
						if (!ByCategory.TryGetValue(Event.Category, out List<NotificationEvent>? Events))
						{
							Events = new List<NotificationEvent>();
							ByCategory[Event.Category] = Events;
						}
						Events.Add(Event);
					}

					await this.OnNewNotification.Raise(this, new NotificationEventArgs(Event));
				}

				// Prepare the event asynchronously.
				Task _ = Task.Run(async () =>
				{
					try
					{
						await Event.Prepare();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				});
			}
		}


		/// <summary>
		/// Deletes events for a given button and category.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <param name="Category">Category</param>
		public async Task DeleteEvents(NotificationEventType Type, CaseInsensitiveString Category)
		{
			int TypeIndex = (int)Type;

			if (TypeIndex >= 0 && TypeIndex < nrTypes)
			{
				NotificationEvent[] ToDelete;

				lock (this.events)
				{
					SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[TypeIndex];

					if (!ByCategory.TryGetValue(Category, out List<NotificationEvent>? Events))
						return;

					ToDelete = [.. Events];
					ByCategory.Remove(Category);
				}

				await this.DoDeleteEvents(ToDelete);
			}
		}

		/// <summary>
		/// Deletes a specified set of events.
		/// </summary>
		/// <param name="Events">Events to delete.</param>
		public Task DeleteEvents(params NotificationEvent[] Events)
		{
			foreach (NotificationEvent Event in Events)
			{
				if (Event.Type is null || Event.Category is null)
					continue;

				int TypeIndex = (int)Event.Type;

				if (TypeIndex >= 0 && TypeIndex < nrTypes)
				{
					lock (this.events)
					{
						SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[TypeIndex];

						if (ByCategory.TryGetValue(Event.Category, out List<NotificationEvent>? List) &&
							List.Remove(Event) &&
							List.Count == 0)
						{
							ByCategory.Remove(Event.Category);
						}
					}
				}

			}

			return this.DoDeleteEvents(Events);
		}

		private async Task DoDeleteEvents(NotificationEvent[] Events)
		{
			try
			{
				await Database.StartBulk();

				try
				{
					foreach (NotificationEvent Event in Events)
					{
						try
						{
							await Database.Delete(Event);
						}
						catch (KeyNotFoundException)
						{
							// Ignore, already deleted.
						}
					}
				}
				finally
				{
					await Database.EndBulk();
				}

				await this.OnNotificationsDeleted.Raise(this, new NotificationEventsArgs(Events));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Gets available notification events for a button.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <returns>Recorded events.</returns>
		public NotificationEvent[] GetEvents(NotificationEventType Type)
		{
			int i = (int)Type;
			if (i < 0 || i >= nrTypes)
				return [];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				List<NotificationEvent> Result = [];

				foreach (List<NotificationEvent> Events in ByCategory.Values)
					Result.AddRange(Events);

				return [.. Result];
			}
		}

		/// <summary>
		/// Gets available categories for a button.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <returns>Recorded categories.</returns>
		public CaseInsensitiveString[] GetCategories(NotificationEventType Type)
		{
			int i = (int)Type;
			if (i < 0 || i >= nrTypes)
				return [];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				List<CaseInsensitiveString> Result = [.. ByCategory.Keys];

				return [.. Result];
			}
		}

		/// <summary>
		/// Gets available notification events for a button, sorted by category.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <returns>Recorded events.</returns>
		public SortedDictionary<CaseInsensitiveString, NotificationEvent[]> GetEventsByCategory(NotificationEventType Type)
		{
			int i = (int)Type;
			if (i < 0 || i >= nrTypes)
				return [];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> Result = [];

				foreach (KeyValuePair<CaseInsensitiveString, List<NotificationEvent>> P in ByCategory)
					Result[P.Key] = [.. P.Value];

				return Result;
			}
		}

		/// <summary>
		/// Gets available notification events for a button, sorted by category.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <returns>Recorded events.</returns>
		public SortedDictionary<CaseInsensitiveString, T[]> GetEventsByCategory<T>(NotificationEventType Type)
			where T : NotificationEvent
		{
			int i = (int)Type;
			if (i < 0 || i >= nrTypes)
				return [];

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];
				SortedDictionary<CaseInsensitiveString, T[]> Result = [];

				foreach (KeyValuePair<CaseInsensitiveString, List<NotificationEvent>> P in ByCategory)
				{
					List<T>? Items = null;

					foreach (NotificationEvent Event in P.Value)
					{
						if (Event is T TypedItem)
						{
							Items ??= [];
							Items.Add(TypedItem);
						}
					}

					if (Items is not null)
						Result[P.Key] = [.. Items];
				}

				return Result;
			}
		}

		/// <summary>
		/// Tries to get available notification events.
		/// </summary>
		/// <param name="Type">Type of event</param>
		/// <param name="Category">Notification event category</param>
		/// <param name="Events">Notification events, if found.</param>
		/// <returns>If notification events where found for the given category.</returns>
		public bool TryGetNotificationEvents(NotificationEventType Type, CaseInsensitiveString Category,
			[NotNullWhen(true)] out NotificationEvent[]? Events)
		{
			int i = (int)Type;
			if (i < 0 || i >= nrTypes)
			{
				Events = null;
				return false;
			}

			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory = this.events[i];

				if (!ByCategory.TryGetValue(Category, out List<NotificationEvent>? Events2))
				{
					Events = null;
					return false;
				}

				Events = [.. Events2];
				return true;
			}
		}

		/// <summary>
		/// Gets all notification events across all types and categories.
		/// </summary>
		/// <returns>All recorded notification events.</returns>
		public NotificationEvent[] GetAllEvents()
		{
			List<NotificationEvent> AllEvents = [];

			lock (this.events)
			{
				// Loop through each event type.
				foreach (SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory in this.events)
				{
					// Loop through each category list.
					foreach (List<NotificationEvent> EventsList in ByCategory.Values)
					{
						AllEvents.AddRange(EventsList);
					}
				}
			}

			return [.. AllEvents];
		}


		/// <summary>
		/// Event raised when a new notification has been logged.
		/// </summary>
		public event EventHandlerAsync<NotificationEventArgs>? OnNewNotification;

		/// <summary>
		/// Event raised when notifications have been deleted.
		/// </summary>
		public event EventHandlerAsync<NotificationEventsArgs>? OnNotificationsDeleted;

		/// <summary>
		/// Number of notifications but button Contacts
		/// </summary>
		public int NrNotificationsContacts => this.Count((int)NotificationEventType.Contacts);

		/// <summary>
		/// Number of notifications but button Things
		/// </summary>
		public int NrNotificationsThings => this.Count((int)NotificationEventType.Things);

		/// <summary>
		/// Number of notifications but button Contracts
		/// </summary>
		public int NrNotificationsContracts => this.Count((int)NotificationEventType.Contracts);

		/// <summary>
		/// Number of notifications but button Wallet
		/// </summary>
		public int NrNotificationsWallet => this.Count((int)NotificationEventType.Wallet);

		private int Count(int Index)
		{
			lock (this.events)
			{
				SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> Events = this.events[Index];
				int Result = 0;

				foreach (List<NotificationEvent> List in Events.Values)
					Result += List.Count;

				return Result;
			}
		}

		/// <summary>
		/// Deletes pending events that have already been resolved.
		/// </summary>
		/// <param name="Resolver">Notification event resolver, determining which events are resolved.</param>
		public async Task DeleteResolvedEvents(IEventResolver Resolver)
		{
			List<NotificationEvent>? Resolved = null;

			lock (this.events)
			{
				foreach (SortedDictionary<CaseInsensitiveString, List<NotificationEvent>> ByCategory in this.events)
				{
					foreach (KeyValuePair<CaseInsensitiveString, List<NotificationEvent>> P in ByCategory)
					{
						foreach (NotificationEvent Event in P.Value)
						{
							if (Resolver.Resolves(Event))
							{
								Resolved ??= [];
								Resolved.Add(Event);
							}
						}
					}
				}
			}

			if (Resolved is not null)
				await this.DeleteEvents([.. Resolved]);
		}

	}
}
