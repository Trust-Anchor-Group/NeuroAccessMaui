namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Groups related catalog navigation items beneath one heading.
	/// </summary>
	public sealed class StoryCatalogGroupViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StoryCatalogGroupViewModel"/> class.
		/// </summary>
		/// <param name="Name">The group heading.</param>
		/// <param name="Items">The ordered items in the group.</param>
		public StoryCatalogGroupViewModel(string Name, IReadOnlyList<CatalogNavigationItemViewModel> Items)
		{
			this.Name = Name;
			this.Items = Items;
		}

		/// <summary>Gets the group heading.</summary>
		public string Name { get; }

		/// <summary>Gets the ordered group items.</summary>
		public IReadOnlyList<CatalogNavigationItemViewModel> Items { get; }
	}
}
