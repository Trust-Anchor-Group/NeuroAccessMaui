using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Helper responsible for backfilling new chat message fields introduced in the modernization.
	/// </summary>
	internal static class ChatMessageMigration
	{
		public static async Task BackfillAsync(CancellationToken CancellationToken)
		{
			IEnumerable<ChatMessageRecord> Records = await Database.Find<ChatMessageRecord>();

			foreach (ChatMessageRecord Record in Records)
			{
				CancellationToken.ThrowIfCancellationRequested();

				bool Changed = false;

				if (Record.OriginalCreatedTimestamp is null || Record.OriginalCreatedTimestamp == DateTime.MinValue)
				{
					Record.OriginalCreatedTimestamp = Record.Created;
					Changed = true;
				}

				ChatMessageDirection Direction = ResolveDirection(Record);
				if (Record.Direction != Direction)
				{
					Record.Direction = Direction;
					Changed = true;
				}

				ChatDeliveryStatus DeliveryStatus = ResolveDeliveryStatus(Record, Direction);
				if (Record.DeliveryStatus != DeliveryStatus)
				{
					Record.DeliveryStatus = DeliveryStatus;
					Changed = true;
				}

				if (string.IsNullOrEmpty(Record.ContentFingerprint))
				{
					Record.ContentFingerprint = ComputeFingerprint(Record.Markdown, Record.PlainText, Record.Html);
					Changed = true;
				}

				if (Changed)
					await Database.Update(Record);
			}
		}

		private static ChatMessageDirection ResolveDirection(ChatMessageRecord Record)
		{
			if (Record.Direction == ChatMessageDirection.Outgoing || Record.Direction == ChatMessageDirection.Incoming || Record.Direction == ChatMessageDirection.System)
				return Record.Direction;

			if (Record.LegacyMessageType == ChatLegacyMessageType.Received)
				return ChatMessageDirection.Incoming;

			return ChatMessageDirection.Outgoing;
		}

		private static ChatDeliveryStatus ResolveDeliveryStatus(ChatMessageRecord Record, ChatMessageDirection Direction)
		{
			if (Record.DeliveryStatus == ChatDeliveryStatus.Pending || Record.DeliveryStatus == ChatDeliveryStatus.Sending ||
				Record.DeliveryStatus == ChatDeliveryStatus.Sent || Record.DeliveryStatus == ChatDeliveryStatus.Failed ||
				Record.DeliveryStatus == ChatDeliveryStatus.Received || Record.DeliveryStatus == ChatDeliveryStatus.Displayed)
				return Record.DeliveryStatus;

			if (Direction == ChatMessageDirection.Incoming || Direction == ChatMessageDirection.System)
				return ChatDeliveryStatus.Received;

			return ChatDeliveryStatus.Sent;
		}

		private static string ComputeFingerprint(string Markdown, string PlainText, string Html)
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
