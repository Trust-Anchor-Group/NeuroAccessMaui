using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	/// <summary>
	/// Represents a page in a KYC process containing fields and sections.
	/// </summary>
	public partial class KycPage : ObservableObject, IDisposable
	{
		private bool disposed;
		private readonly List<IDisposable> subscriptions = new();
		private IDictionary<string, string?> values = null!;

		public KycPage()
		{
			this.VisibleFieldsCollection = new FilteredObservableCollection<ObservableKycField>(this.AllFields, Field => Field.IsVisible);
			this.VisibleSectionsCollection = new FilteredObservableCollection<KycSection>(this.AllSections, Section => Section.IsVisible);
		}

		/// <summary>
		/// Gets the page identifier.
		/// </summary>
		public string Id { get; init; } = string.Empty;
		/// <summary>
		/// Gets or sets the localized page title.
		/// </summary>
		public KycLocalizedText? Title { get; set; }
		/// <summary>
		/// Gets or sets the localized page description.
		/// </summary>
		public KycLocalizedText? Description { get; set; }
		/// <summary>
		/// Gets or sets an optional page condition controlling visibility.
		/// </summary>
		public KycCondition? Condition { get; set; }

		/// <summary>
		/// Gets all fields directly contained in the page (excluding sections).
		/// </summary>
		public ObservableCollection<ObservableKycField> AllFields { get; } = new();
		/// <summary>
		/// Backing collection tracking fields whose <see cref="ObservableKycField.IsVisible"/> is true.
		/// </summary>
		public FilteredObservableCollection<ObservableKycField> VisibleFieldsCollection { get; }
		/// <summary>
		/// Gets the read-only collection of visible fields in the page.
		/// </summary>
		public ReadOnlyObservableCollection<ObservableKycField> VisibleFields => this.VisibleFieldsCollection;

		/// <summary>
		/// Gets all sections contained in the page.
		/// </summary>
		public ObservableCollection<KycSection> AllSections { get; } = new();
		/// <summary>
		/// Backing collection tracking sections whose <see cref="KycSection.IsVisible"/> is true.
		/// </summary>
		public FilteredObservableCollection<KycSection> VisibleSectionsCollection { get; }
		/// <summary>
		/// Gets the read-only collection of visible sections in the page.
		/// </summary>
		public ReadOnlyObservableCollection<KycSection> VisibleSections => this.VisibleSectionsCollection;

		/// <summary>
		/// Updates visibility flags for fields and sections based on current values.
		/// </summary>
		/// <param name="Values">Dictionary of current field values.</param>
		public void UpdateVisibilities(IDictionary<string, string?> Values)
		{
			foreach (ObservableKycField Field in this.AllFields)
			{
				Field.IsVisible = Field.Condition?.Evaluate(Values) ?? true;
			}
			foreach (KycSection Section in this.AllSections)
			{
				Section.UpdateVisibilities(Values);
			}

			this.VisibleFieldsCollection.Refresh();
			this.VisibleSectionsCollection.Refresh();
		}

		/// <summary>
		/// Gets a value indicating whether the page is visible based on current values and its condition.
		/// </summary>
		/// <param name="Values">Dictionary of current field values.</param>
		/// <returns>True if visible.</returns>
		public bool IsVisible(IDictionary<string, string?> Values)
		{
			return this.Condition?.Evaluate(Values) ?? true;
		}

		protected virtual void Dispose(bool Disposing)
		{
			if (this.disposed)
			{
				return;
			}
			if (Disposing)
			{
				this.VisibleFieldsCollection.Dispose();
				this.VisibleSectionsCollection.Dispose();
				this.subscriptions.ForEach(Sub => Sub.Dispose());
				this.subscriptions.Clear();
			}
			this.disposed = true;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Initializes subscriptions to field value changes for this page so page visibility and dependent field visibility are updated.
		/// </summary>
		/// <param name="Values">Dictionary of current field values.</param>
		internal void InitFieldValueNotifications(IDictionary<string, string?> Values)
		{
			this.values = Values;
			foreach (ObservableKycField Field in this.AllFields)
			{
				Field.PropertyChanged += this.Field_ValueChanged;
			}
			foreach (KycSection Section in this.AllSections)
			{
				foreach (ObservableKycField Field in Section.AllFields)
				{
					Field.PropertyChanged += this.Field_ValueChanged;
				}
			}
		}

		private void Field_ValueChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(ObservableKycField.RawValue) && Sender is ObservableKycField Field)
			{
				this.values[Field.Id] = Field.StringValue;
				this.UpdateVisibilities(this.values);
				if (Field.FieldType == FieldType.Country && string.Equals(Field.Id, "country", StringComparison.OrdinalIgnoreCase))
					this.HandleCountryFieldChanged(Field);
			}
		}

		private void HandleCountryFieldChanged(ObservableKycField CountryField)
		{
			if (CountryField is null)
				return;

			string CountryCode = CountryField.StringValue ?? string.Empty;
			if (CountryField.TryGetOwnerProcess(out KycProcess? Process))
			{
				Process.Values[CountryField.Id] = CountryCode;
				string PlaceholderExample = string.IsNullOrEmpty(CountryCode) ? string.Empty : PersonalNumberSchemes.DisplayStringForCountry(CountryCode) ?? string.Empty;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					foreach (KycPage Page in Process.Pages)
					{
						this.UpdatePersonalNumberFields(Page.VisibleFields, CountryCode, PlaceholderExample);
						foreach (KycSection Section in Page.VisibleSections)
							this.UpdatePersonalNumberFields(Section.VisibleFields, CountryCode, PlaceholderExample);
					}
				});
			}
		}

		private void UpdatePersonalNumberFields(IEnumerable<ObservableKycField> Fields, string CountryCode, string PlaceholderExample)
		{
			foreach (ObservableKycField Field in Fields)
			{
				if (!Field.IsVisible)
					continue;

				if (!MapsToPersonalNumber(Field))
					continue;

				this.ApplyPersonalNumberPlaceholder(Field, CountryCode, PlaceholderExample);
				Field.ForceSynchronousValidation();
				Field.ValidationTask.Run();
			}
		}

		private void ApplyPersonalNumberPlaceholder(ObservableKycField Field, string CountryCode, string PlaceholderExample)
		{
			if (Field.Placeholder is null)
				Field.Placeholder = new KycLocalizedText();

			string PlaceholderKey = string.IsNullOrEmpty(CountryCode) ? "en" : CountryCode;
			string PlaceholderValue = PlaceholderExample ?? string.Empty;
			Field.Placeholder.Add(PlaceholderKey, PlaceholderValue);
		}

		private static bool MapsToPersonalNumber(ObservableKycField Field)
		{
			foreach (KycMapping Mapping in Field.Mappings)
			{
				if (string.Equals(Mapping.Key, Constants.XmppProperties.PersonalNumber, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return string.Equals(Field.Id, "personalNumber", StringComparison.OrdinalIgnoreCase);
		}
	}
}
