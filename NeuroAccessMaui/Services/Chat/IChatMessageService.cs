using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	public interface IChatMessageService
	{
		Task<ChatMessageDescriptor> SendMarkdownAsync(string RemoteBareJid, string Markdown, CancellationToken CancellationToken, string? ReplyToId = null, string? ReplaceMessageId = null);
	}
}
