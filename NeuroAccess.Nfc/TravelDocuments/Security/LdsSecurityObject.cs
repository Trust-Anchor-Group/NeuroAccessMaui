using System.Collections.Generic;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// LDS Security Object V1. Reference: §4.6.2.3, ICAO Doc 9303-10.
	/// </summary>
	public class LdsSecurityObject : SecurityObject
	{
		private bool configured;

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
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configured;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length < 3)
				return false;

			if (SecurityInfo.FirstElement is not System.Numerics.BigInteger Version)
				return false;

			ChunkedList<HashFunction> HashFunctions2 = [];

			if (SecurityInfo[1] is HashFunction HashFunction)
				HashFunctions2.Add(HashFunction);
			else if (SecurityInfo[1] is Vector HashFunctions)
			{
				foreach (object Item in HashFunctions)
				{
					if (Item is null || Item is not HashFunction HashFunction2)
						return false;

					HashFunctions2.Add(HashFunction2);
				}
			}
			else
				return false;

			if (SecurityInfo[2] is not Vector DataGroupHashValues)
				return false;

			this.Version = (int)Version;

			Dictionary<int, byte[]> DataGroupHashValues2 = [];

			foreach (object Item in DataGroupHashValues)
			{
				if (Item is null ||
					Item is not Vector ItemArray ||
					ItemArray.Length < 2 ||
					ItemArray.FirstElement is not System.Numerics.BigInteger DataGroup ||
					DataGroup < int.MinValue || DataGroup > int.MaxValue ||
					ItemArray[1] is not byte[] Digest)
				{
					return false;
				}

				DataGroupHashValues2[(int)DataGroup] = Digest;
			}

			this.HashFunctions = [.. HashFunctions2];
			this.DataGroupHashValues = DataGroupHashValues2;
			this.configured = true;

			return true;
		}

		/// <summary>
		/// Version of document.
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		/// Hash Functions used
		/// </summary>
		public HashFunction[]? HashFunctions { get; private set; }

		/// <summary>
		/// Data Group Hash Values.
		/// </summary>
		public Dictionary<int, byte[]>? DataGroupHashValues { get; private set; }
	}
}
