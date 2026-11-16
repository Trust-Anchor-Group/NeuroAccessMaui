using System.Text;
using System.Text.RegularExpressions;
using Waher.Networking.Sniffers;
using Waher.Networking.Sniffers.Model;

namespace NeuroAccessMaui.Services.Xmpp
{
	/// <summary>
	/// Counts the requests the app sends to the server, grouped by what the user was doing when they were sent: the screen
	/// that was shown, or a reconnect. When a new group starts, the previous one is written to the log, so the requests
	/// behind a screen can be compared before and after a change.
	/// </summary>
	/// <remarks>
	/// A request is an IQ stanza of type get or set, named by the element it carries. Requests sent within
	/// <see cref="reconnectWindow"/> after connecting are counted as part of the reconnect.
	/// </remarks>
	public sealed partial class XmppRequestCounter : SnifferBase
	{
		/// <summary>
		/// The name of the group of requests sent right after connecting.
		/// </summary>
		public const string ReconnectGroup = "Reconnect";

		private static readonly TimeSpan reconnectWindow = TimeSpan.FromSeconds(20);

		private readonly Func<string> getGroup;
		private readonly Action<string> report;
		private readonly object synchObject = new();
		private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);
		private string? currentGroup;
		private int total;
		private DateTime reconnectUntil = DateTime.MinValue;

		/// <summary>
		/// Counts the requests the app sends to the server, grouped by what the user was doing when they were sent.
		/// </summary>
		/// <param name="GetGroup">Names what the user is doing, such as the screen that is shown.</param>
		/// <param name="Report">Writes the summary of a finished group.</param>
		public XmppRequestCounter(Func<string> GetGroup, Action<string> Report)
			: base("XMPP request counter")
		{
			this.getGroup = GetGroup;
			this.report = Report;
		}

		/// <inheritdoc/>
		public override BinaryPresentationMethod BinaryPresentationMethod => BinaryPresentationMethod.ByteCount;

		/// <summary>
		/// Starts the reconnect group. Call it when the connection to the server is established.
		/// </summary>
		public void MarkConnected()
		{
			lock (this.synchObject)
			{
				this.reconnectUntil = DateTime.UtcNow.Add(reconnectWindow);
				this.StartGroup(ReconnectGroup);
			}
		}

		/// <summary>
		/// Writes the current group to the log and starts counting again.
		/// </summary>
		public void Flush()
		{
			lock (this.synchObject)
			{
				this.StartGroup(this.currentGroup ?? string.Empty);
			}
		}

		/// <inheritdoc/>
		public override Task Process(SnifferTxText Event, CancellationToken Cancel)
		{
			foreach (Match M in RequestPattern().Matches(Event.Text))
			{
				if (!IsRequest(M.Groups["attributes"].Value))
					continue;

				string Name = M.Groups["name"].Value;
				Match Namespace = NamespacePattern().Match(M.Groups["payload"].Value);
				string Key = Namespace.Success ? Name + " [" + Namespace.Groups["ns"].Value + "]" : Name;

				this.Count(Key, Event.Timestamp);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public override Task Process(SnifferRxBinary Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferTxBinary Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferRxText Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferInformation Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferWarning Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferError Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <inheritdoc/>
		public override Task Process(SnifferException Event, CancellationToken Cancel) => Task.CompletedTask;

		/// <summary>
		/// Counts one request in the group it belongs to.
		/// </summary>
		/// <param name="Key">What the request asks for.</param>
		/// <param name="Timestamp">When the request was sent.</param>
		private void Count(string Key, DateTime Timestamp)
		{
			string Group;

			try
			{
				Group = this.getGroup();
			}
			catch (Exception)
			{
				Group = string.Empty;
			}

			lock (this.synchObject)
			{
				if (Timestamp.ToUniversalTime() <= this.reconnectUntil)
					Group = ReconnectGroup;

				if (!string.Equals(Group, this.currentGroup, StringComparison.Ordinal))
					this.StartGroup(Group);

				this.counts.TryGetValue(Key, out int Previous);
				this.counts[Key] = Previous + 1;
				this.total++;
			}
		}

		/// <summary>
		/// Reports the current group, if it has requests, and starts a new one. Called while holding the lock.
		/// </summary>
		/// <param name="Group">The name of the new group.</param>
		private void StartGroup(string Group)
		{
			if (this.total > 0)
			{
				StringBuilder Sb = new();

				Sb.Append("XMPP requests during ");
				Sb.Append(string.IsNullOrEmpty(this.currentGroup) ? "(unknown)" : this.currentGroup);
				Sb.Append(": ");
				Sb.Append(this.total);

				IEnumerable<KeyValuePair<string, int>> Sorted = this.counts
					.OrderByDescending(Entry => Entry.Value)
					.ThenBy(Entry => Entry.Key, StringComparer.Ordinal);

				foreach (KeyValuePair<string, int> P in Sorted)
				{
					Sb.AppendLine();
					Sb.Append("  ");
					Sb.Append(P.Value);
					Sb.Append(" × ");
					Sb.Append(P.Key);
				}

				try
				{
					this.report(Sb.ToString());
				}
				catch (Exception)
				{
					// The count is only for diagnostics, and must never disturb the connection.
				}
			}

			this.counts.Clear();
			this.total = 0;
			this.currentGroup = Group;
		}

		/// <summary>
		/// Checks if an IQ stanza is a request, rather than a response.
		/// </summary>
		/// <param name="Attributes">The attributes of the iq element.</param>
		/// <returns>If the stanza is of type get or set.</returns>
		private static bool IsRequest(string Attributes)
		{
			Match M = TypePattern().Match(Attributes);
			return M.Success && (M.Groups["type"].Value == "get" || M.Groups["type"].Value == "set");
		}

		[GeneratedRegex(@"<iq\b(?<attributes>[^>]*)>\s*<(?:[\w.-]+:)?(?<name>[\w.-]+)(?<payload>[^>]*)>", RegexOptions.CultureInvariant)]
		private static partial Regex RequestPattern();

		[GeneratedRegex(@"\btype\s*=\s*['""](?<type>\w+)['""]", RegexOptions.CultureInvariant)]
		private static partial Regex TypePattern();

		[GeneratedRegex(@"\bxmlns\s*=\s*['""](?<ns>[^'""]+)['""]", RegexOptions.CultureInvariant)]
		private static partial Regex NamespacePattern();
	}
}
