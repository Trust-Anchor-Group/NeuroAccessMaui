namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Counts unread notifications per token, so token lists can mark tokens with news.
	/// </summary>
	public interface ITokenUnreadTracker
	{
		/// <summary>
		/// Event raised when the number of unread notifications for a token changes. The argument is the token ID.
		/// </summary>
		event EventHandler<string>? UnreadChanged;

		/// <summary>
		/// Loads the stored unread token notifications, the first time it is called.
		/// </summary>
		Task EnsureLoadedAsync();

		/// <summary>
		/// Gets the number of unread notifications for a token.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <returns>Number of unread notifications.</returns>
		int GetUnreadCount(string TokenId);

		/// <summary>
		/// Marks every notification about a token as read.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		Task MarkTokenReadAsync(string TokenId);
	}
}
