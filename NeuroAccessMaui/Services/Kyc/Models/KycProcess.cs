using System.Collections.ObjectModel;
using System.Globalization;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public partial class KycProcess
	{
		private readonly Dictionary<string, string?> values = new();

		public IDictionary<string, string?> Values => this.values;

		public ObservableCollection<KycPage> Pages { get; } = new();

		public void Initialize()
		{
			foreach (KycPage Page in this.Pages)
			{
				Page.InitFieldValueNotifications(this.values);
			}
		}

		public void ClearValidation()
		{
			foreach (KycPage Page in this.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (Field is not null)
					{
						Field.ValidationText = null;
						Field.IsValid = true;
					}
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (Field is not null)
						{
							Field.ValidationText = null;
							Field.IsValid = true;
						}
					}
				}
			}
		}

		public void SaveFieldsToStorage()
		{
			string[] FieldValues = new string[this.Pages.Sum(p => p.AllFields.Count) + this.Pages.Sum(p => p.AllSections.Sum(s => s.AllFields.Count))];

			int Index = 0;

			foreach (KycPage Page in this.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (!string.IsNullOrEmpty(Field.StringValue))
					{
						FieldValues[Index++] = Field.StringValue;
					}
					else
					{
						FieldValues[Index++] = string.Empty; // Ensure all fields are represented
					}
				}

				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if(!string.IsNullOrEmpty(Field.StringValue))
						{
							FieldValues[Index++] = Field.StringValue;
						}
						else
						{
							FieldValues[Index++] = string.Empty; // Ensure all fields are represented
						}
					}
				}
			}

			// Save to storage
			ServiceRef.TagProfile.KycFieldValues = FieldValues;
		}

		public void LoadFieldsAsync()
		{
			string[]? Fields = ServiceRef.TagProfile.KycFieldValues;
			int Index = 0;

			if (Fields is not null)
			{
				foreach (KycPage Page in this.Pages)
				{
					foreach (ObservableKycField Field in Page.AllFields)
					{
						this.SetFieldValue(Field, Fields[Index++]);
					}

					foreach (KycSection Section in Page.AllSections)
					{
						foreach (ObservableKycField Field in Section.AllFields)
						{
							this.SetFieldValue(Field, Fields[Index++]);
						}
					}
				}
			}
		}

		private void SetFieldValue(ObservableKycField Field, string Value)
		{
			switch (Field)
			{
				case ObservableDateField DateField:
					if (Value.Equals("now()", StringComparison.OrdinalIgnoreCase))
						DateField.DateValue = DateTime.Today;
					else if (DateTime.TryParse(Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Dt))
						DateField.DateValue = Dt;
					break;
				case ObservableBooleanField BoolField:
					if (bool.TryParse(Value, out bool Bv))
						BoolField.BoolValue = Bv;
					break;
				case ObservableIntegerField IntField:
					if (int.TryParse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int Iv))
						IntField.IntValue = Iv;
					break;
				case ObservableDecimalField DecField:
					if (decimal.TryParse(Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal Dv))
						DecField.DecimalValue = Dv;
					break;
				default:
					Field.StringValue = Value;
					break;
			}
		}
	}
}
