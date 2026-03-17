using Waher.Runtime.Collections;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// List of other names. 
	/// </summary>
	public class OtherNames : NestedDataObject
	{
		/// <summary>
		/// List of other names. 
		/// </summary>
		public OtherNames()
			: base([])
		{
		}

		/// <summary>
		/// List of other names. 
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="NrInstances">Number of other names reported by the card.</param>
		/// <param name="OtherNames">Actual other names reported.</param>
		public OtherNames(byte[] Value, int NrInstances, string[] OtherNames)
			: base(Value)
		{
			this.NrInstances = NrInstances;
			this.Names = OtherNames;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0xa0;

		/// <summary>
		/// Number of other names reported by the card.
		/// </summary>
		public int NrInstances { get; }

		/// <summary>
		/// Actual other names reported.
		/// </summary>
		public string[]? Names { get; }

		/// <summary>
		/// Creates a parsed instance of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Inner">Parsed content.</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <returns>New data object instance.</returns>
		public override IDataObject Create(byte[] Value, IDataObject[] Inner, TravelDocumentsClient Client)
		{
			NumberOfInstances? NrInstances = null;
			ChunkedList<string> OtherNames = [];

			foreach (IDataObject Object in Inner)
			{
				if (Object is NumberOfInstances NumberOfInstances2)
					NrInstances = NumberOfInstances2;
				else if (Object is OtherName OtherName)
					OtherNames.Add(OtherName.StringValue ?? string.Empty);
			}

			return new OtherNames(Value, NrInstances?.Count ?? 0, [.. OtherNames]);
		}
	}
}
