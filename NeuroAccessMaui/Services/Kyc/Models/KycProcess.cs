using System.Collections.ObjectModel;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Models
{
    /// <summary>
    /// Represents a parsed KYC process with pages, fields, and current values.
    /// </summary>
    public partial class KycProcess
    {
        /// <summary>
        /// Optional process-level localized name for display.
        /// </summary>
        public KycLocalizedText? Name { get; set; }

		/// <summary>
		/// Gets or sets the identity application policy for the process.
		/// </summary>
		public KycApplicationPolicy ApplicationPolicy { get; set; } = new KycApplicationPolicy();

		/// <summary>
		/// Gets or sets the evidence policy for the process.
		/// </summary>
		public KycEvidencePolicy EvidencePolicy { get; set; } = new KycEvidencePolicy();

		private readonly Dictionary<string, string?> values = new();

		/// <summary>
		/// Gets a modifiable dictionary of field values keyed by field identifier.
		/// </summary>
		public IDictionary<string, string?> Values => this.values;

		/// <summary>
		/// Gets the collection of pages in the KYC process.
		/// </summary>
		public ObservableCollection<KycPage> Pages { get; } = new();

		/// <summary>
		/// Initializes page value-change notifications.
		/// </summary>
		public void Initialize()
		{
			foreach (KycPage Page in this.Pages)
			{
				Page.InitFieldValueNotifications(this.values);
			}
		}

		/// <summary>
		/// Clears validation state (error messages and flags) across all fields.
		/// </summary>
		public void ClearValidation()
		{
			foreach (KycPage Page in this.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (Field is not null)
					{
						Field.ValidationText = null;
						Field.IsValid = true;
					}
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (Field is not null)
						{
							Field.ValidationText = null;
							Field.IsValid = true;
						}
					}
				}
			}
		}

		/// <summary>
		/// Determines if the process contains a mapping for a specific identity property.
		/// </summary>
		/// <param name="Mapping">Mapping key, e.g., "EMAIL" or "BDATE".</param>
		/// <returns>True if at least one visible field maps to the key.</returns>
		public bool HasMapping(string Mapping)
		{
			if (Mapping.Equals("BDATE", StringComparison.OrdinalIgnoreCase))
			{
				string[] DateMappings = ["BDAY", "BMONTH", "BYEAR"];

				return DateMappings.Select(f => this.FindMapping(f)).Any(f => f);
			}
			else if (Mapping.Equals("ORGREPBDATE", StringComparison.OrdinalIgnoreCase))
			{
				string[] DateMappings = ["ORGREPBDAY", "ORGREPBMONTH", "ORGREPBYEAR"];

				return DateMappings.Select(f => this.FindMapping(f)).Any(f => f);
			}
			else
			{
				return this.FindMapping(Mapping);
			}
		}

		/// <summary>
		/// Determines whether the first field matching an identity mapping can be edited.
		/// </summary>
		/// <param name="Mapping">The identity mapping key.</param>
		/// <returns><c>true</c> when a matching field exists and is editable; otherwise, <c>false</c>.</returns>
		public bool IsMappingEditable(string Mapping)
		{
			if (string.IsNullOrWhiteSpace(Mapping))
				return false;

			foreach (KycPage Page in this.Pages)
			{
				if (!Page.IsVisible(this.values))
					continue;

				IEnumerable<ObservableKycField> Fields = Page.AllFields.Concat(
					Page.AllSections.SelectMany(Section => Section.AllFields));
				foreach (ObservableKycField Field in Fields)
				{
					if (Field.IsVisible && FieldMatchesMapping(Field, Mapping))
						return Field.IsEditable;
				}
			}

			return false;
		}

		private static bool FieldMatchesMapping(ObservableKycField Field, string Mapping)
		{
			foreach (KycMapping Map in Field.Mappings)
			{
				if (string.Equals(Map.Key, Mapping, StringComparison.OrdinalIgnoreCase))
					return true;

				if (Mapping.Equals(Constants.CustomXmppProperties.BirthDate, StringComparison.OrdinalIgnoreCase) &&
					(string.Equals(Map.Key, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(Map.Key, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(Map.Key, Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase)))
				{
					return true;
				}

				if (Mapping.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase) &&
					Map.Key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private bool FindMapping(string Mapping)
		{
			foreach (KycPage Page in this.Pages)
			{
				if (Page.AllFields.Any(f => f.Mappings.Any(m => m.Key == Mapping)) ||
					Page.AllSections.Any(s => s.AllFields.Any(f => f.Mappings.Any(m => m.Key == Mapping))))
					return true;
			}

			return false;
		}
	}
}
