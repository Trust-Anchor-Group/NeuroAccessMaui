using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NeuroAccessMaui.UI.MVVM
{
	/// <summary>
	/// Provides a live, read-only filtered view of an ObservableCollection&lt;T&gt; that stays in sync and maintains order.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	public class FilteredObservableCollection<T> : ReadOnlyObservableCollection<T>, IDisposable
		where T : INotifyPropertyChanged
	{
		private readonly ObservableCollection<T> source;
		private readonly Func<T, bool> predicate;
		private readonly Subscription subscription = new Subscription();
		private bool disposed;

		public FilteredObservableCollection(ObservableCollection<T> Source, Func<T, bool> Predicate)
			: base(new ObservableCollection<T>(Source.Where(Predicate)))
		{
			this.source = Source;
			this.predicate = Predicate;

			foreach (T Item in this.source)
			{
				this.AddPropertyChangedHandler(Item);
			}

			this.source.CollectionChanged += this.Source_CollectionChanged;
			this.subscription.Add(() => this.source.CollectionChanged -= this.Source_CollectionChanged);
		}

		private void Source_CollectionChanged(object? Sender, NotifyCollectionChangedEventArgs E)
		{
			if (E.OldItems is not null)
			{
				foreach (T Item in E.OldItems)
				{
					this.RemovePropertyChangedHandler(Item);
				}
			}
			if (E.NewItems is not null)
			{
				foreach (T Item in E.NewItems)
				{
					this.AddPropertyChangedHandler(Item);
				}
			}
			this.Refresh();
		}

		private void Item_PropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			this.Refresh();
		}

		private void AddPropertyChangedHandler(T Item)
		{
			Item.PropertyChanged += this.Item_PropertyChanged;
			this.subscription.Add(() => Item.PropertyChanged -= this.Item_PropertyChanged);
		}

		private void RemovePropertyChangedHandler(T Item)
		{
			Item.PropertyChanged -= this.Item_PropertyChanged;
		}

		/// <summary>
		/// Refreshes the filtered collection to match the source according to the predicate, preserving order.
		/// </summary>
		public void Refresh()
		{
			if (this.disposed)
			{
				return;
			}

			List<T> Filtered = this.source.Where(this.predicate).ToList();
			ObservableCollection<T> Target = (ObservableCollection<T>)this.Items;

			foreach (T Item in Target.ToList())
			{
				if (!Filtered.Contains(Item))
				{
					Target.Remove(Item);
				}
			}

			for (int Index = 0; Index < Filtered.Count; Index++)
			{
				T Item = Filtered[Index];
				if (Index >= Target.Count)
				{
					Target.Add(Item);
				}
				else if (!EqualityComparer<T>.Default.Equals(Target[Index], Item))
				{
					Target.Remove(Item);
					Target.Insert(Index, Item);
				}
			}

			while (Target.Count > Filtered.Count)
			{
				Target.RemoveAt(Target.Count - 1);
			}
		}

		public void Dispose()
		{
			this.Dispose(Disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool Disposing)
		{
			if (!this.disposed)
			{
				if (Disposing)
				{
					this.subscription.Dispose();
				}
				this.disposed = true;
			}
		}

		/// <summary>
		/// Helper disposable for event cleanup. Not sealed: follows dispose pattern for best practice.
		/// </summary>
		protected class Subscription : IDisposable
		{
			private bool disposed;
			private readonly List<Action> unsubscribers = new List<Action>();

			public void Add(Action Unsubscribe)
			{
				if (this.disposed)
				{
					return;
				}
				this.unsubscribers.Add(Unsubscribe);
			}

			public void Dispose()
			{
				this.Dispose(Disposing: true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool Disposing)
			{
				if (!this.disposed)
				{
					if (Disposing)
					{
						foreach (Action Unsub in this.unsubscribers)
						{
							Unsub();
						}
						this.unsubscribers.Clear();
					}
					this.disposed = true;
				}
			}
		}
	}
}
