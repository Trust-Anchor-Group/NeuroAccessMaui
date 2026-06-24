using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class SecurityBinaryEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			SecurityBinary s = (SecurityBinary)Object;
			Json.Append(s.ToString());	// Creates invalid JSON, but readable output.
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(SecurityBinary)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
