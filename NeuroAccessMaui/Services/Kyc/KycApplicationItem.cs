namespace NeuroAccessMaui.Services.Kyc
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Xml;
	using Waher.Networking.XMPP.PubSub;

	/// <summary>
	/// Represents a KYC application template retrieved from PubSub storage.
	/// </summary>
	public sealed class KycApplicationItem
	{
		private const string XmlLanguageNamespace = "http://www.w3.org/XML/1998/namespace";
		private readonly IReadOnlyDictionary<string, string> localizedNames;
		private readonly string? primaryDisplayName;

		private KycApplicationItem(
			string nodeId,
			string itemId,
			string processXml,
			string? serviceAddress,
			string? publisher,
			DateTime? published,
			IReadOnlyDictionary<string, string> localizedNames,
			string? primaryDisplayName)
		{
			this.NodeId = nodeId;
			this.ItemId = itemId;
			this.ProcessXml = processXml;
			this.ServiceAddress = serviceAddress;
			this.Publisher = publisher;
			this.Published = published;
			this.localizedNames = localizedNames;
			this.primaryDisplayName = primaryDisplayName;
		}

		/// <summary>
		/// Gets the PubSub node identifier that produced the item.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets the PubSub item identifier.
		/// </summary>
		public string ItemId { get; }

		/// <summary>
		/// Gets the raw KYC process XML payload.
		/// </summary>
		public string ProcessXml { get; }

		/// <summary>
		/// Gets the service address that hosted the node, if available.
		/// </summary>
		public string? ServiceAddress { get; }

		/// <summary>
		/// Gets the publisher that submitted the item, if available.
		/// </summary>
		public string? Publisher { get; }

		/// <summary>
		/// Gets the publish timestamp, if available.
		/// </summary>
		public DateTime? Published { get; }

		/// <summary>
		/// Gets localized display names extracted from the payload.
		/// </summary>
		public IReadOnlyDictionary<string, string> LocalizedNames => this.localizedNames;

		/// <summary>
		/// Gets a localized display name using the current UI culture as the preference.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrEmpty(this.primaryDisplayName))
				{
					return this.primaryDisplayName;
				}
				string Preferred = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				string? Value = this.GetLocalizedName(Preferred);
				return Value ?? string.Empty;
			}
		}

		/// <summary>
		/// Gets the first display name found in the payload, if available.
		/// </summary>
		public string? PrimaryDisplayName => this.primaryDisplayName;

		/// <summary>
		/// Attempts to create an instance from a PubSub item.
		/// </summary>
		/// <param name="Item">PubSub item.</param>
		/// <param name="Application">Resulting application instance.</param>
		/// <returns>True if an application could be created.</returns>
		public static bool TryCreate(PubSubItem Item, out KycApplicationItem? Application)
		{
			Application = null;
			if (Item is null)
				return false;

			string? ItemId = Item.ItemId;
			if (string.IsNullOrWhiteSpace(ItemId))
				return false;

				XmlElement? Payload = Item.Item;
				if (Payload is null)
				{
					string? PayloadXml = Item.Payload;
					if (string.IsNullOrWhiteSpace(PayloadXml))
						return false;

					try
					{
						XmlDocument Document = new XmlDocument();
						Document.LoadXml(PayloadXml);
						Payload = Document.DocumentElement;
					}
					catch (XmlException)
					{
						return false;
					}
				}

					if (Payload is null)
						return false;

					XmlElement? PayloadElement = NormalizePayload(Payload);
					if (PayloadElement is null)
						return false;

					string ProcessXml = PayloadElement.OuterXml;
				if (string.IsNullOrWhiteSpace(ProcessXml))
					return false;

				Dictionary<string, string> NamesDictionary = ExtractLocalizedNames(PayloadElement, out string? PrimaryName);
				ReadOnlyDictionary<string, string> Names = new ReadOnlyDictionary<string, string>(NamesDictionary);

			string NodeId = Item.Node ?? string.Empty;
			string? ServiceAddress = Item.ServiceAddress;
			string? Publisher = Item.Publisher;
			DateTime? Published = null;

				Application = new KycApplicationItem(NodeId, ItemId, ProcessXml, ServiceAddress, Publisher, Published, Names, PrimaryName);
				return true;
			}

		/// <summary>
		/// Converts a collection of PubSub items to KYC application descriptors.
		/// </summary>
		/// <param name="Items">PubSub items to convert.</param>
		/// <returns>List of successfully parsed application descriptors.</returns>
		public static IReadOnlyList<KycApplicationItem> CreateMany(IEnumerable<PubSubItem> Items)
		{
			List<KycApplicationItem> Results = new List<KycApplicationItem>();
			foreach (PubSubItem Item in Items)
			{
				if (Item is null)
					continue;

				if (TryCreate(Item, out KycApplicationItem? Application) && Application is not null)
					Results.Add(Application);
			}

			return Results;
		}

		/// <summary>
		/// Retrieves a localized name for the provided language code.
		/// </summary>
		/// <param name="Language">Language code (two-letter, e.g. "en").</param>
		/// <returns>Localized name if available.</returns>
		public string? GetLocalizedName(string? Language)
		{
			if (Language is not null && this.localizedNames.TryGetValue(Language, out string? Value))
				return Value;

			if (this.localizedNames.TryGetValue("en", out string? English))
				return English;

			foreach (KeyValuePair<string, string> Pair in this.localizedNames)
				return Pair.Value;

			return null;
		}

		private static XmlElement? NormalizePayload(XmlElement Payload)
		{
			if (Payload.LocalName == "item" && Payload.NamespaceURI == PubSubClient.NamespacePubSub)
			{
				foreach (XmlNode Child in Payload.ChildNodes)
				{
					if (Child is XmlElement Element)
						return Element;
				}

				return null;
			}

			return Payload;
		}

		private static Dictionary<string, string> ExtractLocalizedNames(XmlElement Payload, out string? primaryName)
		{
			Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			primaryName = null;
			XmlElement? NameElement = SelectChild(Payload, "Name");
			if (NameElement is null)
				return Names;

			foreach (XmlNode Child in NameElement.ChildNodes)
			{
				if (Child is not XmlElement TextElement)
					continue;

				if (!string.Equals(TextElement.LocalName, "Text", StringComparison.Ordinal))
					continue;

				string Language = TextElement.GetAttribute("lang", XmlLanguageNamespace);
				if (string.IsNullOrWhiteSpace(Language))
					Language = "und";

				string Value = TextElement.InnerText;
				if (string.IsNullOrWhiteSpace(Value))
					continue;

				Names[Language] = Value.Trim();
				if (primaryName is null)
					primaryName = Value.Trim();
			}

			return Names;
		}

		private static XmlElement? SelectChild(XmlElement Parent, string LocalName, string? NamespaceUri = null)
		{
			foreach (XmlNode Child in Parent.ChildNodes)
			{
				if (Child is not XmlElement Candidate)
					continue;

				if (!string.Equals(Candidate.LocalName, LocalName, StringComparison.Ordinal))
					continue;

				if (NamespaceUri is not null && !string.Equals(Candidate.NamespaceURI, NamespaceUri, StringComparison.Ordinal))
					continue;

				return Candidate;
			}

			return null;
		}
	}
}
