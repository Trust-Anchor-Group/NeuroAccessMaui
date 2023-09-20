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
	/// <param name="message">The message.</param>
	public MissingNetworkException(string message)
	: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MissingNetworkException"/> class with the specified message and inner exception.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="innerException">The inner exception.</param>
	public MissingNetworkException(string message, Exception innerException)
	: base(message, innerException)
	{
	}
}
