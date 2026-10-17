using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Draws the rows of a list as one card. When <see cref="IsEnabledProperty"/> is set on an <see cref="ItemsView"/>, or on a
	/// layout that shows its rows with <see cref="BindableLayout"/>, every <see cref="IListCardItem"/> in its items source is
	/// told where it sits each time the list changes, so the first row can round the top of the card, the last row the
	/// bottom, and the rows in between can draw their dividers.
	/// </summary>
	public static class ListCard
	{
		/// <summary>
		/// Identifies the attached property that keeps the positions of a list's rows up to date.
		/// </summary>
		public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
			"IsEnabled",
			typeof(bool),
			typeof(ListCard),
			false,
			propertyChanged: OnIsEnabledChanged);

		private static readonly BindableProperty trackerProperty = BindableProperty.CreateAttached(
			"Tracker",
			typeof(PositionTracker),
			typeof(ListCard),
			null);

		/// <summary>
		/// Gets if the rows of a list are drawn as one card.
		/// </summary>
		/// <param name="View">The items view, or the layout with bindable rows.</param>
		/// <returns>If row positions are kept up to date.</returns>
		public static bool GetIsEnabled(BindableObject View)
		{
			return (bool)View.GetValue(IsEnabledProperty);
		}

		/// <summary>
		/// Sets if the rows of a list are drawn as one card.
		/// </summary>
		/// <param name="View">The items view, or the layout with bindable rows.</param>
		/// <param name="Value">If row positions are kept up to date.</param>
		public static void SetIsEnabled(BindableObject View, bool Value)
		{
			View.SetValue(IsEnabledProperty, Value);
		}

		/// <summary>
		/// Tells every card item in a list where it sits.
		/// </summary>
		/// <param name="Items">The items, in the order they are shown.</param>
		public static void UpdatePositions(IEnumerable? Items)
		{
			if (Items is null)
				return;

			List<IListCardItem> CardItems = [];

			foreach (object? Item in Items)
			{
				if (Item is IListCardItem CardItem)
					CardItems.Add(CardItem);
			}

			int c = CardItems.Count;

			for (int i = 0; i < c; i++)
			{
				ListCardPosition Position;

				if (c == 1)
					Position = ListCardPosition.Only;
				else if (i == 0)
					Position = ListCardPosition.First;
				else if (i == c - 1)
					Position = ListCardPosition.Last;
				else
					Position = ListCardPosition.Middle;

				if (CardItems[i].CardPosition != Position)
					CardItems[i].CardPosition = Position;
			}
		}

		private static void OnIsEnabledChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			BindableProperty? SourceProperty = Bindable switch
			{
				ItemsView => ItemsView.ItemsSourceProperty,
				Layout => BindableLayout.ItemsSourceProperty,
				_ => null
			};

			if (SourceProperty is null)
				return;

			if (Bindable.GetValue(trackerProperty) is PositionTracker Existing)
			{
				Existing.Detach();
				Bindable.ClearValue(trackerProperty);
			}

			if ((bool)NewValue)
				Bindable.SetValue(trackerProperty, new PositionTracker(Bindable, SourceProperty));
		}

		/// <summary>
		/// Follows a list's items source, and the changes to it, and updates the positions of its items.
		/// </summary>
		private sealed class PositionTracker
		{
			private readonly BindableObject view;
			private readonly BindableProperty sourceProperty;
			private INotifyCollectionChanged? collection;

			/// <summary>
			/// Starts following a list.
			/// </summary>
			/// <param name="View">The items view, or the layout with bindable rows.</param>
			/// <param name="SourceProperty">The property that holds the list's items source.</param>
			public PositionTracker(BindableObject View, BindableProperty SourceProperty)
			{
				this.view = View;
				this.sourceProperty = SourceProperty;
				this.view.PropertyChanged += this.View_PropertyChanged;
				this.Attach(View.GetValue(SourceProperty) as IEnumerable);
			}

			/// <summary>
			/// Stops following the list.
			/// </summary>
			public void Detach()
			{
				this.view.PropertyChanged -= this.View_PropertyChanged;
				this.Attach(null);
			}

			private void View_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == this.sourceProperty.PropertyName)
					this.Attach(this.view.GetValue(this.sourceProperty) as IEnumerable);
			}

			private void Attach(IEnumerable? Source)
			{
				if (this.collection is not null)
					this.collection.CollectionChanged -= this.Collection_CollectionChanged;

				this.collection = Source as INotifyCollectionChanged;

				if (this.collection is not null)
					this.collection.CollectionChanged += this.Collection_CollectionChanged;

				UpdatePositions(Source);
			}

			private void Collection_CollectionChanged(object? Sender, NotifyCollectionChangedEventArgs e)
			{
				UpdatePositions(Sender as IEnumerable);
			}
		}
	}
}
