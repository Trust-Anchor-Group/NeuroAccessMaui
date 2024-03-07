namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Controls
{
	/// <summary>
	/// Frame containing sent messages.
	/// </summary>
	public class ReceiveFrame : MessageFrame
	{
		/// <summary>
		/// Frame containing sent messages.
		/// </summary>
		public ReceiveFrame()
			: base()
		{
			this.Style = AppStyles.ReceiveFrame;
		}

		/// <inheritdoc/>
		public override MessageType MessageType => MessageType.Received;
	}
}
