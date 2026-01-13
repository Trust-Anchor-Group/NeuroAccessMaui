using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Repository implementation backed by Waher persistence.
	/// </summary>
	[Singleton]
	internal class ChatMessageRepository : LoadableService, IChatMessageRepository
	{
		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				await ChatMessageMigration.BackfillAsync(CancellationToken).ConfigureAwait(false);
				this.EndLoad(true);
			}
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		public async Task<IReadOnlyList<ChatMessageDescriptor>> LoadRecentAsync(string RemoteBareJid, int PageSize, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (PageSize <= 0)
				return Array.Empty<ChatMessageDescriptor>();

			CaseInsensitiveString Remote = new CaseInsensitiveString(RemoteBareJid);
			Filter Filter = new FilterFieldEqualTo("RemoteBareJid", Remote);
			IEnumerable<ChatMessageRecord> RecordsEnumerable = await Database.Find<ChatMessageRecord>(0, PageSize, Filter, "-Created");
			ChatMessageRecord[] Records = RecordsEnumerable.ToArray();

			List<ChatMessageDescriptor> Descriptors = new List<ChatMessageDescriptor>(Records.Length);
			foreach (ChatMessageRecord Record in Records)
			{
				CancellationToken.ThrowIfCancellationRequested();
				ChatMessageDescriptor Descriptor = this.ToDescriptor(Record);
				Descriptors.Add(Descriptor);
			}

			return Descriptors;
		}

		public async Task<IReadOnlyList<ChatMessageDescriptor>> LoadOlderAsync(string RemoteBareJid, DateTime Before, int PageSize, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (PageSize <= 0)
				return Array.Empty<ChatMessageDescriptor>();

			CaseInsensitiveString Remote = new CaseInsensitiveString(RemoteBareJid);
			Filter Filter = new FilterAnd(
				new FilterFieldEqualTo("RemoteBareJid", Remote),
				new FilterFieldLesserThan("Created", Before));

			IEnumerable<ChatMessageRecord> RecordsEnumerable = await Database.Find<ChatMessageRecord>(0, PageSize, Filter, "-Created");
			ChatMessageRecord[] Records = RecordsEnumerable.ToArray();

			List<ChatMessageDescriptor> Descriptors = new List<ChatMessageDescriptor>(Records.Length);
			foreach (ChatMessageRecord Record in Records)
			{
				CancellationToken.ThrowIfCancellationRequested();
				ChatMessageDescriptor Descriptor = this.ToDescriptor(Record);
				Descriptors.Add(Descriptor);
			}

			return Descriptors;
		}

		public async Task SaveAsync(ChatMessageDescriptor Descriptor, CancellationToken CancellationToken)
		{
			if (Descriptor is null)
				throw new ArgumentNullException(nameof(Descriptor));

			ChatMessageRecord Record = this.ToRecord(Descriptor);
			await Database.Insert(Record);
			Descriptor.MessageId = Record.ObjectId ?? Descriptor.MessageId;
		}

		public async Task ReplaceAsync(ChatMessageDescriptor Descriptor, CancellationToken CancellationToken)
		{
			if (Descriptor is null)
				throw new ArgumentNullException(nameof(Descriptor));

			ChatMessageRecord Record = this.ToRecord(Descriptor);
			await Database.Update(Record);
		}

		public async Task UpdateDeliveryStatusAsync(string RemoteBareJid, string MessageId, ChatDeliveryStatus DeliveryStatus, DateTime Timestamp, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(MessageId))
				throw new ArgumentException("Message id is required.", nameof(MessageId));

			ChatMessageRecord? Record = await this.FindByMessageIdAsync(RemoteBareJid, MessageId, CancellationToken);
			if (Record is null)
				return;

			Record.DeliveryStatus = DeliveryStatus;
			Record.Updated = Timestamp;
			await Database.Update(Record);
		}

		public async Task<ChatMessageDescriptor?> GetAsync(string RemoteBareJid, string MessageId, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(MessageId))
				throw new ArgumentException("Message id is required.", nameof(MessageId));

			ChatMessageRecord? Record = await this.FindByMessageIdAsync(RemoteBareJid, MessageId, CancellationToken);
			if (Record is null)
				return null;

			return this.ToDescriptor(Record);
		}

		public async Task DeleteAsync(string RemoteBareJid, string MessageId, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(MessageId))
				throw new ArgumentException("Message id is required.", nameof(MessageId));

			ChatMessageRecord? Record = await this.FindByMessageIdAsync(RemoteBareJid, MessageId, CancellationToken);
			if (Record is null)
				return;

			await Database.Delete(Record);
		}

		private ChatMessageDescriptor ToDescriptor(ChatMessageRecord Record)
		{
			string RemoteBareJid = Record.RemoteBareJid?.Value ?? string.Empty;
			string MessageId = Record.ObjectId ?? Record.LocalTempId ?? string.Empty;
			IReadOnlyDictionary<string, string>? Metadata = this.DeserializeMetadata(Record.MetadataJson);
			ChatMessageDirection Direction = this.ResolveDirection(Record);
			ChatDeliveryStatus DeliveryStatus = this.ResolveDeliveryStatus(Record, Direction);
			DateTime? OriginalCreated = Record.OriginalCreatedTimestamp ?? Record.Created;
			string Fingerprint = this.EnsureFingerprint(Record);

			ChatMessageDescriptor Descriptor = new ChatMessageDescriptor
			{
				MessageId = MessageId,
				RemoteBareJid = RemoteBareJid,
				LocalTempId = Record.LocalTempId,
				RemoteObjectId = Record.RemoteObjectId,
				Direction = Direction,
				DeliveryStatus = DeliveryStatus,
				Created = Record.Created,
				Updated = Record.Updated,
				IsEdited = Record.IsEdited,
				OriginalCreated = OriginalCreated,
				ReplyToId = Record.ReplyToId,
				Markdown = Record.Markdown,
				PlainText = Record.PlainText,
				Html = Record.Html,
				ContentFingerprint = Fingerprint,
				Metadata = Metadata
			};

			return Descriptor;
		}

		private ChatMessageRecord ToRecord(ChatMessageDescriptor Descriptor)
		{
			string RemoteBareJid = Descriptor.RemoteBareJid ?? string.Empty;
			string? MessageId = string.IsNullOrEmpty(Descriptor.MessageId) ? null : Descriptor.MessageId;
			CaseInsensitiveString Remote = new CaseInsensitiveString(RemoteBareJid);
			string? MetadataJson = this.SerializeMetadata(Descriptor.Metadata);
			string Fingerprint = string.IsNullOrEmpty(Descriptor.ContentFingerprint)
				? this.ComputeFingerprint(Descriptor.Markdown, Descriptor.PlainText, Descriptor.Html)
				: Descriptor.ContentFingerprint;
			ChatLegacyMessageType LegacyType = this.ToLegacyMessageType(Descriptor.Direction);

			ChatMessageRecord Record = new ChatMessageRecord
			{
				ObjectId = MessageId,
				RemoteBareJid = Remote,
				LocalTempId = Descriptor.LocalTempId,
				RemoteObjectId = Descriptor.RemoteObjectId,
				Direction = Descriptor.Direction,
				DeliveryStatus = Descriptor.DeliveryStatus,
				Created = Descriptor.Created,
				Updated = Descriptor.Updated,
				IsEdited = Descriptor.IsEdited,
				OriginalCreatedTimestamp = Descriptor.OriginalCreated ?? Descriptor.Created,
				ReplyToId = Descriptor.ReplyToId,
				Markdown = Descriptor.Markdown ?? string.Empty,
				PlainText = Descriptor.PlainText ?? string.Empty,
				Html = Descriptor.Html ?? string.Empty,
				ContentFingerprint = Fingerprint,
				MetadataJson = MetadataJson,
				LegacyMessageType = LegacyType
			};

			return Record;
		}

		private async Task<ChatMessageRecord?> FindByMessageIdAsync(string RemoteBareJid, string MessageId, CancellationToken CancellationToken)
		{
			ChatMessageRecord? Record = await Database.TryLoadObject<ChatMessageRecord>(MessageId);
			if (Record is not null)
			{
				if (!string.IsNullOrEmpty(Record.RemoteBareJid) && string.Equals(Record.RemoteBareJid.Value, RemoteBareJid, StringComparison.OrdinalIgnoreCase))
					return Record;
			}

			CaseInsensitiveString Remote = new CaseInsensitiveString(RemoteBareJid);

			Filter RemoteObjectFilter = new FilterAnd(
				new FilterFieldEqualTo("RemoteBareJid", Remote),
				new FilterFieldEqualTo("RemoteObjectId", MessageId));

			Record = await Database.FindFirstIgnoreRest<ChatMessageRecord>(RemoteObjectFilter);
			if (Record is not null)
				return Record;

			Filter TempFilter = new FilterAnd(
				new FilterFieldEqualTo("RemoteBareJid", Remote),
				new FilterFieldEqualTo("LocalTempId", MessageId));

			return await Database.FindFirstIgnoreRest<ChatMessageRecord>(TempFilter);
		}

		private string? SerializeMetadata(IReadOnlyDictionary<string, string>? Metadata)
		{
			if (Metadata is null || Metadata.Count == 0)
				return null;

			return JsonSerializer.Serialize(Metadata);
		}

		private IReadOnlyDictionary<string, string>? DeserializeMetadata(string? MetadataJson)
		{
			if (string.IsNullOrEmpty(MetadataJson))
				return null;

			Dictionary<string, string>? Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson);
			if (Metadata is null || Metadata.Count == 0)
				return null;

			return Metadata;
		}

		private ChatMessageDirection ResolveDirection(ChatMessageRecord Record)
		{
			ChatMessageDirection Direction = Record.Direction;
			if (Direction == ChatMessageDirection.Outgoing || Direction == ChatMessageDirection.Incoming || Direction == ChatMessageDirection.System)
				return Direction;

			if (Record.LegacyMessageType == ChatLegacyMessageType.Received)
				return ChatMessageDirection.Incoming;

			return ChatMessageDirection.Outgoing;
		}

		private ChatDeliveryStatus ResolveDeliveryStatus(ChatMessageRecord Record, ChatMessageDirection Direction)
		{
			ChatDeliveryStatus DeliveryStatus = Record.DeliveryStatus;
			if (DeliveryStatus != ChatDeliveryStatus.Pending && DeliveryStatus != ChatDeliveryStatus.Sending && DeliveryStatus != ChatDeliveryStatus.Sent &&
				DeliveryStatus != ChatDeliveryStatus.Failed && DeliveryStatus != ChatDeliveryStatus.Received && DeliveryStatus != ChatDeliveryStatus.Displayed)
			{
				if (Direction == ChatMessageDirection.Incoming)
					return ChatDeliveryStatus.Received;

				if (Direction == ChatMessageDirection.System)
					return ChatDeliveryStatus.Received;

				return ChatDeliveryStatus.Sent;
			}

			return DeliveryStatus;
		}

		private string EnsureFingerprint(ChatMessageRecord Record)
		{
			if (!string.IsNullOrEmpty(Record.ContentFingerprint))
				return Record.ContentFingerprint;

			return this.ComputeFingerprint(Record.Markdown, Record.PlainText, Record.Html);
		}

		private ChatLegacyMessageType ToLegacyMessageType(ChatMessageDirection Direction)
		{
			if (Direction == ChatMessageDirection.Incoming)
				return ChatLegacyMessageType.Received;

			return ChatLegacyMessageType.Sent;
		}

		private string ComputeFingerprint(string Markdown, string PlainText, string Html)
		{
			string SafeMarkdown = Markdown ?? string.Empty;
			string SafePlainText = PlainText ?? string.Empty;
			string SafeHtml = Html ?? string.Empty;
			string Composite = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}", SafeMarkdown, SafePlainText, SafeHtml);

			byte[] CompositeBytes = Encoding.UTF8.GetBytes(Composite);
			byte[] Hash;

			using (SHA256 Sha = SHA256.Create())
			{
				Hash = Sha.ComputeHash(CompositeBytes);
			}

			return Convert.ToBase64String(Hash);
		}
	}
}
