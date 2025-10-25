namespace NeuroAccessMaui.Services.Xmpp
{
	using System;
	using Waher.Networking.XMPP.PubSub;
	using Waher.Networking.XMPP.ResultSetManagement;

	/// <summary>
	/// Represents a page of PubSub items along with associated pagination metadata.
	/// </summary>
	public sealed class PubSubPageResult
	{
		public PubSubPageResult(string nodeId, PubSubItem[] items, ResultPage? page)
		{
			this.NodeId = nodeId;
			this.Items = items ?? Array.Empty<PubSubItem>();
			this.ResultPage = page;
		}

		public string NodeId { get; }

		public PubSubItem[] Items { get; }

		public ResultPage? ResultPage { get; }

		public bool HasItems => this.Items.Length > 0;

		public string? FirstItemId => this.ResultPage?.First;

		public string? LastItemId => this.ResultPage?.Last;

		public int? TotalCount => this.ResultPage?.Count;

		public int? FirstIndex => this.ResultPage?.FirstIndex;
	}
}
