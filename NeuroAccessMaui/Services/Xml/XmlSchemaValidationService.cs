using System.Xml;
using System.Xml.Schema;
using Waher.Content.Xsl;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Xml
{
	/// <summary>
	/// Default implementation of <see cref="IXmlSchemaValidationService"/>.
	/// </summary>
	[Singleton]
	public sealed class XmlSchemaValidationService : IXmlSchemaValidationService
	{
		private sealed class SchemaEntry
		{
			public string Path = string.Empty;
			public XmlSchema? Schema;
			public bool LoadAttempted;
		}

		private readonly Dictionary<string, SchemaEntry> schemas = new(StringComparer.OrdinalIgnoreCase);
		private readonly object gate = new();

		public void RegisterSchema(string Key, string RelativePath)
		{
			if (string.IsNullOrWhiteSpace(Key)) throw new ArgumentNullException(nameof(Key));
			if (string.IsNullOrWhiteSpace(RelativePath)) throw new ArgumentNullException(nameof(RelativePath));

			lock (this.gate)
			{
				if (!this.schemas.TryGetValue(Key, out SchemaEntry? Entry))
				{
					Entry = new SchemaEntry();
					this.schemas[Key] = Entry;
				}
				Entry.Path = RelativePath;
				Entry.LoadAttempted = false; // allow re-registration override
				Entry.Schema = null;
			}
			ServiceRef.LogService.LogDebug("XmlSchemaRegistered", new KeyValuePair<string, object?>("Key", Key), new KeyValuePair<string, object?>("Path", RelativePath));
		}

		public bool IsRegistered(string Key)
		{
			lock (this.gate) return this.schemas.ContainsKey(Key);
		}

		public async Task<bool> ValidateAsync(string Key, string Xml, CancellationToken CancellationToken = default)
		{
			if (Xml is null) throw new ArgumentNullException(nameof(Xml));
			XmlSchema? Schema = await this.GetOrLoadAsync(Key, CancellationToken).ConfigureAwait(false);
			if (Schema is null)
			{
				// Non-fatal: Schema missing or failed to load; treat as valid to allow fallback logic in caller.
				return true;
			}
			try
			{
				XmlDocument Doc = new() { XmlResolver = null };
				using StringReader StringReader = new(Xml);
				using XmlReader Reader = XmlReader.Create(StringReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
				Doc.Load(Reader);
				XSL.Validate(Key, Doc, Schema);
				return true;
			}
			catch (XmlSchemaException Ex)
			{
				ServiceRef.LogService.LogWarning("XmlValidationFailed",
					new KeyValuePair<string, object?>("Key", Key),
					new KeyValuePair<string, object?>("Line", Ex.LineNumber),
					new KeyValuePair<string, object?>("Pos", Ex.LinePosition),
					new KeyValuePair<string, object?>("Message", Ex.Message));
				return false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex,
					new KeyValuePair<string, object?>("Operation", "XmlValidationUnexpected"),
					new KeyValuePair<string, object?>("Key", Key));
				return false;
			}
		}

		private async Task<XmlSchema?> GetOrLoadAsync(string Key, CancellationToken CancellationToken)
		{
			SchemaEntry? Entry;
			lock (this.gate)
			{
				if (!this.schemas.TryGetValue(Key, out Entry))
				{
					ServiceRef.LogService.LogWarning("XmlSchemaKeyNotRegistered", new KeyValuePair<string, object?>("Key", Key));
					return null;
				}
				if (Entry.Schema is not null)
					return Entry.Schema;
				if (Entry.LoadAttempted)
					return Entry.Schema; // failed earlier
				Entry.LoadAttempted = true;
			}
			try
			{
				using Stream Stream = await FileSystem.OpenAppPackageFileAsync(Entry.Path).ConfigureAwait(false);
				using Stream Clone = new MemoryStream();
				await Stream.CopyToAsync(Clone, CancellationToken).ConfigureAwait(false);
				Clone.Position = 0;
				XmlSchema Schema = XSL.LoadSchema(Clone, Entry.Path);
				lock (this.gate) Entry.Schema = Schema;
				ServiceRef.LogService.LogDebug("XmlSchemaLoaded",
					new KeyValuePair<string, object?>("Key", Key));
				return Schema;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex,
					new KeyValuePair<string, object?>("Operation", "XmlSchemaLoad"),
					new KeyValuePair<string, object?>("Key", Key));
				return null;
			}
		}
	}
}
