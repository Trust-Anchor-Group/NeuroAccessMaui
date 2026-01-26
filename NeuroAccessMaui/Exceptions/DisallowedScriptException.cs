using Waher.Script.Model;

namespace NeuroAccessMaui.Exceptions
{
	/// <summary>
	/// Exception thrown when a script (or part of a script) is disallowed and should not be executed.
	/// </summary>
	public class DisallowedScriptException : Exception
	{
		/// <summary>
		/// Gets the script node that triggered the exception, if available.
		/// </summary>
		public ScriptNode? Node { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DisallowedScriptException"/> class.
		/// </summary>
		public DisallowedScriptException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisallowedScriptException"/> class with a specified error message.
		/// </summary>
		/// <param name="Message">The message that describes the error.</param>
		public DisallowedScriptException(string Message)
			: base(Message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisallowedScriptException"/> class with a specified error message
		/// and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="Message">The message that describes the error.</param>
		/// <param name="InnerException">The exception that is the cause of the current exception.</param>
		public DisallowedScriptException(string Message, Exception InnerException)
			: base(Message, InnerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisallowedScriptException"/> class with a specified error message
		/// and the script node that triggered the exception.
		/// </summary>
		/// <param name="Message">The message that describes the error.</param>
		/// <param name="Node">The script node that triggered the exception.</param>
		public DisallowedScriptException(string Message, ScriptNode? Node)
			: base(Message)
		{
			this.Node = Node;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisallowedScriptException"/> class with a specified error message,
		/// a reference to the inner exception that is the cause of this exception, and the script node that triggered the exception.
		/// </summary>
		/// <param name="Message">The message that describes the error.</param>
		/// <param name="InnerException">The exception that is the cause of the current exception.</param>
		/// <param name="Node">The script node that triggered the exception.</param>
		public DisallowedScriptException(string Message, Exception InnerException, ScriptNode? Node)
			: base(Message, InnerException)
		{
			this.Node = Node;
		}
	}
}
