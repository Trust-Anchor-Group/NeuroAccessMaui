using System.Text;
using Waher.Content;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class TypeEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			Type T = (Type)Object;
			Json.Append('"');
			Json.Append(JSON.Encode(T.FullName));
			Json.Append('"');
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(Type)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
