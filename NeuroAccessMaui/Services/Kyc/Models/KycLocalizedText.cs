using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Represents a set of localized strings, addressable by language code (e.g. "en", "sv").
	/// </summary>
	public class KycLocalizedText : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> byLang = new(StringComparer.OrdinalIgnoreCase);
		private string? primaryText;
		private string? primaryLanguage;

		/// <summary>
		/// Gets a value indicating whether any localized strings have been added.
		/// </summary>
		public bool HasAny => this.byLang.Count > 0;

		/// <summary>
		/// Adds or replaces a localized text value.
		/// </summary>
		/// <param name="Lang">Two-letter ISO language code.</param>
		/// <param name="Value">Localized text value.</param>
		public void Add(string Lang, string Value)
		{
			if (this.primaryText is null && !string.IsNullOrWhiteSpace(Value) && !this.byLang.ContainsKey(Lang))
			{
				this.primaryText = Value;
				this.primaryLanguage = Lang;
			}
			this.byLang[Lang] = Value;
			this.OnPropertyChanged(nameof(this.Text));
			this.OnPropertyChanged(nameof(this.PrimaryText));
		}

		/// <summary>
		/// Gets a localized value for a specific language.
		/// </summary>
		/// <param name="Lang">Two-letter ISO language code.</param>
		/// <returns>Localized text if available, English fallback if present, otherwise the first available value; or null.</returns>
		public string? Get(string? Lang)
		{
			if (Lang is not null && this.byLang.TryGetValue(Lang, out string? V)) return V;
			if (this.byLang.TryGetValue("en", out string? Ev)) return Ev;
			return this.byLang.Values.FirstOrDefault();
		}

		/// <summary>
		/// Gets the localized text for the current UI culture, or an appropriate fallback.
		/// </summary>
		public string Text
		{
			get
			{
				string Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				return this.Get(Current) ?? string.Empty;
			}
		}

		/// <summary>
		/// Gets the first localized text that was added, regardless of current culture.
		/// </summary>
		public string? PrimaryText => this.primaryText ?? this.byLang.Values.FirstOrDefault();

		/// <summary>
		/// Gets the language code associated with <see cref="PrimaryText"/>, if available.
		/// </summary>
		public string? PrimaryLanguage => this.primaryLanguage;

		public event PropertyChangedEventHandler? PropertyChanged;
		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="Name">Name of the changed property.</param>
		protected void OnPropertyChanged(string Name)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
		}
	}
}
