namespace NeuroAccessMaui.Exceptions;

/// <summary>
/// Represents network errors.
/// </summary>
public class MissingNetworkException : Exception
{
	/// <summary>
	/// Creates an instance of a <see cref="MissingNetworkException"/>.
	/// </summary>
	public MissingNetworkException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MissingNetworkException"/> class with the specified message.
	/// </summary>
	/// <param name="Message">The message.</param>
	public MissingNetworkException(string Message)
	: base(Message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MissingNetworkException"/> class with the specified message and inner exception.
	/// </summary>
	/// <param name="Message">The message.</param>
	/// <param name="InnerException">The inner exception.</param>
	public MissingNetworkException(string Message, Exception InnerException)
	: base(Message, InnerException)
	{
	}
}
