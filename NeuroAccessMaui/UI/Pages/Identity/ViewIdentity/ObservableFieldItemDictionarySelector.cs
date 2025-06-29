using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Pages.Identity.ObjectModel;


namespace NeuroAccessMaui.UI.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// A single mapping of an ObservableFieldItem.Key → DataTemplate.
	/// </summary>
	public class TemplateMapping
	{
		/// <summary>
		/// The string key to match, e.g. Constants.CustomXmppProperties.Neuro_Id.
		/// </summary>
		public required string Key { get; set; }

		/// <summary>
		/// The template to use when an item’s Key equals <see cref="Key"/>.
		/// </summary>
		public required DataTemplate Template { get; set; }
	}
	public class ObservableFieldItemDictionarySelector : DataTemplateSelector
	{
		// 1) Use ObservableCollection so we can hook CollectionChanged
		public ObservableCollection<TemplateMapping> Templates { get; }
			= [];

		public DataTemplate? DefaultTemplate { get; set; }

		// backing template map
		private Dictionary<string, DataTemplate>? templateMap;

		public ObservableFieldItemDictionarySelector()
		{
			this.Templates.CollectionChanged += this.OnTemplatesChanged;
		}

		void OnTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.templateMap = null;
		}

		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			// populate template map
			this.templateMap ??= this.Templates
					  .Where(m => !string.IsNullOrEmpty(m.Key) && m.Template is not null)
					  .ToDictionary(m => m.Key!, m => m.Template!);

			if (item is ObservableFieldItem Field
				&& !string.IsNullOrEmpty(Field.Key)
				&& this.templateMap.TryGetValue(Field.Key, out DataTemplate? Template))
			{
				return Template;
			}

			return this.DefaultTemplate;
		}
	}
}
