using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public class KycLocalizedText : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> byLang = new(StringComparer.OrdinalIgnoreCase);
		public bool HasAny => this.byLang.Count > 0;

		public void Add(string Lang, string Value)
		{
			this.byLang[Lang] = Value;
			this.OnPropertyChanged(nameof(this.Text));
		}

		public string? Get(string? Lang)
		{
			if (Lang is not null && this.byLang.TryGetValue(Lang, out string? V)) return V;
			if (this.byLang.TryGetValue("en", out string? Ev)) return Ev;
			return this.byLang.Values.FirstOrDefault();
		}

		public string Text
		{
			get
			{
				string Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				return this.Get(Current) ?? string.Empty;
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string Name)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
		}
	}
}
