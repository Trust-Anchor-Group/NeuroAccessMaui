using System.Collections.Generic;

namespace NeuroAccess.Nfc.TravelDocuments.Security.FieldTypes
{
	/// <summary>
	/// Abstract base class for field types.
	/// </summary>
	public abstract class FieldType : SecurityObject
	{
		/// <summary>
		/// Gets parsed parameters from the field type definition, if available.
		/// </summary>
		/// <param name="Parameters">Dictionary to receive parsed parameters.</param>
		public abstract void GetParsedParameters(Dictionary<string, object?> Parameters);
	}
}
