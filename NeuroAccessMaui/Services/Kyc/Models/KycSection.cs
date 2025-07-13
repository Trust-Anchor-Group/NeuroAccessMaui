using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public partial class KycSection : ObservableObject, IDisposable
	{
		private bool disposed;
		private readonly List<IDisposable> subscriptions = new();

		public KycSection()
		{
			this.VisibleFieldsCollection = new FilteredObservableCollection<KycField>(this.AllFields, Field => Field.IsVisible);
		}

		public KycLocalizedText? Label { get; set; }
		public KycLocalizedText? Description { get; set; }

		public ObservableCollection<KycField> AllFields { get; } = new();
		public FilteredObservableCollection<KycField> VisibleFieldsCollection { get; }
		public ReadOnlyObservableCollection<KycField> VisibleFields => this.VisibleFieldsCollection;

		public int VisibleFieldsCount => this.VisibleFields.Count;
		public bool IsVisible { get; private set; } = true;

		public void UpdateVisibilities(IDictionary<string, string?> Values)
		{
			foreach (KycField Field in this.AllFields)
			{
				Field.IsVisible = Field.Condition?.Evaluate(Values) ?? true;
			}
			this.IsVisible = this.VisibleFields.Count > 0;
			this.VisibleFieldsCollection.Refresh();
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
	}
}
