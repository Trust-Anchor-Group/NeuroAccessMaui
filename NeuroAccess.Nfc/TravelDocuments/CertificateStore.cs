using NeuroAccess.Nfc.TravelDocuments.RevocationLists;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking;
using Waher.Runtime.Settings;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Internal store of ICAO certificates
	/// </summary>
	public static class CertificateStore
	{
		/// <summary>
		/// Maximum number of days a certificate in the cache is considered valid.
		/// </summary>
		private const int maxDaysInCache = 7;

		/// <summary>
		/// Tries to load an ICAO certificate, provided its country and key reference.
		/// </summary>
		/// <param name="IdDomain">Domain name of Neuron hosting ICAO certificates.</param>
		/// <param name="Country">Country</param>
		/// <param name="KeyReference">Key reference.</param>
		/// <returns>Certificate, if able to load it.</returns>
		public static Task<X509Certificate2?> TryLoadCertificate(string IdDomain, string Country, byte[] KeyReference)
		{
			return TryLoadCertificate(IdDomain, Country, KeyReference, null);
		}

		/// <summary>
		/// Tries to load an ICAO certificate, provided its country and key reference.
		/// </summary>
		/// <param name="IdDomain">Domain name of Neuron hosting ICAO certificates.</param>
		/// <param name="Country">Country</param>
		/// <param name="KeyReference">Key reference.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Certificate, if able to load it.</returns>
		public static async Task<X509Certificate2?> TryLoadCertificate(string IdDomain, string Country, byte[] KeyReference,
			ICommunicationLayer? Client)
		{
			Country = Country.ToUpper(CultureInfo.InvariantCulture);
			string KeyReferenceString = Hashes.BinaryToString(KeyReference).ToUpper(CultureInfo.InvariantCulture);
			X509Certificate2? Certificate;

			string Uri = "https://" + IdDomain + "/IcaoPki/" + Country + "/" + KeyReferenceString + ".cer";

			Client?.Information("Loading certificate from " + Uri);

			ContentResponse? Response = await TryCachedGet(Uri, Client);

			if (Response is null)
				return null;

			try
			{
				Certificate = X509CertificateLoader.LoadCertificate(Response.Encoded);
				return Certificate;
			}
			catch (Exception ex)
			{
				if (Client is null)
					Log.Exception(ex);
				else
					Client.Error(ex.Message);

				return null;
			}
		}

		/// <summary>
		/// Tries to load an ICAO-compliant CRL, provided its URL.
		/// </summary>
		/// <param name="Url">URL to CRL</param>
		/// <returns>Certificate List, if able to load it.</returns>
		public static Task<CertificateList?> TryLoadCrl(string Url)
		{
			return TryLoadCrl(Url, null);
		}

		/// <summary>
		/// Tries to load an ICAO-compliant CRL, provided its URL.
		/// </summary>
		/// <param name="Url">URL to CRL</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Certificate List, if able to load it.</returns>
		public static async Task<CertificateList?> TryLoadCrl(string Url, ICommunicationLayer? Client)
		{
			Client?.Information("Loading CRL from " + Url);

			ContentResponse? Response = await TryCachedGet(Url, Client);

			if (Response is null || Response.Decoded is not CertificateList Crl)
				return null;

			return Crl;
		}

		/// <summary>
		/// Tries to GET content from an URI. If content is cached, it is returned directly.
		/// Otherwise, content is retrieved from the URI and stored in cache for future use.
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <returns>Content, if successulf, null otherwise.</returns>
		public static Task<ContentResponse?> TryCachedGet(string Uri)
		{
			return TryCachedGet(Uri, null);
		}

		/// <summary>
		/// Tries to GET content from an URI. If content is cached, it is returned directly.
		/// Otherwise, content is retrieved from the URI and stored in cache for future use.
		/// </summary>
		/// <param name="Url">URI</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Content, if successulf, null otherwise.</returns>
		public static async Task<ContentResponse?> TryCachedGet(string Url, ICommunicationLayer? Client)
		{
			string UriKey = "Cache.Uri." + Url;
			string TimestampKey = "Cache.Timestamp." + Url;
			string ContentTypeKey = "Cache.ContentType." + Url;
			DateTime Now = DateTime.UtcNow;
			Uri Uri = new(Url);
			ContentResponse Response;

			try
			{
				string Base64 = await RuntimeSettings.GetAsync(UriKey, string.Empty);
				DateTime Timestamp = await RuntimeSettings.GetAsync(TimestampKey, DateTime.MinValue);
				string ContentType = await RuntimeSettings.GetAsync(ContentTypeKey, string.Empty);

				if (!string.IsNullOrEmpty(Base64) &&
					(Now - Timestamp).TotalDays < maxDaysInCache &&
					!string.IsNullOrEmpty(ContentType))
				{
					byte[] Bin = Convert.FromBase64String(Base64);
					Response = await InternetContent.DecodeAsync(ContentType, Bin, Uri);

					if (!Response.HasError)
					{
						Client?.Information("Using cached result for " + Url);

						return Response;
					}
				}
			}
			catch (Exception)
			{
				// Ignore
			}

			Client?.Information("Retrieving " + Url);

			Response = await InternetContent.GetAsync(Uri);

			if (Response.HasError)
			{
				if (Client is null)
					Log.Exception(Response.Error);
				else
					Client.Error(Response.Error.Message);

				return null;
			}

			Client?.Information("Storing content in cache.");

			await RuntimeSettings.SetAsync(UriKey, Convert.ToBase64String(Response.Encoded));
			await RuntimeSettings.SetAsync(TimestampKey, Now);
			await RuntimeSettings.SetAsync(ContentTypeKey, Response.ContentType);

			return Response;
		}
	}
}
