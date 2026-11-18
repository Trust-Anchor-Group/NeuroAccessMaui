namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// A contact card (vCard, RFC 6350): a list of properties, with typed access to the ones the app uses.
	/// </summary>
	/// <remarks>
	/// Cards are read by <see cref="VCardReader"/> from version 2.1, 3.0 and 4.0 text, and written by
	/// <see cref="VCardWriter"/> as version 4.0. Properties the app does not know are kept, so they are written back.
	/// </remarks>
	public sealed class VCard
	{
		/// <summary>
		/// The version the card was read as, such as "2.1", "3.0" or "4.0". A new card is "4.0".
		/// </summary>
		public string Version { get; set; } = "4.0";

		/// <summary>
		/// The properties of the card, in order. BEGIN, END and VERSION are not among them.
		/// </summary>
		public List<VCardProperty> Properties { get; } = [];

		#region Generic access

		/// <summary>
		/// Gets the first property with a name.
		/// </summary>
		/// <param name="Name">Name of the property, in any case.</param>
		/// <returns>The property, or null if the card has none.</returns>
		public VCardProperty? GetProperty(string Name)
		{
			foreach (VCardProperty Property in this.Properties)
			{
				if (Property.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
					return Property;
			}

			return null;
		}

		/// <summary>
		/// Gets all properties with a name.
		/// </summary>
		/// <param name="Name">Name of the property, in any case.</param>
		/// <returns>The properties, in order.</returns>
		public IEnumerable<VCardProperty> GetProperties(string Name)
		{
			return this.Properties.Where(Property => Property.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets the value of the first property with a name, such as an extension property like X-NEURO-LEGAL-ID.
		/// </summary>
		/// <param name="Name">Name of the property, in any case.</param>
		/// <returns>The value, unescaped if it is text, or null if the card has no such property or it is empty.</returns>
		public string? GetValue(string Name)
		{
			string? Value = this.GetProperty(Name)?.Value;
			return string.IsNullOrWhiteSpace(Value) ? null : Value.Trim();
		}

		/// <summary>
		/// Adds a property at the end of the card.
		/// </summary>
		/// <param name="Property">The property.</param>
		public void Add(VCardProperty Property)
		{
			this.Properties.Add(Property);
		}

		/// <summary>
		/// Removes all properties with a name.
		/// </summary>
		/// <param name="Name">Name of the property, in any case.</param>
		/// <returns>The number of properties removed.</returns>
		public int RemoveAll(string Name)
		{
			return this.Properties.RemoveAll(Property => Property.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Sets a text property that the card has once, such as FN or NOTE, replacing any it had. Null or empty text
		/// removes it.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Text">The text, or null to remove the property.</param>
		public void SetSingle(string Name, string? Text)
		{
			this.Replace(Name, string.IsNullOrWhiteSpace(Text) ? null : VCardProperty.FromText(Name, Text.Trim()));
		}

		/// <summary>
		/// Sets a property whose value is a link, replacing any it had. Null or empty removes it.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Uri">The link, or null to remove the property.</param>
		public void SetUri(string Name, string? Uri)
		{
			this.Replace(Name, string.IsNullOrWhiteSpace(Uri) ? null : VCardProperty.FromUri(Name, Uri));
		}

		/// <summary>
		/// Replaces the first property with the same name in place, so the order of the card is kept, and removes the
		/// others. A null property removes them all.
		/// </summary>
		private void Replace(string Name, VCardProperty? Property)
		{
			int Index = this.Properties.FindIndex(Existing => Existing.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
			this.RemoveAll(Name);

			if (Property is null)
				return;

			if (Index < 0 || Index > this.Properties.Count)
				this.Properties.Add(Property);
			else
				this.Properties.Insert(Index, Property);
		}

		#endregion

		#region Identity and name

		/// <summary>
		/// The card's unique ID (UID), such as <c>urn:uuid:…</c>. It is kept on import, to match cards imported again.
		/// </summary>
		public string? Uid
		{
			get => this.GetValue("UID");
			set => this.SetSingle("UID", value);
		}

		/// <summary>
		/// The name as it is shown (FN).
		/// </summary>
		public string? FormattedName
		{
			get => this.GetValue("FN");
			set => this.SetSingle("FN", value);
		}

		/// <summary>
		/// The parts of the name (N), or null if the card has no N property.
		/// </summary>
		public VCardName? Name
		{
			get
			{
				VCardProperty? Property = this.GetProperty("N");
				if (Property is null)
					return null;

				IReadOnlyList<string> Parts = Property.Components;
				return new VCardName(Part(Parts, 0), Part(Parts, 1), Part(Parts, 2), Part(Parts, 3), Part(Parts, 4));
			}

			set
			{
				this.Replace("N", value is null || value.IsEmpty ? null :
					VCardProperty.FromComponents("N", [value.Family, value.Given, value.Additional, value.Prefixes, value.Suffixes]));
			}
		}

		/// <summary>
		/// Nicknames (NICKNAME), from all NICKNAME properties.
		/// </summary>
		public IReadOnlyList<string> Nicknames => [.. this.GetProperties("NICKNAME").SelectMany(Property => Property.ListValues)];

		/// <summary>
		/// The name to show: the formatted name, or else the name parts, or else a nickname, or else the organization.
		/// </summary>
		public string? DisplayName
		{
			get
			{
				string? Name = this.FormattedName;
				if (!string.IsNullOrWhiteSpace(Name))
					return Name;

				Name = this.Name?.ToDisplayName();
				if (!string.IsNullOrWhiteSpace(Name))
					return Name;

				if (this.Nicknames.Count > 0)
					return this.Nicknames[0];

				IReadOnlyList<string> Organization = this.Organization;
				return Organization.Count > 0 && !string.IsNullOrWhiteSpace(Organization[0]) ? Organization[0].Trim() : null;
			}
		}

		#endregion

		#region Work

		/// <summary>
		/// The parts of the organization (ORG): its name, then any units. Empty if the card has none.
		/// </summary>
		public IReadOnlyList<string> Organization
		{
			get
			{
				VCardProperty? Property = this.GetProperty("ORG");
				if (Property is null)
					return [];

				List<string> Parts = [.. Property.Components.Select(Part => Part.Trim())];
				while (Parts.Count > 0 && string.IsNullOrEmpty(Parts[^1]))
					Parts.RemoveAt(Parts.Count - 1);

				return Parts;
			}
		}

		/// <summary>
		/// Sets the organization (ORG) and its units. No name removes it.
		/// </summary>
		/// <param name="Parts">The organization's name, then any units.</param>
		public void SetOrganization(params string[] Parts)
		{
			bool Any = Parts.Any(Part => !string.IsNullOrWhiteSpace(Part));
			this.Replace("ORG", Any ? VCardProperty.FromComponents("ORG", Parts) : null);
		}

		/// <summary>
		/// The person's title or role at the organization (TITLE).
		/// </summary>
		public string? Title
		{
			get => this.GetValue("TITLE");
			set => this.SetSingle("TITLE", value);
		}

		#endregion

		#region Ways to reach

		/// <summary>
		/// Phone numbers (TEL), with their types. A tel: link is given without its scheme.
		/// </summary>
		public IReadOnlyList<VCardTypedValue> Telephones => this.TypedValues("TEL", "tel:");

		/// <summary>
		/// Email addresses (EMAIL), with their types. A mailto: link is given without its scheme.
		/// </summary>
		public IReadOnlyList<VCardTypedValue> Emails => this.TypedValues("EMAIL", "mailto:");

		/// <summary>
		/// Chat addresses and other messaging links (IMPP), such as <c>xmpp:anna@example.org</c>.
		/// </summary>
		public IReadOnlyList<string> Impp => [.. this.GetProperties("IMPP").Select(Property => Property.Value.Trim()).Where(Value => Value.Length > 0)];

		/// <summary>
		/// Adds a phone number.
		/// </summary>
		/// <param name="Number">The number.</param>
		/// <param name="Types">Types, such as "cell", "home" or "work".</param>
		public void AddTelephone(string Number, params string[] Types)
		{
			this.Add(VCardProperty.FromText("TEL", Number.Trim(), Types));
		}

		/// <summary>
		/// Adds an email address.
		/// </summary>
		/// <param name="Address">The email address.</param>
		/// <param name="Types">Types, such as "home" or "work".</param>
		public void AddEmail(string Address, params string[] Types)
		{
			this.Add(VCardProperty.FromText("EMAIL", Address.Trim(), Types));
		}

		/// <summary>
		/// Adds a chat address or other messaging link (IMPP).
		/// </summary>
		/// <param name="Uri">The link, such as <c>xmpp:anna@example.org</c>.</param>
		public void AddImpp(string Uri)
		{
			this.Add(VCardProperty.FromUri("IMPP", Uri));
		}

		private List<VCardTypedValue> TypedValues(string Name, string Scheme)
		{
			List<VCardTypedValue> Result = [];

			foreach (VCardProperty Property in this.GetProperties(Name))
			{
				string Value = Property.Value.Trim();

				if (Value.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
					Value = Value[Scheme.Length..].Trim();

				if (Value.Length == 0)
					continue;

				List<string> Types = [.. Property.Types.Where(Type => !Type.Equals("pref", StringComparison.OrdinalIgnoreCase))];
				Result.Add(new VCardTypedValue(Value, Types, Property.IsPreferred));
			}

			return Result;
		}

		#endregion

		#region Addresses

		/// <summary>
		/// Postal addresses (ADR), with their types and labels.
		/// </summary>
		public IReadOnlyList<VCardAddress> Addresses
		{
			get
			{
				List<VCardAddress> Result = [];

				foreach (VCardProperty Property in this.GetProperties("ADR"))
				{
					IReadOnlyList<string> Parts = Property.Components;
					List<string> Types = [.. Property.Types.Where(Type => !Type.Equals("pref", StringComparison.OrdinalIgnoreCase))];
					string? Label = Property.GetParameter("LABEL");

					VCardAddress Address = new(Part(Parts, 0), Part(Parts, 1), Part(Parts, 2), Part(Parts, 3), Part(Parts, 4),
						Part(Parts, 5), Part(Parts, 6), Types, string.IsNullOrWhiteSpace(Label) ? null : Label);

					if (!Address.IsEmpty || Address.Label is not null)
						Result.Add(Address);
				}

				return Result;
			}
		}

		/// <summary>
		/// Adds a postal address.
		/// </summary>
		/// <param name="Address">The address.</param>
		public void AddAddress(VCardAddress Address)
		{
			VCardProperty Property = VCardProperty.FromComponents("ADR",
				[Address.PostOfficeBox, Address.Extended, Address.Street, Address.Locality, Address.Region, Address.PostalCode, Address.Country]);

			if (Address.Types.Count > 0)
				Property.SetParameter("TYPE", [.. Address.Types]);

			if (!string.IsNullOrWhiteSpace(Address.Label))
				Property.SetParameter("LABEL", Address.Label);

			this.Add(Property);
		}

		#endregion

		#region Personal

		/// <summary>
		/// Birthday (BDAY), as written in the card, such as 1985-04-12 or --04-12.
		/// </summary>
		public string? Birthday
		{
			get => this.GetValue("BDAY");
			set => this.SetSingle("BDAY", value);
		}

		/// <summary>
		/// Notes (NOTE). Several NOTE properties are joined with blank lines.
		/// </summary>
		public string? Note
		{
			get
			{
				List<string> Notes = [.. this.GetProperties("NOTE").Select(Property => Property.Value.Trim()).Where(Value => Value.Length > 0)];
				return Notes.Count == 0 ? null : string.Join("\n\n", Notes);
			}

			set => this.SetSingle("NOTE", value);
		}

		/// <summary>
		/// Labels the user has given the contact (CATEGORIES), from all CATEGORIES properties, without repeats.
		/// </summary>
		public IReadOnlyList<string> Categories
		{
			get
			{
				List<string> Result = [];

				foreach (string Category in this.GetProperties("CATEGORIES").SelectMany(Property => Property.ListValues))
				{
					if (!Result.Contains(Category, StringComparer.CurrentCultureIgnoreCase))
						Result.Add(Category);
				}

				return Result;
			}

			set
			{
				this.Replace("CATEGORIES", value is null || value.Count == 0 ? null : VCardProperty.FromList("CATEGORIES", value));
			}
		}

		/// <summary>
		/// When the card was last changed (REV), as written in the card, such as 20261118T101500Z.
		/// </summary>
		public string? Rev
		{
			get => this.GetValue("REV");
			set => this.SetSingle("REV", value);
		}

		#endregion

		#region Photo

		/// <summary>
		/// The photo (PHOTO): embedded data, from a data: link or base64 data in version 2.1 and 3.0 cards, or a link. Null
		/// if the card has no photo, or the photo can't be read.
		/// </summary>
		public VCardPhoto? Photo
		{
			get
			{
				VCardProperty? Property = this.GetProperty("PHOTO");
				if (Property is null || string.IsNullOrWhiteSpace(Property.RawValue))
					return null;

				if (Property.IsBinary)
				{
					byte[]? Data = FromBase64(Property.RawValue);
					return Data is null ? null : new VCardPhoto(Data, MediaTypeOf(Property), null);
				}

				string Raw = Property.RawValue.Trim();

				if (Raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
					return TryParseDataUri(Raw, out byte[]? Data, out string? MediaType) ? new VCardPhoto(Data, MediaType, null) : null;

				return new VCardPhoto(null, null, Raw);
			}
		}

		/// <summary>
		/// Sets an embedded photo, replacing any photo the card had.
		/// </summary>
		/// <param name="Data">The encoded image.</param>
		/// <param name="MediaType">Its media type, such as image/jpeg.</param>
		public void SetPhoto(byte[] Data, string MediaType)
		{
			this.SetUri("PHOTO", ToDataUri(Data, MediaType));
		}

		/// <summary>
		/// Removes the photo.
		/// </summary>
		public void RemovePhoto()
		{
			this.RemoveAll("PHOTO");
		}

		/// <summary>
		/// Writes embedded data as a data: link.
		/// </summary>
		/// <param name="Data">The data.</param>
		/// <param name="MediaType">Its media type.</param>
		/// <returns>The data: link.</returns>
		public static string ToDataUri(byte[] Data, string MediaType)
		{
			string Type = string.IsNullOrWhiteSpace(MediaType) ? "application/octet-stream" : MediaType.Trim();
			return "data:" + Type + ";base64," + Convert.ToBase64String(Data);
		}

		/// <summary>
		/// Reads a data: link that holds base64 data.
		/// </summary>
		/// <param name="Uri">The data: link.</param>
		/// <param name="Data">The data, if the link could be read.</param>
		/// <param name="MediaType">Its media type, if the link gives one.</param>
		/// <returns>If the link could be read.</returns>
		public static bool TryParseDataUri(string Uri, out byte[]? Data, out string? MediaType)
		{
			Data = null;
			MediaType = null;

			if (!Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				return false;

			int Comma = Uri.IndexOf(',');
			if (Comma < 0)
				return false;

			string Header = Uri[5..Comma];
			string Payload = Uri[(Comma + 1)..];
			string[] Parts = Header.Split(';', StringSplitOptions.TrimEntries);

			if (Parts.Length > 0 && Parts[0].Contains('/'))
				MediaType = Parts[0].ToLowerInvariant();

			if (!Parts.Any(Part => Part.Equals("base64", StringComparison.OrdinalIgnoreCase)))
				return false;

			Data = FromBase64(Payload);
			return Data is not null;
		}

		/// <summary>
		/// The media type of embedded data in a version 2.1 or 3.0 property, from its MEDIATYPE or TYPE parameter, such as
		/// TYPE=JPEG for a photo.
		/// </summary>
		/// <param name="Property">The property.</param>
		/// <returns>The media type, or null if it is not given.</returns>
		public static string? MediaTypeOf(VCardProperty Property)
		{
			string? MediaType = Property.GetParameter("MEDIATYPE");
			if (!string.IsNullOrWhiteSpace(MediaType))
				return MediaType.Trim().ToLowerInvariant();

			foreach (string Type in Property.Types)
			{
				if (Type.Contains('/'))
					return Type;

				string Kind = Property.Name is "SOUND" ? "audio" : Property.Name is "KEY" ? "application" : "image";

				return Type switch
				{
					"jpeg" or "jpg" => "image/jpeg",
					"png" => "image/png",
					"gif" => "image/gif",
					"bmp" => "image/bmp",
					"webp" => "image/webp",
					"tiff" or "tif" => "image/tiff",
					_ => Kind + "/" + Type
				};
			}

			return null;
		}

		private static byte[]? FromBase64(string Text)
		{
			string Clean = string.Concat(Text.Where(ch => !char.IsWhiteSpace(ch)));

			try
			{
				return Convert.FromBase64String(Clean);
			}
			catch (FormatException)
			{
				return null;
			}
		}

		#endregion

		private static string Part(IReadOnlyList<string> Parts, int Index)
		{
			return Index < Parts.Count ? Parts[Index].Trim() : string.Empty;
		}
	}
}
