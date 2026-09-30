using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Displays and filters the production list of phone calling-code countries.
	/// </summary>
	public partial class PhoneCountrySelectionView : ContentView
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneCountrySelectionView"/> class.
		/// </summary>
		public PhoneCountrySelectionView()
		{
			this.InitializeComponent();
			this.InnerSearchBar.Text = string.Empty;
		}

		/// <summary>
		/// Occurs when a country is selected.
		/// </summary>
		public event EventHandler<ISO_3166_Country>? CountrySelected;

		private void SearchBar_TextChanged(object? Sender, TextChangedEventArgs e)
		{
			string SearchText = e.NewTextValue ?? string.Empty;
			if (string.IsNullOrWhiteSpace(SearchText))
			{
				this.InnerListView.ItemsSource = ISO_3166_1.Countries;
				return;
			}

			this.InnerListView.ItemsSource = ISO_3166_1.Countries.Where(Country =>
				Country.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Country.Alpha2, SearchText, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Country.Alpha3, SearchText, StringComparison.OrdinalIgnoreCase) ||
				Country.DialCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
		}

		private void InnerListView_SelectionChanged(object? Sender, SelectionChangedEventArgs e)
		{
			if (e.CurrentSelection.FirstOrDefault() is not ISO_3166_Country SelectedCountry)
				return;

			this.InnerListView.SelectedItem = null;
			this.CountrySelected?.Invoke(this, SelectedCountry);
		}
	}
}
