using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using NeuroAccessMaui.Services;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	[Singleton]
	internal class ChatMessageService : LoadableService, IChatMessageService
	{
		public Task<ChatMessageDescriptor> SendMarkdownAsync(string RemoteBareJid, string Markdown, CancellationToken CancellationToken, string? ReplyToId = null, string? ReplaceMessageId = null)
		{
			return this.SendMarkdownInternalAsync(RemoteBareJid, Markdown, CancellationToken, ReplyToId, ReplaceMessageId);
		}

		private async Task<ChatMessageDescriptor> SendMarkdownInternalAsync(string RemoteBareJid, string Markdown, CancellationToken CancellationToken, string? ReplyToId, string? ReplaceMessageId)
		{
			if (string.IsNullOrWhiteSpace(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrWhiteSpace(Markdown))
				throw new ArgumentException("Markdown is required.", nameof(Markdown));

			IChatMessageRepository repository = ServiceRef.ChatMessageRepository;
			IChatTransportService transport = ServiceRef.ChatTransportService;
			IChatEventStream eventStream = ServiceRef.ChatEventStream;

			CancellationToken.ThrowIfCancellationRequested();

			MarkdownDocument Document = await MarkdownDocument.CreateAsync(Markdown, this.CreateMarkdownSettings()).ConfigureAwait(false);

			string Html = HtmlDocument.GetBody(await Document.GenerateHTML().ConfigureAwait(false));
			string Plain = (await Document.GeneratePlainText().ConfigureAwait(false)).Trim();
			if (string.IsNullOrEmpty(Plain))
				Plain = Markdown;

			DateTime Timestamp = DateTime.UtcNow;
			ChatMessageDescriptor? Descriptor;

			if (!string.IsNullOrEmpty(ReplaceMessageId))
			{
				Descriptor = await repository.GetAsync(RemoteBareJid, ReplaceMessageId, CancellationToken).ConfigureAwait(false);

				if (Descriptor is not null)
				{
				Descriptor.Markdown = Markdown;
					Descriptor.Html = Html;
					Descriptor.PlainText = Plain;
					Descriptor.Updated = Timestamp;
					Descriptor.IsEdited = true;
					Descriptor.ReplyToId = ReplyToId;
					Descriptor.DeliveryStatus = ChatDeliveryStatus.Pending;
					Descriptor.ContentFingerprint = string.Empty;
					Descriptor.LocalTempId ??= Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
					Descriptor.OriginalCreated ??= Descriptor.Created;

					await repository.ReplaceAsync(Descriptor, CancellationToken).ConfigureAwait(false);

					eventStream.Publish(new ChatSessionEvent(ChatSessionEventType.MessageUpdated, RemoteBareJid, new[] { Descriptor }));

					await this.SendOutboundMessageAsync(Descriptor, Markdown, transport, repository, eventStream, CancellationToken, true).ConfigureAwait(false);

					return Descriptor;
				}
			}

			Descriptor = new ChatMessageDescriptor
			{
				RemoteBareJid = RemoteBareJid,
				LocalTempId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
				Direction = ChatMessageDirection.Outgoing,
				DeliveryStatus = ChatDeliveryStatus.Pending,
				Created = Timestamp,
				Updated = Timestamp,
				Markdown = Markdown,
				PlainText = Plain,
				Html = Html,
				ReplyToId = ReplyToId,
				IsEdited = false,
				ContentFingerprint = string.Empty,
				OriginalCreated = Timestamp
			};

			await repository.SaveAsync(Descriptor, CancellationToken).ConfigureAwait(false);

			eventStream.Publish(new ChatSessionEvent(ChatSessionEventType.MessagesAppended, RemoteBareJid, new[] { Descriptor }));

			await this.SendOutboundMessageAsync(Descriptor, Markdown, transport, repository, eventStream, CancellationToken, false).ConfigureAwait(false);

			return Descriptor;
		}

		private async Task SendOutboundMessageAsync(ChatMessageDescriptor Descriptor, string Markdown, IChatTransportService Transport, IChatMessageRepository Repository, IChatEventStream EventStream, CancellationToken CancellationToken, bool IsCorrection)
		{
			ChatOutboundMessage Outbound = new ChatOutboundMessage(
				Descriptor.RemoteBareJid,
				Markdown,
				Descriptor.PlainText,
				Descriptor.Html,
				Descriptor.ReplyToId,
				Descriptor.LocalTempId,
				Descriptor.Metadata);

			try
			{
				string? RemoteObjectId;

				if (IsCorrection && !string.IsNullOrEmpty(Descriptor.RemoteObjectId))
				{
					await Transport.SendCorrectionAsync(Descriptor.RemoteBareJid, Descriptor.RemoteObjectId, Outbound, CancellationToken).ConfigureAwait(false);
					RemoteObjectId = Descriptor.RemoteObjectId;
				}
				else
				{
					RemoteObjectId = await Transport.SendAsync(Outbound, CancellationToken).ConfigureAwait(false);
				}

				if (!string.IsNullOrEmpty(RemoteObjectId))
				{
					Descriptor.RemoteObjectId = RemoteObjectId;
				}

				Descriptor.DeliveryStatus = ChatDeliveryStatus.Sent;
				Descriptor.Updated = DateTime.UtcNow;

				await Repository.ReplaceAsync(Descriptor, CancellationToken).ConfigureAwait(false);

				Dictionary<string, string> AdditionalData = new Dictionary<string, string>(StringComparer.Ordinal)
				{
					{ "MessageId", Descriptor.MessageId },
					{ "LocalTempId", Descriptor.LocalTempId ?? string.Empty },
					{ "DeliveryStatus", Descriptor.DeliveryStatus.ToString() }
				};

				EventStream.Publish(new ChatSessionEvent(ChatSessionEventType.DeliveryReceipt, Descriptor.RemoteBareJid, new[] { Descriptor }, AdditionalData));
			}
			catch (Exception ex)
			{
				Descriptor.DeliveryStatus = ChatDeliveryStatus.Failed;
				Descriptor.Updated = DateTime.UtcNow;

				await Repository.ReplaceAsync(Descriptor, CancellationToken).ConfigureAwait(false);

				Dictionary<string, string> AdditionalData = new Dictionary<string, string>(StringComparer.Ordinal)
				{
					{ "MessageId", Descriptor.MessageId },
					{ "LocalTempId", Descriptor.LocalTempId ?? string.Empty },
					{ "DeliveryStatus", Descriptor.DeliveryStatus.ToString() },
					{ "Error", ex.Message }
				};

				EventStream.Publish(new ChatSessionEvent(ChatSessionEventType.DeliveryReceipt, Descriptor.RemoteBareJid, new[] { Descriptor }, AdditionalData));

				throw;
			}
		}

		private MarkdownSettings CreateMarkdownSettings()
		{
			return new MarkdownSettings
			{
				AllowScriptTag = false,
				EmbedEmojis = false,
				AudioAutoplay = false,
				AudioControls = false,
				ParseMetaData = false,
				VideoAutoplay = false,
				VideoControls = false
			};
		}
	}
}
