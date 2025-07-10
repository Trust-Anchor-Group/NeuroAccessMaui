using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{
	public class KycFieldTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TextFieldTemplate { get; set; }
		public DataTemplate DateFieldTemplate { get; set; }
		public DataTemplate PickerFieldTemplate { get; set; }
		public DataTemplate BooleanFieldTemplate { get; set; }
		// Add more as needed

		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is not KycField field) return this.TextFieldTemplate;
			return field.Type switch
			{
				"date" => this.DateFieldTemplate,
				"picker" => this.PickerFieldTemplate,
				"boolean" => this.BooleanFieldTemplate,
				// Add more here
				_ => this.TextFieldTemplate
			};
		}
	}
}
