using NeuroAccess.Nfc.TravelDocuments.RevocationLists;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking;
using Waher.Runtime.Settings;
using Waher.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Binary parser.
	/// </summary>
	/// <typeparam name="T">Type to parse to.</typeparam>
	/// <param name="Binary">Binary data to parse.</param>
	/// <param name="Parsed">Parsed result.</param>
	/// <returns>True if parsing was successful, false otherwise.</returns>
	public delegate bool BinaryParser<T>(byte[] Binary, [NotNullWhen(true)] out T? Parsed);

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
		public static Task<Certificate?> TryLoadCertificate(string IdDomain, string Country, byte[] KeyReference)
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
		public static async Task<Certificate?> TryLoadCertificate(string IdDomain, string Country, byte[] KeyReference,
			ICommunicationLayer? Client)
		{
			Country = Country.ToUpper(CultureInfo.InvariantCulture);
			string KeyReferenceString = Hashes.BinaryToString(KeyReference).ToUpper(CultureInfo.InvariantCulture);

			string Uri = "https://" + IdDomain + "/IcaoPki/" + Country + "/" + KeyReferenceString + ".cer";

			Client?.Information("Loading certificate from " + Uri);

			return await TryCachedGet<Certificate>(Uri, Client, Certificate.TryParse);
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
			return await TryCachedGet<CertificateList>(Url, Client, CertificateList.TryParse);
		}

		/// <summary>
		/// Tries to GET content from an URI. If content is cached, it is returned directly.
		/// Otherwise, content is retrieved from the URI and stored in cache for future use.
		/// </summary>
		/// <typeparam name="T">Type of parsed content.</typeparam>
		/// <param name="Uri">URI</param>
		/// <returns>Parsed Content, if successulf, null otherwise.</returns>
		public static Task<T?> TryCachedGet<T>(string Uri)
			where T : class
		{
			return TryCachedGet<T>(Uri, null);
		}

		/// <summary>
		/// Tries to GET content from an URI. If content is cached, it is returned directly.
		/// Otherwise, content is retrieved from the URI and stored in cache for future use.
		/// </summary>
		/// <typeparam name="T">Type of parsed content.</typeparam>
		/// <param name="Url">URI</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>Parsed Content, if successulf, null otherwise.</returns>
		public static Task<T?> TryCachedGet<T>(string Url, ICommunicationLayer? Client)
			where T : class
		{
			return TryCachedGet<T>(Url, Client, null);
		}

		private static async Task<T?> TryCachedGet<T>(string Url, ICommunicationLayer? Client,
			BinaryParser<T>? ParseBinary)
			where T : class
		{
			string UriKey = "Cache.Uri." + Url;
			string TimestampKey = "Cache.Timestamp." + Url;
			string ContentTypeKey = "Cache.ContentType." + Url;
			DateTime Now = DateTime.UtcNow;
			Uri Uri = new(Url);
			ContentResponse Response;
			T? Result;

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
						Result = Response.Decoded as T;

						if (Result is not null ||
							(ParseBinary is not null && ParseBinary(Response.Encoded, out Result)))
						{
							Client?.Information("Using cached result for " + Url);
							return Result;
						}

						Client?.Error("Cached content could not be parsed.");
					}

					if (ParseBinary is not null)
					{
						Client?.Information("Removing incorrect cache item.");
						await RuntimeSettings.SetAsync(UriKey, string.Empty);
					}
				}
			}
			catch (Exception ex)
			{
				if (Client is null)
					Log.Exception(ex);
				else
					Client?.Error(ex.Message);
			}

			Client?.Information("Retrieving " + Url);

			Response = await InternetContent.GetAsync(Uri);

			if (Response.HasError)
			{
				if (Client is null)
					Log.Exception(Response.Error);
				else
					Client?.Error(Response.Error.Message);

				return default;
			}

			if (Response.Decoded is T Parsed3)
				Result = Parsed3;
			else if (ParseBinary is not null && !ParseBinary(Response.Encoded, out T? Parsed4))
				Result = Parsed4;
			else
				return default;

			Client?.Information("Storing content in cache.");

			await RuntimeSettings.SetAsync(UriKey, Convert.ToBase64String(Response.Encoded));
			await RuntimeSettings.SetAsync(TimestampKey, Now);
			await RuntimeSettings.SetAsync(ContentTypeKey, Response.ContentType);

			return Result;
		}
	}
}
