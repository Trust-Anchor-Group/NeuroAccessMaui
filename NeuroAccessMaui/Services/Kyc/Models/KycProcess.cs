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
	}
}
