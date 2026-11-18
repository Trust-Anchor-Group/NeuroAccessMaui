namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// A property of a contact card, such as <c>TEL;TYPE=cell:+46 70 123 45 67</c>.
	/// </summary>
	/// <remarks>
	/// The value is kept as it is written in the card, after unfolding and decoding: <see cref="RawValue"/>. Text values
	/// are escaped there; <see cref="Value"/>, <see cref="Components"/> and <see cref="ListValues"/> give them unescaped.
	/// Links and embedded data, such as a photo, are never escaped.
	/// </remarks>
	public sealed class VCardProperty
	{
		private static readonly HashSet<string> linkNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"PHOTO", "LOGO", "SOUND", "KEY", "URL", "IMPP", "SOURCE", "MEMBER", "FBURL", "CALADRURI", "CALURI", "RELATED", "GEO"
		};

		/// <summary>
		/// A property of a contact card.
		/// </summary>
		/// <param name="Name">Name of the property. It is stored in upper case.</param>
		public VCardProperty(string Name)
		{
			this.Name = Name.Trim().ToUpperInvariant();
		}

		/// <summary>
		/// The group the property belongs to, as in <c>item1.TEL</c>, if any.
		/// </summary>
		public string? Group { get; set; }

		/// <summary>
		/// Name of the property, in upper case.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Parameters of the property.
		/// </summary>
		public List<VCardParameter> Parameters { get; } = [];

		/// <summary>
		/// The value as written in the card, after unfolding and decoding of quoted-printable text. Text is escaped here.
		/// </summary>
		public string RawValue { get; set; } = string.Empty;

		/// <summary>
		/// If the value is text, which is escaped in the card, rather than a link or embedded data.
		/// </summary>
		public bool IsText
		{
			get
			{
				string? ValueType = this.GetParameter("VALUE");
				if (ValueType is not null &&
					(ValueType.Equals("uri", StringComparison.OrdinalIgnoreCase) || ValueType.Equals("url", StringComparison.OrdinalIgnoreCase) ||
					ValueType.Equals("binary", StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}

				if (this.IsBinary || linkNames.Contains(this.Name))
					return false;

				return !this.RawValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// If the value is embedded data in base64, as version 2.1 and 3.0 cards write photos.
		/// </summary>
		public bool IsBinary
		{
			get
			{
				string? Encoding = this.GetParameter("ENCODING");
				return Encoding is not null &&
					(Encoding.Equals("b", StringComparison.OrdinalIgnoreCase) || Encoding.Equals("base64", StringComparison.OrdinalIgnoreCase));
			}
		}

		/// <summary>
		/// The value as text: unescaped for text values, and as written for links and embedded data.
		/// </summary>
		public string Value => this.IsText ? VCardText.Unescape(this.RawValue) : this.RawValue;

		/// <summary>
		/// The parts of a structured value, such as a name or an address, split on semicolons and unescaped.
		/// </summary>
		public IReadOnlyList<string> Components => VCardText.Split(this.RawValue, ';');

		/// <summary>
		/// The values of a list, such as categories or nicknames, split on commas and unescaped. Empty values are left out.
		/// </summary>
		public IReadOnlyList<string> ListValues
		{
			get
			{
				List<string> Values = VCardText.Split(this.RawValue, ',');
				Values.RemoveAll(Value => string.IsNullOrWhiteSpace(Value));

				for (int i = 0; i < Values.Count; i++)
					Values[i] = Values[i].Trim();

				return Values;
			}
		}

		/// <summary>
		/// The types of the property, from its TYPE parameters, in lower case.
		/// </summary>
		public IReadOnlyList<string> Types
		{
			get
			{
				List<string> Result = [];

				foreach (string Value in this.GetParameterValues("TYPE"))
				{
					foreach (string Type in Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						string Lower = Type.ToLowerInvariant();
						if (!Result.Contains(Lower))
							Result.Add(Lower);
					}
				}

				return Result;
			}
		}

		/// <summary>
		/// If the property is preferred over others of its kind, with a PREF parameter or a "pref" type.
		/// </summary>
		public bool IsPreferred => this.GetParameter("PREF") is not null || this.HasType("pref");

		/// <summary>
		/// Gets the first value of a parameter.
		/// </summary>
		/// <param name="Name">Name of the parameter, in any case.</param>
		/// <returns>The value, or null if the property has no such parameter.</returns>
		public string? GetParameter(string Name)
		{
			foreach (VCardParameter Parameter in this.Parameters)
			{
				if (Parameter.Name.Equals(Name, StringComparison.OrdinalIgnoreCase) && Parameter.Values.Count > 0)
					return Parameter.Values[0];
			}

			return null;
		}

		/// <summary>
		/// Gets all values of a parameter, also when it is given more than once.
		/// </summary>
		/// <param name="Name">Name of the parameter, in any case.</param>
		/// <returns>The values.</returns>
		public IReadOnlyList<string> GetParameterValues(string Name)
		{
			List<string> Result = [];

			foreach (VCardParameter Parameter in this.Parameters)
			{
				if (Parameter.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
					Result.AddRange(Parameter.Values);
			}

			return Result;
		}

		/// <summary>
		/// Adds values to a parameter, or the parameter if the property doesn't have it.
		/// </summary>
		/// <param name="Name">Name of the parameter.</param>
		/// <param name="Values">Values to add.</param>
		public void AddParameter(string Name, params string[] Values)
		{
			foreach (VCardParameter Parameter in this.Parameters)
			{
				if (Parameter.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
				{
					Parameter.Values.AddRange(Values);
					return;
				}
			}

			this.Parameters.Add(new VCardParameter(Name, Values));
		}

		/// <summary>
		/// Sets a parameter to the given values, replacing any values it had.
		/// </summary>
		/// <param name="Name">Name of the parameter.</param>
		/// <param name="Values">The new values.</param>
		public void SetParameter(string Name, params string[] Values)
		{
			this.RemoveParameter(Name);

			if (Values.Length > 0)
				this.Parameters.Add(new VCardParameter(Name, Values));
		}

		/// <summary>
		/// Removes a parameter.
		/// </summary>
		/// <param name="Name">Name of the parameter, in any case.</param>
		public void RemoveParameter(string Name)
		{
			this.Parameters.RemoveAll(Parameter => Parameter.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// If the property has a type, such as "cell" or "home". Version 2.1 types written without TYPE= count too.
		/// </summary>
		/// <param name="Type">The type, in any case.</param>
		/// <returns>If the property has the type.</returns>
		public bool HasType(string Type)
		{
			foreach (string Value in this.Types)
			{
				if (Value.Equals(Type, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Creates a text property. The text is escaped.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Text">The text.</param>
		/// <param name="Types">Types, such as "home" or "cell", if any.</param>
		/// <returns>The property.</returns>
		public static VCardProperty FromText(string Name, string Text, params string[] Types)
		{
			VCardProperty Property = new(Name)
			{
				RawValue = VCardText.Escape(Text)
			};

			if (Types.Length > 0)
				Property.SetParameter("TYPE", Types);

			return Property;
		}

		/// <summary>
		/// Creates a property with a structured value, such as a name or an address. Each part is escaped.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Components">The parts, in order.</param>
		/// <returns>The property.</returns>
		public static VCardProperty FromComponents(string Name, IEnumerable<string?> Components)
		{
			return new VCardProperty(Name)
			{
				RawValue = string.Join(';', Components.Select(Component => VCardText.Escape(Component ?? string.Empty)))
			};
		}

		/// <summary>
		/// Creates a property with a list value, such as categories. Each value is escaped.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Values">The values.</param>
		/// <returns>The property.</returns>
		public static VCardProperty FromList(string Name, IEnumerable<string> Values)
		{
			return new VCardProperty(Name)
			{
				RawValue = string.Join(',', Values.Where(Value => !string.IsNullOrWhiteSpace(Value)).Select(Value => VCardText.Escape(Value.Trim())))
			};
		}

		/// <summary>
		/// Creates a property whose value is a link, such as <c>xmpp:anna@example.org</c>. Links are not escaped.
		/// </summary>
		/// <param name="Name">Name of the property.</param>
		/// <param name="Uri">The link.</param>
		/// <returns>The property.</returns>
		public static VCardProperty FromUri(string Name, string Uri)
		{
			return new VCardProperty(Name)
			{
				RawValue = Uri.Trim()
			};
		}
	}
}
