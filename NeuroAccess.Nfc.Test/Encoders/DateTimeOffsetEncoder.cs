using System.Text;
using Waher.Content.Json;
using Waher.Content.Xml;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class DateTimeOffsetEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			DateTimeOffset TP = (DateTimeOffset)Object;
			Json.Append(XML.Encode(TP));	// Creates invalid JSON, but readable output.
		}

		public Grade Supports(Type Object)
		{
			return Object == typeof(DateTimeOffset) ? Grade.Perfect : Grade.NotAtAll;
		}
	}
}
