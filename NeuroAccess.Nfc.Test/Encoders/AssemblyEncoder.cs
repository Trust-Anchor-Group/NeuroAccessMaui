using System.Reflection;
using System.Text;
using Waher.Content;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class AssemblyEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			Assembly A = (Assembly)Object;
			Json.Append('"');
			Json.Append(JSON.Encode(A.FullName));
			Json.Append('"');
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(Assembly)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
