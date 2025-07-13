using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public partial class KycPage : ObservableObject, IDisposable
	{
		private bool disposed;
		private readonly List<IDisposable> subscriptions = new();
		private IDictionary<string, string?> values = null!;

		public KycPage()
		{
			this.VisibleFieldsCollection = new FilteredObservableCollection<KycField>(this.AllFields, Field => Field.IsVisible);
			this.VisibleSectionsCollection = new FilteredObservableCollection<KycSection>(this.AllSections, Section => Section.IsVisible);
		}

		public string Id { get; init; } = string.Empty;
		public KycLocalizedText? Title { get; set; }
		public KycLocalizedText? Description { get; set; }
		public KycCondition? Condition { get; set; }

		public ObservableCollection<KycField> AllFields { get; } = new();
		public FilteredObservableCollection<KycField> VisibleFieldsCollection { get; }
		public ReadOnlyObservableCollection<KycField> VisibleFields => this.VisibleFieldsCollection;

		public ObservableCollection<KycSection> AllSections { get; } = new();
		public FilteredObservableCollection<KycSection> VisibleSectionsCollection { get; }
		public ReadOnlyObservableCollection<KycSection> VisibleSections => this.VisibleSectionsCollection;

		public void UpdateVisibilities(IDictionary<string, string?> Values)
		{
			foreach (KycField Field in this.AllFields)
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

		internal void InitFieldValueNotifications(IDictionary<string, string?> Values)
		{
			this.values = Values;
			foreach (KycField Field in this.AllFields)
			{
				Field.PropertyChanged += this.Field_ValueChanged;
			}
			foreach (KycSection Section in this.AllSections)
			{
				foreach (KycField Field in Section.AllFields)
				{
					Field.PropertyChanged += this.Field_ValueChanged;
				}
			}
		}

		private void Field_ValueChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycField.RawValue) && Sender is KycField Field)
			{
				this.values[Field.Id] = Field.ValueString;
				this.UpdateVisibilities(this.values);
			}
		}
	}
}
