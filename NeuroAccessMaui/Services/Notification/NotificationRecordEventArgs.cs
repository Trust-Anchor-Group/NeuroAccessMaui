using System;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Event args for notification record events.
	/// </summary>
	public sealed class NotificationRecordEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationRecordEventArgs"/> class.
		/// </summary>
		/// <param name="record">Notification record.</param>
		public NotificationRecordEventArgs(NotificationRecord Record)
		{
			this.Record = Record;
		}

		/// <summary>
		/// Gets the notification record.
		/// </summary>
		public NotificationRecord Record { get; }
	}
}
