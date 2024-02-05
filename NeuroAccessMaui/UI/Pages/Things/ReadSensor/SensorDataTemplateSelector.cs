using NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model;

namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor
{
	/// <summary>
	/// Data Template Selector, based on Sensor Data.
	/// </summary>
	public class SensorDataTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for headers
		/// </summary>
		public DataTemplate? HeaderTemplate { get; set; }

		/// <summary>
		/// Template to use for fields
		/// </summary>
		public DataTemplate? FieldTemplate { get; set; }

		/// <summary>
		/// Template to use for graphs
		/// </summary>
		public DataTemplate? GraphTemplate { get; set; }

		/// <summary>
		/// Template to use for informative tags
		/// </summary>
		public DataTemplate? TagTemplate { get; set; }

		/// <summary>
		/// Template to use for errors
		/// </summary>
		public DataTemplate? ErrorTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate? DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			if (item is HeaderModel)
				return this.HeaderTemplate ?? this.DefaultTemplate;
			else if (item is FieldModel)
				return this.FieldTemplate ?? this.DefaultTemplate;
			else if (item is GraphModel)
				return this.GraphTemplate ?? this.DefaultTemplate;
			else if (item is HumanReadableTag)
				return this.TagTemplate ?? this.DefaultTemplate;
			else if (item is ErrorModel)
				return this.ErrorTemplate ?? this.DefaultTemplate;
			else
				return this.DefaultTemplate;
		}
	}
}
