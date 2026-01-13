using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Handles markdown rendering and caching responsibilities for chat messages.
	/// </summary>
	[DefaultImplementation(typeof(MarkdownRenderService))]
	public interface IMarkdownRenderService
	{
		/// <summary>
		/// Renders markdown into UI-agnostic segments.
		/// </summary>
		/// <param name="Request">Render request.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<ChatRenderResult> RenderAsync(ChatRenderRequest Request, CancellationToken CancellationToken);

		/// <summary>
		/// Attempts to retrieve a cached render result.
		/// </summary>
		/// <param name="MessageId">Message identifier.</param>
		/// <param name="Fingerprint">Content fingerprint.</param>
		/// <param name="Result">Cached result if available.</param>
		bool TryGetCached(string MessageId, string Fingerprint, out ChatRenderResult? Result);

		/// <summary>
		/// Invalidates any cached result for a message.
		/// </summary>
		/// <param name="MessageId">Message identifier.</param>
		void Invalidate(string MessageId);
	}
}
