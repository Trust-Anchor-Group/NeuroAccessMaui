using System.Collections.ObjectModel;

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
	}
}
