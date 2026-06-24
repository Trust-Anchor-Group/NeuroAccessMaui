using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class HashFunctionEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			HashFunction H = (HashFunction)Object;
			Json.Append(H.ToString());	// Creates invalid JSON, but readable output.
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(HashFunction)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
