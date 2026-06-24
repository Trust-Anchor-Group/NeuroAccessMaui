using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Waher.Content;
using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Test.Encoders
{
	public class DelegateEncoder : IJsonEncoder
	{
		public void Encode(object Object, int? Indent, StringBuilder Json)
		{
			// Creates invalid JSON, but readable output.

			Delegate D = (Delegate)Object;
			bool First = true;

			if (D.Target is not null)
			{
				Json.Append(D.Target.ToString());
				Json.Append('.');
			}
			Json.Append(D.Method.Name);
			Json.Append('(');

			foreach (ParameterInfo Arg in D.Method.GetParameters())
			{
				if (First)
					First = false;
				else
					Json.Append(',');

				Json.Append(Arg.Name);
			}

			Json.Append(')');
		}

		public Grade Supports(Type Object)
		{
			return Object.IsAssignableTo(typeof(Delegate)) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
