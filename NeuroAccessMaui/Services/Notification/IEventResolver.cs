namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Interface for event resolvers. Such can be used to resolve multiple pending notifications at once.
	/// </summary>
	public interface IEventResolver
	{
		/// <summary>
		/// If the resolver resolves an event.
		/// </summary>
		/// <param name="Event">Pending notification event.</param>
		/// <returns>If the resolver resolves the event.</returns>
		bool Resolves(NotificationEvent Event);
	}
}
