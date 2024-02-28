using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract.ObjectModel
{
	/// <summary>
	/// The data model for a contract part.
	/// </summary>
	public partial class PartModel : ObservableObject
	{
		/// <summary>
		/// Creates an instance of the <see cref="PartModel"/> class.
		/// </summary>
		/// <param name="key">A unique contract part key.</param>
		/// <param name="value">The contract part value.</param>
		/// <param name="BgColor">Background color.</param>
		/// <param name="LegalId">A legal id (optional).</param>
		public PartModel(string key, string value, string? LegalId = null)
		{
			this.Key = key;
			this.Value = value;
			this.LegalId = LegalId;
		}

		/// <summary>
		/// A unique contract part key.
		/// </summary>
		[ObservableProperty]
		private string? key;

		/// <summary>
		/// The contract part value.
		/// </summary>
		[ObservableProperty]
		private string? value;

		/// <summary>
		/// A legal id (optional).
		/// </summary>
		[ObservableProperty]
		private string? legalId;

		/// <summary>
		/// Gets or sets whether the contract part can sign a contract.
		/// </summary>
		[ObservableProperty]
		private bool canSign;

		/// <summary>
		/// The role to use when signing.
		/// </summary>
		[ObservableProperty]
		private string? signAsRole;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.SignAsRole):
					this.CanSign = !string.IsNullOrWhiteSpace(this.SignAsRole);
					break;
			}
		}

		/// <summary>
		/// The free text value of the 'sign as role'
		/// </summary>
		[ObservableProperty]
		private string? signAsRoleText;

		/// <summary>
		/// Gets or sets whether the format of the contract part is html or not.
		/// </summary>
		[ObservableProperty]
		private bool isHtml;
	}
}
