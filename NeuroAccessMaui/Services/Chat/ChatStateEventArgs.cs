namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// The other party in a chat started or stopped writing.
	/// </summary>
	/// <param name="RemoteBareJid">Address of the other party.</param>
	/// <param name="State">What the other party is doing.</param>
	public sealed class ChatStateEventArgs(string RemoteBareJid, ChatState State) : EventArgs
	{
		/// <summary>
		/// Address of the other party.
		/// </summary>
		public string RemoteBareJid { get; } = RemoteBareJid;

		/// <summary>
		/// What the other party is doing.
		/// </summary>
		public ChatState State { get; } = State;
	}
}
