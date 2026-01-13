using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Renders markdown content into UI-agnostic segments with caching.
	/// </summary>
	[Singleton]
	internal class MarkdownRenderService : LoadableService, IMarkdownRenderService
	{
		private const int cacheCapacity = 128;

		private readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
		private readonly LinkedList<string> cacheOrder = new LinkedList<string>();
		private readonly object cacheLock = new object();

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
				lock (this.cacheLock)
				{
					this.cache.Clear();
					this.cacheOrder.Clear();
				}

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		public async Task<ChatRenderResult> RenderAsync(ChatRenderRequest Request, CancellationToken CancellationToken)
		{
			if (Request is null)
				throw new ArgumentNullException(nameof(Request));

			string CacheKey = this.CreateCacheKey(Request.MessageId, Request.Fingerprint);

			if (this.TryGetCachedInternal(CacheKey, out ChatRenderResult? Cached) && Cached is not null)
				return Cached;

			IReadOnlyList<ChatRenderSegment> Segments = await this.BuildSegmentsAsync(Request, CancellationToken).ConfigureAwait(false);
			ChatRenderResult Result = new ChatRenderResult(Request.MessageId, Request.Fingerprint, Segments, Request.Culture);

			this.StoreInCache(CacheKey, Result);

			return Result;
		}

		public bool TryGetCached(string MessageId, string Fingerprint, out ChatRenderResult? Result)
		{
			string CacheKey = this.CreateCacheKey(MessageId, Fingerprint);
			return this.TryGetCachedInternal(CacheKey, out Result);
		}

		public void Invalidate(string MessageId)
		{
			lock (this.cacheLock)
			{
				List<string> KeysToRemove = new List<string>();

				foreach (KeyValuePair<string, CacheEntry> Pair in this.cache)
				{
					if (string.Equals(Pair.Value.Result.MessageId, MessageId, StringComparison.Ordinal))
						KeysToRemove.Add(Pair.Key);
				}

				foreach (string Key in KeysToRemove)
					this.RemoveFromCache(Key);
			}
		}

		private async Task<IReadOnlyList<ChatRenderSegment>> BuildSegmentsAsync(ChatRenderRequest Request, CancellationToken CancellationToken)
		{
			List<ChatRenderSegment> Segments = new List<ChatRenderSegment>();

			if (!string.IsNullOrWhiteSpace(Request.Markdown))
			{
				MarkdownSettings Settings = this.CreateMarkdownSettings(Request.Culture);
				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Request.Markdown, Settings).ConfigureAwait(false);
				CancellationToken.ThrowIfCancellationRequested();

				string HtmlBody = HtmlDocument.GetBody(await Doc.GenerateHTML().ConfigureAwait(false));
				CancellationToken.ThrowIfCancellationRequested();

				string Plain = (await Doc.GeneratePlainText().ConfigureAwait(false)).Trim();
				CancellationToken.ThrowIfCancellationRequested();

				if (!string.IsNullOrEmpty(HtmlBody))
					Segments.Add(this.CreateSegment(ChatRenderSegmentType.Block, HtmlBody, "html"));

				if (!string.IsNullOrEmpty(Plain))
					Segments.Add(this.CreateSegment(ChatRenderSegmentType.Text, Plain, "plain"));
			}
			else if (!string.IsNullOrWhiteSpace(Request.Html))
			{
				string HtmlBody = HtmlDocument.GetBody(Request.Html);
				CancellationToken.ThrowIfCancellationRequested();

				string Plain = ExtractPlainTextFromHtml(HtmlBody);

				if (!string.IsNullOrEmpty(HtmlBody))
					Segments.Add(this.CreateSegment(ChatRenderSegmentType.Block, HtmlBody, "html"));

				if (!string.IsNullOrEmpty(Plain))
					Segments.Add(this.CreateSegment(ChatRenderSegmentType.Text, Plain, "plain"));
			}
			else
			{
				string PlainText = (Request.PlainText ?? string.Empty).Trim();
				if (!string.IsNullOrEmpty(PlainText))
					Segments.Add(this.CreateSegment(ChatRenderSegmentType.Text, PlainText, "plain"));
			}

			if (Segments.Count == 0)
				Segments.Add(this.CreateSegment(ChatRenderSegmentType.Text, string.Empty, "plain"));

			return Segments;
		}

		private MarkdownSettings CreateMarkdownSettings(CultureInfo Culture)
		{
			MarkdownSettings Settings = new MarkdownSettings
			{
				AllowScriptTag = false,
				EmbedEmojis = false,
				AudioAutoplay = false,
				AudioControls = false,
				ParseMetaData = false,
				VideoAutoplay = false,
				VideoControls = false
			};

			return Settings;
		}

		private ChatRenderSegment CreateSegment(ChatRenderSegmentType Type, string Value, string Format)
		{
			Dictionary<string, string> Attributes = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "format", Format }
			};

			return new ChatRenderSegment(Type, Value, Attributes);
		}

		private static string ExtractPlainTextFromHtml(string Html)
		{
			if (string.IsNullOrWhiteSpace(Html))
				return string.Empty;

			string WithoutTags = Regex.Replace(Html, "<[^>]+>", string.Empty);
			string Decoded = WebUtility.HtmlDecode(WithoutTags);
			return (Decoded ?? string.Empty).Trim();
		}

		private bool TryGetCachedInternal(string CacheKey, out ChatRenderResult? Result)
		{
			lock (this.cacheLock)
			{
				if (this.cache.TryGetValue(CacheKey, out CacheEntry? Entry))
				{
					this.cacheOrder.Remove(Entry.Node);
					Entry.Node = this.cacheOrder.AddFirst(CacheKey);
					Result = Entry.Result;
					return true;
				}
			}

			Result = null;
			return false;
		}

		private void StoreInCache(string CacheKey, ChatRenderResult Result)
		{
			lock (this.cacheLock)
			{
				if (this.cache.TryGetValue(CacheKey, out CacheEntry? Existing))
				{
					this.cacheOrder.Remove(Existing.Node);
				}

				LinkedListNode<string> Node = this.cacheOrder.AddFirst(CacheKey);
				this.cache[CacheKey] = new CacheEntry(Result, Node);

				this.TrimCache();
			}
		}

		private void RemoveFromCache(string CacheKey)
		{
			if (this.cache.TryGetValue(CacheKey, out CacheEntry? Entry))
			{
				this.cacheOrder.Remove(Entry.Node);
				this.cache.Remove(CacheKey);
			}
		}

		private void TrimCache()
		{
			while (this.cache.Count > cacheCapacity && this.cacheOrder.Last is not null)
			{
				string KeyToRemove = this.cacheOrder.Last.Value;
				this.cacheOrder.RemoveLast();
				this.cache.Remove(KeyToRemove);
			}
		}

		private string CreateCacheKey(string MessageId, string Fingerprint)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", MessageId, Fingerprint);
		}

		private sealed class CacheEntry
		{
			public CacheEntry(ChatRenderResult Result, LinkedListNode<string> Node)
			{
				this.Result = Result;
				this.Node = Node;
			}

			public ChatRenderResult Result { get; }

			public LinkedListNode<string> Node { get; set; }
		}
	}
}
