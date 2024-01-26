namespace NeuroAccessMaui.Services.Notification.Wallet
{
	/// <summary>
	/// Abstract base class for wallet notification events.
	/// </summary>
	public abstract class WalletNotificationEvent : NotificationEvent
	{
		/// <summary>
		/// Abstract base class for wallet notification events.
		/// </summary>
		public WalletNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class for wallet notification events.
		/// </summary>
		/// <param name="_">Event arguments</param>
		public WalletNotificationEvent(EventArgs _)
			: base()
		{
			this.Type = NotificationEventType.Wallet;
			this.Timestamp = this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Timestamp
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <returns>Money Icon</returns>
		public override Task<string> GetCategoryIcon()
		{
			return Task.FromResult<string>("💵");	// TODO: SVG icon
		}
	}
}
