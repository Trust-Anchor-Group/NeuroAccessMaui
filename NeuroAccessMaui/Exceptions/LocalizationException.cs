namespace NeuroAccessMaui.Exceptions
{
	/// <summary>
	/// Represents localization errors.
	/// </summary>
	public class LocalizationException : Exception
	{
		/// <summary>
		/// Creates an instance of a <see cref="LocalizationException"/>.
		/// </summary>
		public LocalizationException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizationException"/> class with the specified message.
		/// </summary>
		/// <param name="Message">The message.</param>
		public LocalizationException(string Message)
		: base(Message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizationException"/> class with the specified message and inner exception.
		/// </summary>
		/// <param name="Message">The message.</param>
		/// <param name="InnerException">The inner exception.</param>
		public LocalizationException(string Message, Exception InnerException)
		: base(Message, InnerException)
		{
		}
	}
}
