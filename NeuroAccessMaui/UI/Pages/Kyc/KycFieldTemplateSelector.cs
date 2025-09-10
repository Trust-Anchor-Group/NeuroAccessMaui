using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	public class KycFieldTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? TextFieldTemplate { get; set; }
		public DataTemplate? DateFieldTemplate { get; set; }
		public DataTemplate? PickerFieldTemplate { get; set; }
		public DataTemplate? BooleanFieldTemplate { get; set; }
		public DataTemplate? IntegerFieldTemplate { get; set; }
		public DataTemplate? DecimalFieldTemplate { get; set; }
		public DataTemplate? RadioFieldTemplate { get; set; }
		public DataTemplate? CountryFieldTemplate { get; set; }
		public DataTemplate? CheckboxFieldTemplate { get; set; }
		public DataTemplate? FileUploadFieldTemplate { get; set; }
		public DataTemplate? ImageUploadFieldTemplate { get; set; }
		public DataTemplate? LabelFieldTemplate { get; set; }
		public DataTemplate? InfoFieldTemplate { get; set; }

		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			if (item is not ObservableKycField Field) return this.TextFieldTemplate;
			return Field.FieldType switch
			{
				FieldType.Boolean => this.BooleanFieldTemplate,
				FieldType.Date => this.DateFieldTemplate,
				FieldType.Integer => this.IntegerFieldTemplate ?? this.TextFieldTemplate,
				FieldType.Decimal => this.DecimalFieldTemplate ?? this.TextFieldTemplate,
				FieldType.Picker => this.PickerFieldTemplate,
				FieldType.Radio => this.RadioFieldTemplate ?? this.PickerFieldTemplate,
				FieldType.Country => this.CountryFieldTemplate ?? this.PickerFieldTemplate,
				FieldType.Gender => this.PickerFieldTemplate,
				FieldType.Checkbox => this.CheckboxFieldTemplate ?? this.PickerFieldTemplate,
				FieldType.File => this.FileUploadFieldTemplate,
				FieldType.Image => this.ImageUploadFieldTemplate,
				FieldType.Label => this.LabelFieldTemplate ?? this.TextFieldTemplate,
				FieldType.Info => this.InfoFieldTemplate ?? this.TextFieldTemplate,
				_ => this.TextFieldTemplate
			};
		}
	}
}
