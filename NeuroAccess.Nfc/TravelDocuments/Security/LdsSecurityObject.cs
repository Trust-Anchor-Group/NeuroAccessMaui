using System;
using System.Collections.Generic;
using System.Numerics;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// LDS Security Object V1. Reference: §4.6.2.3, ICAO Doc 9303-10.
	/// </summary>
	public class LdsSecurityObject : SecurityObject
	{
		/// <summary>
		/// LDS Security Object V1. Reference: §4.6.2.3, ICAO Doc 9303-10.
		/// </summary>
		public LdsSecurityObject()
		{
		}

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "2.23.136.1.1.1";

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Array SecurityInfo)
		{
			if (SecurityInfo is null || SecurityInfo.Length < 2)
				return false;

			if (SecurityInfo.GetValue(1) is not ContextSpecific Properties ||
				Properties.Elements.Length < 1 ||
				Properties.Elements.GetValue(0) is not Vector Properties2)
			{
				return false;
			}

			if (Properties2.Elements.Length < 3)
				return false;

			if (Properties2.Elements.GetValue(0) is not BigInteger Version)
				return false;

			if (Properties2.Elements.GetValue(1) is not Vector HashAlgorithms)
				return false;

			if (Properties2.Elements.GetValue(2) is not Vector DataGroupHashValues)
				return false;

			this.Version = (int)Version;

			ChunkedList<HashFunction> HashAlgorithms2 = [];
			Dictionary<int, byte[]> DataGroupHashValues2 = [];

			foreach (object Item in HashAlgorithms.Elements)
			{
				if (Item is null || Item is not HashFunction HashFunction)
					return false;

				HashAlgorithms2.Add(HashFunction);
			}

			foreach (object Item in DataGroupHashValues.Elements)
			{
				if (Item is null ||
					Item is not Vector ItemArray ||
					ItemArray.Elements.Length < 2 ||
					ItemArray.Elements.GetValue(0) is not BigInteger DataGroup ||
					DataGroup < int.MinValue || DataGroup > int.MaxValue ||
					ItemArray.Elements.GetValue(1) is not byte[] Digest)
				{
					return false;
				}

				DataGroupHashValues2[(int)DataGroup] = Digest;
			}

			this.HashAlgorithms = [.. HashAlgorithms2];
			this.DataGroupHashValues = DataGroupHashValues2;

			return true;
		}

		/// <summary>
		/// Version of document.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		/// Hash Algorithm used
		/// </summary>
		public HashFunction[]? HashAlgorithms { get; private set; }

		/// <summary>
		/// Data Group Hash Values.
		/// </summary>
		public Dictionary<int, byte[]>? DataGroupHashValues { get; private set; }
	}
}
