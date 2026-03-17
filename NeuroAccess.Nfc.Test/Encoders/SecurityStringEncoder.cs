using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class SecurityStringEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			SecurityString s = (SecurityString)Object;
			Json.Append(s.ToString());	// Creates invalid JSON, but readable output.
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(SecurityString)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
