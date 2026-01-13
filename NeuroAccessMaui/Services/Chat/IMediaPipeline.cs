using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Coordinates media capture, encryption, upload, and markdown generation.
	/// </summary>
	public interface IMediaPipeline
	{
		/// <summary>
		/// Sends media content and returns descriptors for the resulting message.
		/// </summary>
		/// <param name="Request">Media send request.</param>
		/// <param name="Progress">Progress reporter.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<ChatMediaSendResult> SendMediaAsync(ChatMediaSendRequest Request, IProgress<ChatMediaProgress>? Progress, CancellationToken CancellationToken);

		/// <summary>
		/// Generates markdown representing the supplied media descriptor.
		/// </summary>
		/// <param name="Descriptor">Media descriptor.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		Task<string> GenerateMarkdownAsync(ChatMediaDescriptor Descriptor, CancellationToken CancellationToken);
	}
}
