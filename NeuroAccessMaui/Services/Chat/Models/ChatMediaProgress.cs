namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents progress for a media transfer.
	/// </summary>
	public class ChatMediaProgress
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMediaProgress"/> class.
		/// </summary>
		/// <param name="bytesTransferred">Bytes transferred so far.</param>
		/// <param name="totalBytes">Total bytes to transfer.</param>
		public ChatMediaProgress(long bytesTransferred, long totalBytes)
		{
			this.BytesTransferred = bytesTransferred;
			this.TotalBytes = totalBytes;
			this.Percent = totalBytes > 0 ? bytesTransferred / (double)totalBytes : 0d;
		}

		/// <summary>
		/// Bytes transferred so far.
		/// </summary>
		public long BytesTransferred { get; }

		/// <summary>
		/// Total bytes to transfer.
		/// </summary>
		public long TotalBytes { get; }

		/// <summary>
		/// Percentage completion (0-1).
		/// </summary>
		public double Percent { get; }
	}
}
