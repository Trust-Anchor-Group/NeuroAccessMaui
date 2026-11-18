using System.Text;
using NeuroAccessMaui.Services.Contacts.VCard;

namespace NeuroAccess.Nfc.Test.VCards
{
	/// <summary>
	/// Tests writing contact files as vCard 4.0: line folding, escaping, and that what is written reads back the same.
	/// </summary>
	[TestClass]
	public class VCardWriterTests
	{
		[TestMethod]
		public void Test_01_RoundTrip_V21_ReadsBackTheSame()
		{
			AssertRoundTrip(VCardReader.Parse(V21Text()).Single());
		}

		[TestMethod]
		public void Test_02_RoundTrip_V30_ReadsBackTheSame()
		{
			AssertRoundTrip(VCardReader.Parse(V30Text()).Single());
		}

		[TestMethod]
		public void Test_03_RoundTrip_V40_ReadsBackTheSame()
		{
			AssertRoundTrip(VCardReader.Parse(V40Text()).Single());
		}

		[TestMethod]
		public void Test_04_Write_LongMultiByteName_FoldsAt75Octets()
		{
			VCard Card = BuiltCard();
			string Text = VCardWriter.Write(Card);
			VCard Back = VCardReader.Parse(Text).Single();

			Assert.IsTrue(Text.Split("\r\n").All(Line => Encoding.UTF8.GetByteCount(Line) <= 75));
			Assert.AreEqual(Card.FormattedName, Back.FormattedName);
		}

		[TestMethod]
		public void Test_05_Write_NoteWithEmoji_ReadsBack()
		{
			VCard Card = BuiltCard();
			VCard Back = VCardReader.Parse(VCardWriter.Write(Card)).Single();

			Assert.AreEqual(Card.Note, Back.Note);
		}

		[TestMethod]
		public void Test_06_Write_SemicolonInStreet_IsEscaped()
		{
			VCard Back = VCardReader.Parse(VCardWriter.Write(BuiltCard())).Single();

			Assert.AreEqual("Gata 1; hus B", Back.Addresses.Single().Street);
		}

		[TestMethod]
		public void Test_07_Write_CommaInLabel_IsEscaped()
		{
			VCard Back = VCardReader.Parse(VCardWriter.Write(BuiltCard())).Single();

			CollectionAssert.AreEqual(new[] { "Family", "Work, old" }, Back.Categories.ToArray());
		}

		[TestMethod]
		public void Test_08_Write_Photo_ReadsBack()
		{
			VCard Back = VCardReader.Parse(VCardWriter.Write(BuiltCard())).Single();

			Assert.AreEqual("image/jpeg", Back.Photo?.MediaType);
			CollectionAssert.AreEqual(Jpeg(), Back.Photo?.Data);
		}

		[TestMethod]
		public void Test_09_Write_SeveralCards_ReadsBackAll()
		{
			VCard[] Cards = [BuiltCard(), VCardReader.Parse(V30Text()).Single(), VCardReader.Parse(V40Text()).Single()];

			Assert.HasCount(3, VCardReader.Parse(VCardWriter.Write(Cards)));
		}

		/// <summary>
		/// Writes a card, reads it back, and checks that the text is vCard 4.0 folded at 75 octets with CRLF line endings,
		/// and that every field reads back the same.
		/// </summary>
		private static void AssertRoundTrip(VCard Card)
		{
			string Text = VCardWriter.Write(Card);
			VCard Back = VCardReader.Parse(Text).Single();
			string WithoutLineEnds = Text.Replace("\r\n", string.Empty);

			Assert.AreEqual("4.0", Back.Version);
			Assert.Contains("VERSION:4.0\r\n", Text);
			Assert.IsTrue(Text.Split("\r\n").All(Line => Encoding.UTF8.GetByteCount(Line) <= 75));
			Assert.IsFalse(WithoutLineEnds.Contains('\n') || WithoutLineEnds.Contains('\r'));

			Assert.AreEqual(Card.FormattedName, Back.FormattedName);
			Assert.AreEqual(Card.Name, Back.Name);
			CollectionAssert.AreEqual(Card.Telephones.Select(Describe).ToArray(), Back.Telephones.Select(Describe).ToArray());
			CollectionAssert.AreEqual(Card.Emails.Select(Email => Email.Value).ToArray(), Back.Emails.Select(Email => Email.Value).ToArray());
			CollectionAssert.AreEqual(Card.Addresses.Select(Address => Address.ToSingleLine()).ToArray(),
				Back.Addresses.Select(Address => Address.ToSingleLine()).ToArray());
			CollectionAssert.AreEqual(Card.Categories.ToArray(), Back.Categories.ToArray());
			CollectionAssert.AreEqual(Card.Organization.ToArray(), Back.Organization.ToArray());
			CollectionAssert.AreEqual(Card.Impp.ToArray(), Back.Impp.ToArray());
			Assert.AreEqual(Card.Note, Back.Note);
			Assert.AreEqual(Card.Uid, Back.Uid);
			Assert.AreEqual(Card.GetValue("X-NEURO-LEGAL-ID"), Back.GetValue("X-NEURO-LEGAL-ID"));
			Assert.AreEqual(Card.Photo?.MediaType, Back.Photo?.MediaType);
			CollectionAssert.AreEqual(Card.Photo?.Data, Back.Photo?.Data);
		}

		/// <summary>
		/// A telephone number with its types and if it is preferred, as one text to compare.
		/// </summary>
		private static string Describe(VCardTypedValue Phone)
		{
			return Phone.Value + "/" + string.Join("+", Phone.Types) + "/" + Phone.IsPreferred;
		}

		/// <summary>
		/// A card built in code, with a long name in multi-byte letters, a note with emoji, and values that need escaping.
		/// </summary>
		private static VCard BuiltCard()
		{
			VCard Card = new()
			{
				Uid = "urn:uuid:" + Guid.NewGuid(),
				FormattedName = "Åsa Öberg-Ängström Ödmansson Ångermanländska Kärnkraftsföreningen ÅÄÖ ÅÄÖ ÅÄÖ",
				Name = new VCardName("Öberg", "Åsa", string.Empty, string.Empty, string.Empty),
				Categories = ["Family", "Work, old"],
				Note = "Notes: a, b; c\nsecond line " + string.Concat(Enumerable.Repeat("\U0001F600", 25))
			};

			Card.AddTelephone("+46 70 000 00 00", "cell");
			Card.AddEmail("asa@example.com", "home");
			Card.AddAddress(new VCardAddress(string.Empty, string.Empty, "Gata 1; hus B", "Umeå", string.Empty, "901 00", "Sverige", ["home"], null));
			Card.AddImpp("xmpp:asa@lab.tagroot.io");
			Card.SetPhoto(Jpeg(), "image/jpeg");

			return Card;
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
		/// A vCard 2.1 card with quoted-printable UTF-8 text, bare telephone types and a folded photo.
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
