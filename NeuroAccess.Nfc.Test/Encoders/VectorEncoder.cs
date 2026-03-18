using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class VectorEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			Vector V = (Vector)Object;
			JSON.Encode(V.Elements, Indent, Json);
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(Vector)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
