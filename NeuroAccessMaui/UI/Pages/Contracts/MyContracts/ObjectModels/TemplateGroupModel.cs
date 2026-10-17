using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// A row in the template picker: a template, with its older versions folded under it. Templates with the same title
	/// are versions of the same template.
	/// </summary>
	public partial class TemplateGroupModel : ObservableObject, IListCardItem
	{
		/// <summary>
		/// Creates a row for a template and its versions.
		/// </summary>
		/// <param name="Title">The title the versions share.</param>
		/// <param name="Versions">The versions, newest first. There is at least one.</param>
		public TemplateGroupModel(string Title, IReadOnlyList<ContractModel> Versions)
		{
			this.Title = Title;
			this.Key = GetKey(Title, Versions[0].ContractId);
			this.Monogram = GetMonogram(Title);

			List<TemplateVersionModel> Items = [];

			for (int i = 0; i < Versions.Count; i++)
				Items.Add(new TemplateVersionModel(Versions[i], i == 0));

			this.Versions = Items;
		}

		/// <summary>
		/// Identifies the row between reloads of the list: its title, or the template's ID when it has no title.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// The template's title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// The first letter or digit of the title, shown in the row's tile.
		/// </summary>
		public string Monogram { get; }

		/// <summary>
		/// The versions of the template, newest first.
		/// </summary>
		public IReadOnlyList<TemplateVersionModel> Versions { get; }

		/// <summary>
		/// The newest version, which is used when the row is tapped.
		/// </summary>
		public ContractModel Newest => this.Versions[0].Contract;

		/// <summary>
		/// If there is more than one version of the template.
		/// </summary>
		public bool HasVersions => this.Versions.Count > 1;

		/// <summary>
		/// How many versions there are, such as "3 versions".
		/// </summary>
		public string VersionsText => ServiceRef.Localizer[nameof(AppResources.TemplateVersionCount), this.Versions.Count];

		/// <summary>
		/// When the template was created, or when its newest version was.
		/// </summary>
		public string DetailText => this.HasVersions
			? ServiceRef.Localizer[nameof(AppResources.TemplateNewestOn), this.Versions[0].DateText]
			: ServiceRef.Localizer[nameof(AppResources.TemplateCreatedOn), this.Versions[0].DateText];

		/// <summary>
		/// What a screen reader announces for the row.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				List<string> Parts = [this.Title];

				if (this.IsNew)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.TemplateNew)]);

				if (this.HasVersions)
					Parts.Add(this.VersionsText);

				Parts.Add(this.DetailText);

				return string.Join(", ", Parts);
			}
		}

		/// <summary>
		/// What a screen reader announces for the share button.
		/// </summary>
		public string ShareDescription => ServiceRef.Localizer[nameof(AppResources.TemplateShareDescription), this.Title];

		/// <summary>
		/// What a screen reader announces for the button that shows or hides the versions.
		/// </summary>
		public string ToggleDescription => this.IsExpanded
			? ServiceRef.Localizer[nameof(AppResources.TemplateHideVersions), this.Title]
			: ServiceRef.Localizer[nameof(AppResources.TemplateShowVersions), this.Title];

		/// <summary>
		/// If the versions are shown under the row.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ToggleDescription))]
		private bool isExpanded;

		/// <summary>
		/// If the template arrived while the picker was open.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private bool isNew;

		/// <summary>
		/// Where the row sits in the list.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// Gets the key that groups templates into versions of the same template.
		/// </summary>
		/// <param name="Title">The template's title.</param>
		/// <param name="ContractId">The template's ID, used when it has no title.</param>
		/// <returns>The key.</returns>
		public static string GetKey(string? Title, string ContractId)
		{
			string Trimmed = Title?.Trim() ?? string.Empty;
			return Trimmed.Length == 0 ? ContractId : Trimmed.ToUpperInvariant();
		}

		/// <summary>
		/// Gets the first letter or digit of a title.
		/// </summary>
		/// <param name="Title">The title.</param>
		/// <returns>The letter or digit in upper case, or an empty string.</returns>
		private static string GetMonogram(string Title)
		{
			foreach (char Ch in Title)
			{
				if (char.IsLetterOrDigit(Ch))
					return char.ToUpper(Ch, CultureInfo.CurrentCulture).ToString();
			}

			return string.Empty;
		}
	}
}
