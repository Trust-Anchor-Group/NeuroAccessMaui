using NeuroAccess.Nfc.Extensions.PACE;
using Waher.Security;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class CMacTests
	{
		// CMAC test vectors from:
		// https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Standards-and-Guidelines/documents/examples/AES_CMAC.pdf

		[TestMethod]
		[DataRow(   // CMAC-AES128, Example #1
			"2B7E1516 28AED2A6 ABF71588 09CF4F3C",
			"",
			"BB1D6929 E9593728 7FA37D12 9B756746")]
		[DataRow(   // CMAC-AES128, Example #2
			"2B7E1516 28AED2A6 ABF71588 09CF4F3C",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A",
			"070A16B4 6B4D4144 F79BDD9D D04A287C")]
		[DataRow(   // CMAC-AES128, Example #3
			"2B7E1516 28AED2A6 ABF71588 09CF4F3C",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57",
			"7D85449E A6EA19C8 23A7BF78 837DFADE")]
		[DataRow(   // CMAC-AES128, Example #4
			"2B7E1516 28AED2A6 ABF71588 09CF4F3C",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57 1E03AC9C 9EB76FAC 45AF8E51 30C81C46 A35CE411 E5FBC119 1A0A52EF F69F2445 DF4F9B17 AD2B417B E66C3710",
			"51F0BEBF 7E3B9D92 FC497417 79363CFE")]
		public void Test_01_CMAC_AES128(string Key, string Message, string Signature)
		{
			using CMac Cipher = CMac.CreateAes128CMac(Hashes.StringToBinary(Key));
			byte[] Mac = Cipher.Sign(Hashes.StringToBinary(Message));

			Assert.IsTrue(Cipher.Verify(Hashes.StringToBinary(Message), Mac));

			Assert.AreEqual(Signature.Replace(" ", string.Empty),
				Hashes.BinaryToString(Mac).ToUpperInvariant());
		}

		[TestMethod]
		[DataRow(   // CMAC-AES192, Example #1
			"8E73B0F7 DA0E6452 C810F32B 809079E5 62F8EAD2 522C6B7B",
			"",
			"D17DDF46 ADAACDE5 31CAC483 DE7A9367")]
		[DataRow(   // CMAC-AES192, Example #2
			"8E73B0F7 DA0E6452 C810F32B 809079E5 62F8EAD2 522C6B7B",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A",
			"9E99A7BF 31E71090 0662F65E 617C5184")]
		[DataRow(   // CMAC-AES192, Example #3
			"8E73B0F7 DA0E6452 C810F32B 809079E5 62F8EAD2 522C6B7B",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57",
			"3D75C194 ED960704 44A9FA7E C740ECF8")]
		[DataRow(   // CMAC-AES192, Example #4
			"8E73B0F7 DA0E6452 C810F32B 809079E5 62F8EAD2 522C6B7B",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57 1E03AC9C 9EB76FAC 45AF8E51 30C81C46 A35CE411 E5FBC119 1A0A52EF F69F2445 DF4F9B17 AD2B417B E66C3710",
			"A1D5DF0E ED790F79 4D775896 59F39A11")]
		public void Test_02_CMAC_AES192(string Key, string Message, string Signature)
		{
			using CMac Cipher = CMac.CreateAes192CMac(Hashes.StringToBinary(Key));
			byte[] Mac = Cipher.Sign(Hashes.StringToBinary(Message));

			Assert.IsTrue(Cipher.Verify(Hashes.StringToBinary(Message), Mac));

			Assert.AreEqual(Signature.Replace(" ", string.Empty),
				Hashes.BinaryToString(Mac).ToUpperInvariant());
		}

		[TestMethod]
		[DataRow(   // CMAC-AES256, Example #1
			"603DEB10 15CA71BE 2B73AEF0 857D7781 1F352C07 3B6108D7 2D9810A3 0914DFF4",
			"",
			"028962F6 1B7BF89E FC6B551F 4667D983")]
		[DataRow(   // CMAC-AES256, Example #2
			"603DEB10 15CA71BE 2B73AEF0 857D7781 1F352C07 3B6108D7 2D9810A3 0914DFF4",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A",
			"28A7023F 452E8F82 BD4BF28D 8C37C35C")]
		[DataRow(   // CMAC-AES256, Example #3
			"603DEB10 15CA71BE 2B73AEF0 857D7781 1F352C07 3B6108D7 2D9810A3 0914DFF4",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57",
			"156727DC 0878944A 023C1FE0 3BAD6D93")]
		[DataRow(   // CMAC-AES256, Example #4
			"603DEB10 15CA71BE 2B73AEF0 857D7781 1F352C07 3B6108D7 2D9810A3 0914DFF4",
			"6BC1BEE2 2E409F96 E93D7E11 7393172A AE2D8A57 1E03AC9C 9EB76FAC 45AF8E51 30C81C46 A35CE411 E5FBC119 1A0A52EF F69F2445 DF4F9B17 AD2B417B E66C3710",
			"E1992190 549F6ED5 696A2C05 6C315410")]
		public void Test_03_CMAC_AES256(string Key, string Message, string Signature)
		{
			using CMac Cipher = CMac.CreateAes256CMac(Hashes.StringToBinary(Key));
			byte[] Mac = Cipher.Sign(Hashes.StringToBinary(Message));

			Assert.IsTrue(Cipher.Verify(Hashes.StringToBinary(Message), Mac));

			Assert.AreEqual(Signature.Replace(" ", string.Empty),
				Hashes.BinaryToString(Mac).ToUpperInvariant());
		}
	}
}
