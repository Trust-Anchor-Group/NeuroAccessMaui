using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments.DataObjects
{
	/// <summary>
	/// Number of instances
	/// </summary>
	public class NrInstances : DataObject
	{
		/// <summary>
		/// Number of instances
		/// </summary>
		public NrInstances()
			: base([])
		{
		}

		/// <summary>
		/// Number of instances
		/// </summary>
		/// <param name="Value">Binary value.</param>
		/// <param name="Count">Instance count.</param>
		public NrInstances(byte[] Value, int Count)
			: base(Value)
		{
			this.Count = Count;
		}

		/// <summary>
		/// Tag value.
		/// </summary>
		public override ushort Tag => 0x02;

		/// <summary>
		/// Instance count.
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Tries to parse a binary representation of the data object.
		/// </summary>
		/// <param name="Value">Binary representation</param>
		/// <param name="Client">Client parsing objects.</param>
		/// <param name="Parsed">Parsed object, if successful.</param>
		/// <returns>If value could be parsed.</returns>
		public override bool TryParse(byte[] Value, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject? Parsed)
		{
			int Count;

			switch (Value.Length)
			{
				case 0:
					Count = 0;
					break;

				case 1:
					Count = Value[0];
					break;

				case 2:
					Count = Value[0];
					Count <<= 8;
					Count |= Value[1];
					break;

				default:
					Parsed = null;
					return false;
			}

			Parsed = new NrInstances(Value, Count);
			return true;
		}
	}
}
