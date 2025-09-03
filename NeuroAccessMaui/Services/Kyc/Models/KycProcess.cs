using System.Collections.ObjectModel;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public partial class KycProcess
	{
		private readonly Dictionary<string, string?> values = new();

		public IDictionary<string, string?> Values => this.values;

		public ObservableCollection<KycPage> Pages { get; } = new();

		public void Initialize()
		{
			foreach (KycPage Page in this.Pages)
			{
				Page.InitFieldValueNotifications(this.values);
			}
		}

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

		public bool HasMapping(string Mapping)
		{
			if (Mapping.Equals("BDATE", StringComparison.OrdinalIgnoreCase))
			{
				string[] DateMappings = ["BDAY", "BMONTH", "BYEAR"];

				if (DateMappings.Select(f => this.FindMapping(f)).Any(f => f))
					return true;
				return false;
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
