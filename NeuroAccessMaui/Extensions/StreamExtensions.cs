namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// An extensions class for <see cref="Stream"/>s.
	/// </summary>
	public static class StreamExtensions
	{
		/// <summary>
		/// Convenience method for resetting a stream to position = 0.
		/// </summary>
		/// <param name="Stream">The stream to reset.</param>
		public static void Reset(this Stream Stream)
		{
			if (Stream is not null && Stream.CanSeek)
				Stream.Seek(0, SeekOrigin.Begin);
			else if (Stream is not null)
				Stream.Position = 0;
		}

		/// <summary>
		/// Returns a byte array containing the contents of a stream.
		/// </summary>
		/// <param name="Stream">Stream.</param>
		/// <returns>Byte array.</returns>
		public static byte[]? ToByteArray(this Stream Stream)
		{
			if (Stream is null)
				return null;

			if (Stream is not MemoryStream ms)
			{
				ms = new MemoryStream();
				Stream.CopyTo(ms);
			}

			return ms.ToArray();
		}
	}
}
