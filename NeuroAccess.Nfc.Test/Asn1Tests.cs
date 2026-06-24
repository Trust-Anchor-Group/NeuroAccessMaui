using NeuroAccess.Nfc.TravelDocuments;
using Waher.Content;
using Waher.Security;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class Asn1Tests
	{
		[TestMethod]
		[DataRow("3081d406072a8648ce3d02013081c8020101302806072a8648ce3d0101021d00d7c134aa264366862a18302575d1d787b09f075797da89f57ec8c0ff303c041c68a5e62ca9ce6c1c299803a6c1530b514e182ad8b0042a59cad29f43041c2580f63ccfe44138870713b1a92369e33e2135d266dbb372386c400b0439040d9029ad2c7e5cf4340823b2a87dc68c9e4ce3174c1e6efdee12c07d58aa56f772c0726f24c6b89e4ecdac24354b9e99caa3f6d3761402cd021d00d7c134aa264366862a18302575d0fb98d116bc4b6ddebca3a5a7939f020101")]
		public void Test_01_TryParse(string Hex)
		{
			byte[] Bin = Hashes.StringToBinary(Hex);

			Assert.IsTrue(ASN1.TryDecodeDer(Bin, out object? Obj));
			Assert.IsNotNull(Obj);

			Console.Out.WriteLine(JSON.Encode(Obj, true));
		}
	}
}
