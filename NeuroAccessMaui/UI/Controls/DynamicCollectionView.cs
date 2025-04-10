using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Represents the direction in which sorting is performed.
	/// </summary>
	public enum SortDirection
	{
		/// <summary>
		/// Items are sorted in ascending order.
		/// </summary>
		Ascending,
		/// <summary>
		/// Items are sorted in descending order.
		/// </summary>
		Descending
	}

	/// <summary>
	/// A CollectionView that supports dynamic filtering and sorting of its items.
	/// </summary>
	/// <remarks>
	/// The <see cref="DynamicCollectionView"/> class extends the standard CollectionView by allowing you to apply
	/// a filter and sort operations on an original items source. Changes to the original items source or the filter/sort
	/// criteria automatically trigger a refresh of the view.
	/// </remarks>
	public class DynamicCollectionView : CollectionView
	{
		// OriginalItemsSource: the unmodified source collection.
		public static readonly BindableProperty OriginalItemsSourceProperty =
			  BindableProperty.Create(
					 nameof(OriginalItemsSource),
					 typeof(IEnumerable),
					 typeof(DynamicCollectionView),
					 default(IEnumerable),
					 propertyChanged: OnOriginalItemsSourceChanged);

		public IEnumerable OriginalItemsSource
		{
			get => (IEnumerable)this.GetValue(OriginalItemsSourceProperty);
			set => this.SetValue(OriginalItemsSourceProperty, value);
		}

		// Filter: A predicate to filter items.
		public static readonly BindableProperty FilterProperty =
			  BindableProperty.Create(
					 nameof(Filter),
					 typeof(Predicate<object>),
					 typeof(DynamicCollectionView),
					 default(Predicate<object>),
					 propertyChanged: OnFilterChanged);

		public Predicate<object> Filter
		{
			get => (Predicate<object>)this.GetValue(FilterProperty);
			set => this.SetValue(FilterProperty, value);
		}

		// SortComparison: A comparison delegate to sort items.
		public static readonly BindableProperty SortComparisonProperty =
			  BindableProperty.Create(
					 nameof(SortComparison),
					 typeof(Comparison<object>),
					 typeof(DynamicCollectionView),
					 default(Comparison<object>),
					 propertyChanged: OnSortComparisonChanged);

		public Comparison<object> SortComparison
		{
			get => (Comparison<object>)this.GetValue(SortComparisonProperty);
			set => this.SetValue(SortComparisonProperty, value);
		}

		// SortPropertyName: The name of the property to sort by.
		public static readonly BindableProperty SortPropertyNameProperty =
			  BindableProperty.Create(
					 nameof(SortPropertyName),
					 typeof(string),
					 typeof(DynamicCollectionView),
					 default(string),
					 propertyChanged: OnSortPropertyNameChanged);

		public string SortPropertyName
		{
			get => (string)this.GetValue(SortPropertyNameProperty);
			set => this.SetValue(SortPropertyNameProperty, value);
		}

		// SortDirection: Ascending or Descending sort.
		public static readonly BindableProperty SortDirectionProperty =
			  BindableProperty.Create(
					 nameof(SortDirection),
					 typeof(SortDirection),
					 typeof(DynamicCollectionView),
					 SortDirection.Ascending,
					 propertyChanged: OnSortDirectionChanged);

		public SortDirection SortDirection
		{
			get => (SortDirection)this.GetValue(SortDirectionProperty);
			set => this.SetValue(SortDirectionProperty, value);
		}

		// Handle changes to the original items source.
		private static void OnOriginalItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
		{
			DynamicCollectionView Control = (DynamicCollectionView)bindable;

			// Unsubscribe from previous collection changes if applicable.
			if (oldValue is INotifyCollectionChanged OldNotify)
			{
				OldNotify.CollectionChanged -= Control.OriginalItemsSource_CollectionChanged;
			}

			// Subscribe to the new collection if it supports notifications.
			if (newValue is INotifyCollectionChanged NewNotify)
			{
				NewNotify.CollectionChanged += Control.OriginalItemsSource_CollectionChanged;
			}

			Control.ApplyFilterAndSort();
		}

		// When the underlying collection changes, refresh the view.
		private void OriginalItemsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.ApplyFilterAndSort();
		}

		// Handle changes to the Filter property.
		private static void OnFilterChanged(BindableObject bindable, object oldValue, object newValue)
		{
			DynamicCollectionView Control = (DynamicCollectionView)bindable;
			Control.ApplyFilterAndSort();
		}

		// Handle changes to the SortComparison property.
		private static void OnSortComparisonChanged(BindableObject bindable, object oldValue, object newValue)
		{
			DynamicCollectionView Control = (DynamicCollectionView)bindable;
			Control.ApplyFilterAndSort();
		}

		// Handle changes to the SortPropertyName property.
		private static void OnSortPropertyNameChanged(BindableObject bindable, object oldValue, object newValue)
		{
			DynamicCollectionView Control = (DynamicCollectionView)bindable;
			Control.ApplyFilterAndSort();
		}

		// Handle changes to the SortDirection property.
		private static void OnSortDirectionChanged(BindableObject bindable, object oldValue, object newValue)
		{
			DynamicCollectionView Control = (DynamicCollectionView)bindable;
			Control.ApplyFilterAndSort();
		}

		/// <summary>
		/// Applies filtering and sorting to the OriginalItemsSource and assigns the result to the base ItemsSource.
		/// </summary>
		private void ApplyFilterAndSort()
		{
			if (this.OriginalItemsSource is null)
			{
				base.ItemsSource = null;
				return;
			}

			// Cast the original items to object.
			IEnumerable<object> Items = this.OriginalItemsSource.Cast<object>();

			// Apply filtering if a predicate is provided.
			if (this.Filter is not null)
			{
				Items = Items.Where(item => this.Filter(item));
			}

			// Apply sorting.
			// Priority: if a SortComparison is provided, use it. Otherwise, if SortPropertyName is set, use reflection.
			if (this.SortComparison is not null)
			{
				Items = Items.OrderBy(item => item, Comparer<object>.Create(this.SortComparison));
			}
			else if (!string.IsNullOrWhiteSpace(this.SortPropertyName))
			{
				if (this.SortDirection == SortDirection.Ascending)
				{
					Items = Items.OrderBy(item => GetPropertyValue(item, this.SortPropertyName));
				}
				else
				{
					Items = Items.OrderByDescending(item => GetPropertyValue(item, this.SortPropertyName));
				}
			}

			// Create an observable collection for the filtered/sorted items.
			ObservableCollection<object> TransformedCollection = new ObservableCollection<object>(Items);

			// Set the base ItemsSource to the new collection.
			base.ItemsSource = TransformedCollection;
		}

		/// <summary>
		/// Retrieves the value of the given property from an object via reflection.
		/// </summary>
		private static object? GetPropertyValue(object obj, string propertyName)
		{
			if (obj is null || string.IsNullOrWhiteSpace(propertyName))
				return null;

			PropertyInfo? Prop = obj.GetType().GetProperty(propertyName);
			return Prop?.GetValue(obj, null);
		}
	}
}
