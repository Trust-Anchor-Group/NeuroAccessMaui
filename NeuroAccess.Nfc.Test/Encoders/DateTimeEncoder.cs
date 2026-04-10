using System.Text;
using Waher.Content.Json;
using Waher.Content.Xml;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class DateTimeEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			DateTime TP = (DateTime)Object;
			Json.Append(XML.Encode(TP));	// Creates invalid JSON, but readable output.
		}

		public Grade Supports(Type Object)
		{
			return Object == typeof(DateTime) ? Grade.Perfect : Grade.NotAtAll;
		}
	}
}
