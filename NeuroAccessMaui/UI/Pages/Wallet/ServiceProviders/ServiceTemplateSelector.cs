namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Data Template Selector, based on Service information.
	/// </summary>
	public class ServiceTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template for services that will only display an image.
		/// </summary>
		public DataTemplate? ImageOnlyTemplate { get; set; }

		/// <summary>
		/// Template for services that will only display text.
		/// </summary>
		public DataTemplate? TextOnlyTemplate { get; set; }

		/// <summary>
		/// Template for services that will display both image and texts.
		/// </summary>
		public DataTemplate? ImageAndTextTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate? DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			if (item is ServiceProviderViewModel Service)
			{
				bool ShowImage = Service.ShowImage;
				bool ShowText = Service.ShowText;

				if (ShowImage && ShowText)
					return this.ImageAndTextTemplate;
				else if (ShowImage)
					return this.ImageOnlyTemplate;
				else if (ShowText)
					return this.TextOnlyTemplate;
			}

			return this.DefaultTemplate;
		}
	}
}
