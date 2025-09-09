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
			else
			{
				return this.FindMapping(Mapping);
			}
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
