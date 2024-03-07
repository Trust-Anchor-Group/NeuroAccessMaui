namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Controls
{
	/// <summary>
	/// Frame containing sent messages.
	/// </summary>
	public class SendFrame : MessageFrame
	{
		/// <summary>
		/// Frame containing sent messages.
		/// </summary>
		public SendFrame()
			: base()
		{
			this.Style = AppStyles.SendFrame;
		}

		/// <inheritdoc/>
		public override MessageType MessageType => MessageType.Sent;
	}
}
