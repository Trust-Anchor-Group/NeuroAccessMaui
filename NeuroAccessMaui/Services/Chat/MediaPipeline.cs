using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Placeholder media pipeline until full encryption and upload orchestration is implemented.
	/// </summary>
	internal class MediaPipeline : LoadableService, IMediaPipeline
	{
		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		public Task<ChatMediaSendResult> SendMediaAsync(ChatMediaSendRequest Request, IProgress<ChatMediaProgress>? Progress, CancellationToken CancellationToken)
		{
			DateTime Timestamp = DateTime.UtcNow;
			string MessageId = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
			string Fingerprint = MessageId;
			ChatMessageDescriptor MessageDescriptor = new ChatMessageDescriptor
			{
				MessageId = MessageId,
				RemoteBareJid = Request.RemoteBareJid,
				Direction = ChatMessageDirection.Outgoing,
				DeliveryStatus = ChatDeliveryStatus.Pending,
				Created = Timestamp,
				Updated = Timestamp,
				IsEdited = false,
				Markdown = string.Empty,
				PlainText = string.Empty,
				Html = string.Empty,
				ContentFingerprint = Fingerprint
			};

			ChatMediaDescriptor MediaDescriptor = new ChatMediaDescriptor(MessageId, Request.ContentType, 0, null);
			ChatMediaSendResult Result = new ChatMediaSendResult(MessageDescriptor, MediaDescriptor);

			if (Progress is not null)
			{
				ChatMediaProgress CompletedProgress = new ChatMediaProgress(1, 1);
				Progress.Report(CompletedProgress);
			}

			return Task.FromResult(Result);
		}

		public Task<string> GenerateMarkdownAsync(ChatMediaDescriptor Descriptor, CancellationToken CancellationToken)
		{
			string Markdown = $"![media]({Descriptor.MediaId})";
			return Task.FromResult(Markdown);
		}
	}
}
