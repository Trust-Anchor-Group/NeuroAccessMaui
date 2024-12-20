﻿

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{

	/// <summary>
	/// Selects a template based on the type of the parameter.
	/// </summary>
	public class ParameterTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? BooleanTemplate { get; set; }
		public DataTemplate? DateTemplate { get; set; }
		public DataTemplate? DurationTemplate { get; set; }
		public DataTemplate? StringTemplate { get; set; }
		public DataTemplate? NumericalTemplate { get; set; }
		public DataTemplate? TimeTemplate { get; set; }
		public DataTemplate? DefaultTemplate { get; set; }

		// Add other templates if needed

		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			return item switch
			{
				ObservableBooleanParameter => this.BooleanTemplate ?? this.DefaultTemplate,
				ObservableDateParameter => this.DateTemplate ?? this.DefaultTemplate,
				ObservableDurationParameter => this.DurationTemplate ?? this.DefaultTemplate,
				ObservableStringParameter => this.StringTemplate ?? this.DefaultTemplate,
				ObservableNumericalParameter => this.NumericalTemplate ?? this.DefaultTemplate,
				ObservableTimeParameter => this.TimeTemplate ?? this.DefaultTemplate,
				// Add other parameter type checks here...
				_ => this.DefaultTemplate
			};
		}
	}
}
