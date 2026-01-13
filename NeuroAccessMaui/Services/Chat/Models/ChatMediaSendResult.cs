namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Describes the outcome of a media send request.
	/// </summary>
	public class ChatMediaSendResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMediaSendResult"/> class.
		/// </summary>
		/// <param name="messageDescriptor">Associated message descriptor.</param>
		/// <param name="mediaDescriptor">Descriptor of uploaded media.</param>
		public ChatMediaSendResult(ChatMessageDescriptor messageDescriptor, ChatMediaDescriptor mediaDescriptor)
		{
			this.MessageDescriptor = messageDescriptor;
			this.MediaDescriptor = mediaDescriptor;
		}

		/// <summary>
		/// Message descriptor for the message created during the media send.
		/// </summary>
		public ChatMessageDescriptor MessageDescriptor { get; }

		/// <summary>
		/// Descriptor of uploaded media.
		/// </summary>
		public ChatMediaDescriptor MediaDescriptor { get; }
	}
}
