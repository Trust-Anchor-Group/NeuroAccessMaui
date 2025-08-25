﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Contains a local reference to a KYC process.
	/// </summary>
	[CollectionName("KycReferences")]
	[Index(nameof(UpdatedUtc), nameof(UpdatedUtc))]
	public class KycReference
	{
		private KycProcess? process;

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// XML describing the KYC process.
		/// </summary>
		public string? KycXml { get; set; }

		/// <summary>
		/// When the reference was created.
		/// </summary>
		public DateTime CreatedUtc { get; set; }

		/// <summary>
		/// When the reference was last updated.
		/// </summary>
		public DateTime UpdatedUtc { get; set; }

		/// <summary>
		/// When the process was last fetched.
		/// </summary>
		public DateTime FetchedUtc { get; set; }

		/// <summary>
		/// Field values in the process.
		/// </summary>
		[DefaultValueNull]
		public KycFieldValue[]? Fields { get; set; }

		/// <summary>
		/// Optional friendly name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string FriendlyName { get; set; } = string.Empty;

		/// <summary>
		/// The legal ID of the created identity (if any).
		/// </summary>
		[DefaultValueNull]
		public string? CreatedIdentityId { get; set; }

		/// <summary>
		/// Last known state of the created identity (if any), for quick status tagging offline.
		/// </summary>
		[DefaultValueNull]
		public IdentityState? CreatedIdentityState { get; set; }

		/// <summary>
		/// Gets a parsed KYC process, populating its fields from the reference.
		/// </summary>
		/// <param name="lang">Optional language.</param>
		/// <returns>The parsed and populated KYC process, or null if XML is missing.</returns>
		public async Task<KycProcess?> GetProcess(string? lang = null)
		{
			if (this.process is null && this.KycXml is not null)
			{
				this.process = await KycProcessParser.LoadProcessAsync(this.KycXml, lang).ConfigureAwait(false);

				if (this.Fields is not null)
				{
					foreach (KycFieldValue Field in this.Fields)
						this.process.Values[Field.FieldId] = Field.Value;

					foreach (KycPage Page in this.process.Pages)
					{
						foreach (ObservableKycField Field in Page.AllFields)
							this.ApplyFieldValue(Field);

						foreach (KycSection Section in Page.AllSections)
							foreach (ObservableKycField Field in Section.AllFields)
								this.ApplyFieldValue(Field);
					}
				}
			}

			return this.process;
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference.
		/// </summary>
		/// <param name="process">KYC process.</param>
		/// <param name="xml">Process XML.</param>
		public void SetProcess(KycProcess process, string xml)
		{
			this.SetProcess(process, xml, DateTime.UtcNow, DateTime.UtcNow);
		}

		/// <summary>
		/// Stores a parsed KYC process into the reference with explicit timestamps.
		/// </summary>
		/// <param name="process">KYC process.</param>
		/// <param name="xml">Process XML.</param>
		/// <param name="created">Created time.</param>
		/// <param name="updated">Updated time.</param>
		public void SetProcess(KycProcess process, string xml, DateTime created, DateTime updated)
		{
			this.process = process;
			this.KycXml = xml;
			this.CreatedUtc = created;
			this.UpdatedUtc = updated;
			this.FetchedUtc = DateTime.UtcNow;
			this.Fields = process.Values.Select(p => new KycFieldValue(p.Key, p.Value)).ToArray();
		}

		public void ApplyFieldValue(ObservableKycField Field)
		{
			if (this.process is null || !this.process.Values.TryGetValue(Field.Id, out string? Val) || Val is null)
				return;
			Field.StringValue = Val;
		}

		// SetFieldValue removed; logic is now in Field.StringValue

		/// <summary>
		/// Creates a KycReference from a KycProcess, serializing its field values.
		/// </summary>
		/// <param name="process">The KYC process to serialize.</param>
		/// <param name="xml">The process XML.</param>
		/// <param name="friendlyName">Optional friendly name.</param>
		/// <returns>A new KycReference instance.</returns>
		public static KycReference FromProcess(KycProcess process, string xml, string? friendlyName = null)
		{
			KycReference Reference = new KycReference
			{
				process = process,
				KycXml = xml,
				CreatedUtc = DateTime.UtcNow,
				UpdatedUtc = DateTime.UtcNow,
				FetchedUtc = DateTime.UtcNow,
				Fields = process.Values.Select(P => new KycFieldValue(P.Key, P.Value)).ToArray(),
				FriendlyName = friendlyName ?? string.Empty
			};
			return Reference;
		}

		/// <summary>
		/// Creates a KycProcess from this reference, populating its fields.
		/// </summary>
		/// <param name="lang">Optional language.</param>
		/// <returns>The populated KycProcess, or null if XML is missing.</returns>
		public async Task<KycProcess?> ToProcess(string? lang = null)
		{
			return await this.GetProcess(lang);
		}
	}
}
