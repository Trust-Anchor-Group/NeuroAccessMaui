namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Indicates the semantic type of a rendered chat segment.
	/// </summary>
	public enum ChatRenderSegmentType
	{
		/// <summary>
		/// Plain text run.
		/// </summary>
		Text,

		/// <summary>
		/// Hyperlink run.
		/// </summary>
		Link,

		/// <summary>
		/// Inline code.
		/// </summary>
		Code,

		/// <summary>
		/// Block element such as paragraph or quote.
		/// </summary>
		Block,

		/// <summary>
		/// Image or media placeholder.
		/// </summary>
		Media,

		/// <summary>
		/// Embedded component (contracts, identities, etc.).
		/// </summary>
		EmbeddedObject
	}
}
