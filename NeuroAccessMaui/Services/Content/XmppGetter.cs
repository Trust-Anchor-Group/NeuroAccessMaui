using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Services.Tag;
using Waher.Content;
using Waher.Content.Xsl;
using Waher.Networking.DNS;
using Waher.Networking.DNS.Enumerations;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP.PubSub;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;
using System.Xml;
using System.Xml.Schema;
using Waher.Networking.DNS.Communication;

namespace NeuroAccessMaui.Services.Content
{
	/// <summary>
	/// An IContentGetter that handles XMPP URIs of the form "xmpp:node@domain/resource".
	/// Example: xmpp:foo@pubsub.lab.tagroot.io/ResourceId
	/// </summary>
	public class XmppGetter : IContentGetter
	{
		/// <summary>
		/// Supported URI schemes for this getter.
		/// </summary>
		public string[] UriSchemes => new string[] { "xmpp" };

		/// <summary>
		/// If the getter is able to get a resource, given its URI.
		/// </summary>
		public bool CanGet(Uri uri, out Grade grade)
		{
			if (uri.Scheme.Equals("xmpp", StringComparison.OrdinalIgnoreCase))
			{
				grade = Grade.Ok;
				return true;
			}
			else
			{
				grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Gets a resource from an XMPP URI.
		/// </summary>
		public Task<ContentResponse> GetAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			params KeyValuePair<string, string>[] headers)
		{
			return this.GetAsync(uri, certificate, remoteCertificateValidator, 60000, headers);
		}

		/// <summary>
		/// Gets a resource from an XMPP URI, with timeout.
		/// </summary>
		public async Task<ContentResponse> GetAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			int timeoutMs,
			params KeyValuePair<string, string>[] headers)
		{
			//
			// 1. Parse the URI into node, domain, and resource.
			//    Format expected: xmpp:node@domain/resource
			//
			string Full = uri.OriginalString;      // e.g. "xmpp:foo@pubsub.lab.tagroot.io/ResourceId"
			string WithoutScheme = Full.Substring(uri.Scheme.Length + 1);

			// Split on the first '/' to separate "node@domain" from "resource"
			string NodeAtDomain;
			string Resource;
			int SlashIndex = WithoutScheme.IndexOf('/');
			if (SlashIndex < 0)
			{
				NodeAtDomain = WithoutScheme;
				Resource = string.Empty;
			}
			else
			{
				NodeAtDomain = WithoutScheme.Substring(0, SlashIndex);
				Resource = WithoutScheme.Substring(SlashIndex + 1);
			}

			// Split node@domain into node and domain
			string Node, Domain;
			int AtIndex = NodeAtDomain.IndexOf('@');
			if (AtIndex < 0)
			{
				// If there is no '@', treat entire left part as domain, leave node empty.
				Node = string.Empty;
				Domain = NodeAtDomain;
			}
			else
			{
				Node = NodeAtDomain.Substring(0, AtIndex);
				Domain = NodeAtDomain.Substring(AtIndex + 1);
			}
			/*
			// DNS LOOKUPS To determine if it is an xmpp component or a server account:
			// Currently disabled, as Androids MONO runtime is missing a correct implemention of unix getaddrinfo(),
			// 
			// 
			// 2. DNS SRV Lookup for _xmpp-server._tcp.DOMAIN
			//
			string srvName = $"_xmpp-server._tcp.{domain}";
			ResourceRecord[] records;
			try
			{
				records = await DnsResolver.Resolve(srvName, QTYPE.SRV, QCLASS.IN);
			}
			catch (Exception dnsEx)
			{
				// Unable to resolve SRV records
				return new ContentResponse(dnsEx);
			}

			// Filter for SRV entries
			SRV[] SrvRecords = records
				.OfType<SRV>()
				.OrderBy(r => r.Priority)
				.ThenByDescending(r => r.Weight)
				.ToArray();

			if (SrvRecords.Length == 0)
			{
				// No SRV records found�cannot locate XMPP server
				InvalidOperationException ex = new InvalidOperationException($"No SRV records found for {srvName}");
				return new ContentResponse(ex);
			}
			
			// Pick the first SRV record (lowest priority, highest weight)
			SRV chosenSrv = SrvRecords[0];
			string xmppHost = chosenSrv.TargetHost; // e.g. "xmpp1.pubsub.lab.tagroot.io"
			int xmppPort = chosenSrv.Port;          // typically 5222
			*/
			bool IsComponent = true;//!xmppHost.Equals(domain, StringComparison.OrdinalIgnoreCase);

			//
			// 3. If this target is a PubSub component (i.e. SRV target differs from domain),
			//    fetch the PubSub item (and validate its XML payload). Otherwise, treat as account JID.
			//
			if (IsComponent)
			{
				// Ensure we have a TagProfile to compare against
				ITagProfile TagProfile = ServiceRef.TagProfile;
				if (TagProfile is null)
				{
					return new ContentResponse(
						new InvalidOperationException("TagProfile service is not available."));
				}

				// We expect the "domain" part of the URI to match the PubSub JID in the TagProfile.
				if (!Domain.Equals(TagProfile.PubSubJid, StringComparison.OrdinalIgnoreCase))
				{
					return new ContentResponse(
						new InvalidOperationException($"Domain '{Domain}' is not the configured PubSub JID."));
				}

				// Try fetching the item from the PubSub service:
				PubSubItem? Item;
				try
				{
					Item = await ServiceRef.XmppService.GetItemAsync(Node, Resource);

					// ServiceRef.XmppService.GetItemAsync(node, resource) should connect,
					// authenticate, discover the pubsub component (domain) and retrieve the item.
					if (await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
						Item = await ServiceRef.XmppService.GetItemAsync(Node, Resource);
					else
						throw new InvalidOperationException("XMPP service is not connected. Cannot fetch PubSub item.");
				}
				catch (Exception ex)
				{
					return new ContentResponse(ex);
				}

				if (Item is null)
				{
					return new ContentResponse(
						new InvalidOperationException($"No PubSub item found for node '{Node}' and ID '{Resource}'."));
				}

				// The payload is returned as XML string
				string ItemXml = Item.Item.InnerXml ?? string.Empty;
				byte[] ItemBytes = Encoding.UTF8.GetBytes(Item.Item.InnerXml ?? string.Empty);

				// Validate that the XML is well-formed
				XmlDocument XmlDoc = new XmlDocument();
				try
				{
					XmlDoc.LoadXml(ItemXml);
				}
				catch (XmlException XmlEx)
				{
					return new ContentResponse(
						new InvalidOperationException("PubSub payload is not well-formed XML.", XmlEx));
				}


				//  If valid, return the XML payload
				//     ContentType = "application/xml"; Decoded = itemXml; Encoded = itemBytes
				return new ContentResponse("application/xml", ItemXml, ItemBytes);
			}
			else
			{
				//
				// 4. The SRV target equals the domain: treat this as a user/account JID.
				//    For example: xmpp:username@domain/ResourceId might map to presence, vCard, etc.
				//    Here, we leave a stub for future expansion.
				//
				return new ContentResponse(
					new NotSupportedException($"Fetching for account JID '{Node}@{Domain}' is not implemented."));
			}
		}

		/// <summary>
		/// Gets a (possibly large) resource from an XMPP URI, writing it to a temporary stream.
		/// </summary>
		public async Task<ContentStreamResponse> GetTempStreamAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			params KeyValuePair<string, string>[] headers)
		{
			ContentResponse Response = await this.GetAsync(uri, certificate, remoteCertificateValidator, headers);
			if (Response.Error is not null)
				return new ContentStreamResponse(Response.Error);

			TemporaryStream Temp = new TemporaryStream();
			await Temp.WriteAsync(Response.Encoded, 0, Response.Encoded.Length);
			Temp.Position = 0;
			return new ContentStreamResponse(Response.ContentType, Temp);
		}

		/// <summary>
		/// Gets a (possibly large) resource from an XMPP URI, writing it to a temporary stream, with timeout.
		/// </summary>
		public async Task<ContentStreamResponse> GetTempStreamAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			int timeoutMs,
			params KeyValuePair<string, string>[] headers)
		{
			ContentResponse Response = await this.GetAsync(uri, certificate, remoteCertificateValidator, timeoutMs, headers);
			if (Response.Error is not null)
				return new ContentStreamResponse(Response.Error);

			TemporaryStream Temp = new TemporaryStream();
			await Temp.WriteAsync(Response.Encoded, 0, Response.Encoded.Length);
			Temp.Position = 0;
			return new ContentStreamResponse(Response.ContentType, Temp);
		}

		/// <summary>
		/// Gets a (possibly large) resource from an XMPP URI, writing it into the provided TemporaryStream.
		/// </summary>
		Task<ContentStreamResponse> IContentGetter.GetTempStreamAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			TemporaryStream destination,
			params KeyValuePair<string, string>[] headers)
		{
			// We ignore the supplied 'destination' since PubSub payloads are usually small.
			// In a real streaming scenario, you'd write directly into 'destination'.
			return this.GetTempStreamAsync(uri, certificate, remoteCertificateValidator, headers);
		}

		/// <summary>
		/// Gets a (possibly large) resource from an XMPP URI, writing it into the provided TemporaryStream, with timeout.
		/// </summary>
		Task<ContentStreamResponse> IContentGetter.GetTempStreamAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			int timeoutMs,
			params KeyValuePair<string, string>[] headers)
		{
			return this.GetTempStreamAsync(uri, certificate, remoteCertificateValidator, timeoutMs, headers);
		}

		/// <summary>
		/// Gets a (possibly large) resource from an XMPP URI, writing it into the provided TemporaryStream, with timeout.
		/// </summary>
		Task<ContentStreamResponse> IContentGetter.GetTempStreamAsync(
			Uri uri,
			X509Certificate certificate,
			EventHandler<RemoteCertificateEventArgs> remoteCertificateValidator,
			int timeoutMs,
			TemporaryStream destination,
			params KeyValuePair<string, string>[] headers)
		{
			// We ignore 'destination' here as well.
			return this.GetTempStreamAsync(uri, certificate, remoteCertificateValidator, timeoutMs, headers);
		}
	}
}
