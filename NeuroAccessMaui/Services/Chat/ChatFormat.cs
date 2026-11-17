namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// A markdown format the formatting bar of a chat applies to the text being written.
	/// </summary>
	public enum ChatFormat
	{
		/// <summary>Bold text, between ** marks.</summary>
		Bold,

		/// <summary>Italic text, between * marks.</summary>
		Italic,

		/// <summary>Struck-through text, between ~~ marks.</summary>
		Strikethrough,

		/// <summary>Code in the text, between ` marks.</summary>
		Code,

		/// <summary>A link, as [text](address).</summary>
		Link,

		/// <summary>Lines of a bulleted list, each starting with "- ".</summary>
		BulletList,

		/// <summary>Lines of a numbered list, each starting with its number.</summary>
		NumberedList,

		/// <summary>Quoted lines, each starting with "> ".</summary>
		Quote,

		/// <summary>A block of code, between ``` lines.</summary>
		CodeBlock
	}
}
