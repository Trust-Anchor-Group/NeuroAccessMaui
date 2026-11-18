using System.Text;
using NeuroAccessMaui.Services.Contacts.VCard;

namespace NeuroAccess.Nfc.Test.VCards
{
	/// <summary>
	/// Tests reading contact files in vCard 2.1, 3.0 and 4.0, and that broken files are read without throwing.
	/// </summary>
	[TestClass]
	public class VCardReaderTests
	{
		[TestMethod]
		public void Test_01_Read_V21_QuotedPrintableNameAndPhoto()
		{
			VCard Card = VCardReader.Parse(V21Text()).Single();

			Assert.AreEqual("2.1", Card.Version);
			Assert.AreEqual("Öberg", Card.Name?.Family);
			Assert.AreEqual("Åsa", Card.Name?.Given);
			Assert.AreEqual("Åsa Öberg €", Card.FormattedName);
		}

		[TestMethod]
		public void Test_02_Read_V21_BareTelephoneTypes()
		{
			VCard Card = VCardReader.Parse(V21Text()).Single();

			Assert.HasCount(1, Card.Telephones);
			Assert.Contains("cell", Card.Telephones[0].Types);
			Assert.IsTrue(Card.Telephones[0].IsPreferred);
		}

		[TestMethod]
		public void Test_03_Read_V21_FoldedBase64Photo()
		{
			VCard Card = VCardReader.Parse(V21Text()).Single();

			Assert.IsNotNull(Card.Photo);
			Assert.IsTrue(Card.Photo.IsEmbedded);
			Assert.AreEqual("image/jpeg", Card.Photo.MediaType);
			CollectionAssert.AreEqual(Jpeg(), Card.Photo.Data);
		}

		[TestMethod]
		public void Test_04_Read_V30_TelephoneAndAddress()
		{
			VCard Card = VCardReader.Parse(V30Text()).Single();
			VCardTypedValue Phone = Card.Telephones.Single();
			VCardAddress Address = Card.Addresses.Single();

			Assert.AreEqual("+46 70 123 45 67", Phone.Value);
			Assert.Contains("cell", Phone.Types);
			Assert.AreEqual("Storgatan 12, 3 tr", Address.Street);
			Assert.AreEqual("Stockholm", Address.Locality);
			Assert.AreEqual("111 51", Address.PostalCode);
			Assert.AreEqual("Sweden", Address.Country);
			Assert.Contains("home", Address.Types);
			Assert.AreEqual("item1", Card.GetProperty("ADR")?.Group);
		}

		[TestMethod]
		public void Test_05_Read_V30_CategoriesPhotoAndOrganization()
		{
			VCard Card = VCardReader.Parse(V30Text()).Single();

			CollectionAssert.AreEqual(new[] { "Work", "Friends" }, Card.Categories.ToArray());
			CollectionAssert.AreEqual(Jpeg(), Card.Photo?.Data);
			CollectionAssert.AreEqual(new[] { "Trust Anchor Group", "Product" }, Card.Organization.ToArray());
		}

		[TestMethod]
		public void Test_06_Read_V40_UidPhotoChatLinkAndLegalId()
		{
			VCard Card = VCardReader.Parse(V40Text()).Single();

			Assert.AreEqual("urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427", Card.Uid);
			Assert.IsNotNull(Card.Photo);
			Assert.IsTrue(Card.Photo.IsEmbedded);
			Assert.AreEqual("image/png", Card.Photo.MediaType);
			CollectionAssert.AreEqual(Jpeg(), Card.Photo.Data);
			CollectionAssert.AreEqual(new[] { "xmpp:erik@lab.tagroot.io" }, Card.Impp.ToArray());
			Assert.AreEqual("2c8b@legal.lab.tagroot.io", Card.GetValue("X-NEURO-LEGAL-ID"));
		}

		[TestMethod]
		public void Test_07_Read_V40_EscapedNoteAndPreferredEmail()
		{
			VCard Card = VCardReader.Parse(V40Text()).Single();

			Assert.AreEqual("Line one\nLine two, with comma; and semicolon\\ backslash", Card.Note);
			Assert.IsTrue(Card.Emails.Single().IsPreferred);
			CollectionAssert.AreEqual(new[] { "work" }, Card.Emails.Single().Types.ToArray());
		}

		[TestMethod]
		public void Test_08_Read_ThreeCards_BlankLinesAndJunk()
		{
			string Text = "\r\n\r\nBEGIN:VCARD\nVERSION:3.0\nFN:One\nEND:VCARD\n\n\njunk line without colon\nBEGIN:VCARD\r\nVERSION:4.0\r\nFN:Two\r\n" +
				"END:VCARD\r\n   \r\nBEGIN:VCARD\r\nFN:Three\r\nEND:VCARD";

			IReadOnlyList<VCard> Cards = VCardReader.Parse(Text);

			CollectionAssert.AreEqual(new[] { "One", "Two", "Three" }, Cards.Select(Card => Card.FormattedName).ToArray());
		}

		[TestMethod]
		public void Test_09_Read_MissingEnd_CardIsKept()
		{
			VCard Card = VCardReader.Parse("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:No end").Single();

			Assert.AreEqual("No end", Card.FormattedName);
		}

		[TestMethod]
		public void Test_10_Read_UnterminatedQuote_DoesNotThrow()
		{
			AssertReadsWithoutThrowing("BEGIN:VCARD\r\nFN;TYPE=\"unterminated:Name\r\nTEL:123\r\nEND:VCARD");
		}

		[TestMethod]
		public void Test_11_Read_BrokenValues_DoesNotThrow()
		{
			AssertReadsWithoutThrowing("garbage\r\n:::\r\n;;;\r\nBEGIN:VCARD\r\nN:a;b;c;d;e;f;g;h\r\nPHOTO;ENCODING=b:!!!notbase64!!!\r\n" +
				"PHOTO:data:image/png;base64,%%%\r\nADR:\r\nEND:VCARD");
		}

		[TestMethod]
		public void Test_12_Read_StrayEndAndNestedBegin_DoesNotThrow()
		{
			AssertReadsWithoutThrowing("END:VCARD\r\nBEGIN:VCARD\r\nBEGIN:VCARD\r\nFN:Nested\r\nEND:VCARD");
		}

		[TestMethod]
		public void Test_13_Read_BrokenQuotedPrintable_DoesNotThrow()
		{
			AssertReadsWithoutThrowing("FN;ENCODING=QUOTED-PRINTABLE:=ZZ=4\r\n");
		}

		[TestMethod]
		public void Test_14_Read_EmptyAndJunkText_DoesNotThrow()
		{
			AssertReadsWithoutThrowing(string.Empty);
			AssertReadsWithoutThrowing(new string('=', 10000));
			Assert.IsEmpty(VCardReader.Parse(null));
		}

		[TestMethod]
		public void Test_15_Read_TooManyProperties_AreCapped()
		{
			StringBuilder Text = new("BEGIN:VCARD\r\nVERSION:4.0\r\n");
			for (int i = 0; i < 1500; i++)
				Text.Append("NOTE:n").Append(i).Append("\r\n");
			Text.Append("END:VCARD\r\n");

			VCard Card = VCardReader.Parse(Text.ToString()).Single();

			Assert.HasCount(VCardReader.MaxProperties, Card.Properties);
		}

		/// <summary>
		/// Reads a text, and every part of every card in it, and fails if anything throws.
		/// </summary>
		private static void AssertReadsWithoutThrowing(string Text)
		{
			foreach (VCard Card in VCardReader.Parse(Text))
			{
				_ = Card.DisplayName;
				_ = Card.Photo;
				_ = Card.Addresses;
				_ = Card.Telephones;
				_ = Card.Name;
				_ = VCardWriter.Write(Card);
			}
		}

		/// <summary>
		/// Bytes that stand in for a small photo.
		/// </summary>
		private static byte[] Jpeg()
		{
			byte[] Data = new byte[300];
			for (int i = 0; i < Data.Length; i++)
				Data[i] = (byte)(i * 7);

			Data[0] = 0xFF;
			Data[1] = 0xD8;
			return Data;
		}

		/// <summary>
		/// A vCard 2.1 card with quoted-printable UTF-8 text, a soft line break, bare telephone types and a folded photo.
		/// </summary>
		private static string V21Text()
		{
			string Base64 = Convert.ToBase64String(Jpeg());
			StringBuilder Text = new();

			Text.Append("BEGIN:VCARD\r\nVERSION:2.1\r\n");
			Text.Append("N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:=C3=96berg;=C3=85sa;;;\r\n");
			Text.Append("FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:=C3=85sa =C3=96berg =\r\n=E2=82=AC\r\n");
			Text.Append("TEL;CELL;PREF:+46701234567\r\n");
			Text.Append("EMAIL;INTERNET:asa@example.com\r\n");
			Text.Append("PHOTO;ENCODING=BASE64;TYPE=JPEG:");
			for (int i = 0; i < Base64.Length; i += 60)
				Text.Append(i == 0 ? string.Empty : "\r\n ").Append(Base64.AsSpan(i, Math.Min(60, Base64.Length - i)));
			Text.Append("\r\n\r\nEND:VCARD\r\n");

			return Text.ToString();
		}

		/// <summary>
		/// A vCard 3.0 card with a grouped address holding an escaped comma, labels, a photo and an organization.
		/// </summary>
		private static string V30Text()
		{
			return "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Anna Lindqvist\r\nN:Lindqvist;Anna;;;\r\nTEL;TYPE=CELL:+46 70 123 45 67\r\n" +
				"item1.ADR;TYPE=HOME:;;Storgatan 12\\, 3 tr;Stockholm;;111 51;Sweden\r\nCATEGORIES:Work,Friends\r\n" +
				"PHOTO;ENCODING=b;TYPE=JPEG:" + Convert.ToBase64String(Jpeg()) + "\r\nORG:Trust Anchor Group;Product\r\nEND:VCARD\r\n";
		}

		/// <summary>
		/// A vCard 4.0 card with a data: photo, a chat link, a legal ID and a note with escaped characters.
		/// </summary>
		private static string V40Text()
		{
			return "BEGIN:VCARD\r\nVERSION:4.0\r\nUID:urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427\r\nFN:Erik Johansson\r\n" +
				"PHOTO:data:image/png;base64," + Convert.ToBase64String(Jpeg()) + "\r\nIMPP:xmpp:erik@lab.tagroot.io\r\n" +
				"X-NEURO-LEGAL-ID:2c8b@legal.lab.tagroot.io\r\n" +
				"NOTE:Line one\\nLine two\\, with comma\\; and semicolon\\\\ backslash\r\nEMAIL;TYPE=work;PREF=1:erik@example.com\r\nEND:VCARD\r\n";
		}
	}
}
