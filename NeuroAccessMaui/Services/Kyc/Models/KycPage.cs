using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
			}
		}
	}
}
