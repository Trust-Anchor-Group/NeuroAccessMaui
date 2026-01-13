using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Visual host that renders a collection of tabs and keeps its selection in sync with external consumers (e.g., <see cref="ViewSwitcher"/>).
	/// </summary>
	public class TabNavigationHost : ContentView
	{
		/// <summary>
		/// Backing store for the <see cref="ItemsSource"/> property.
		/// </summary>
		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(TabNavigationHost), default(IEnumerable), propertyChanged: OnItemsSourcePropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="ItemTemplate"/> property.
		/// </summary>
		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(TabNavigationHost), default(DataTemplate), propertyChanged: OnItemTemplatePropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="BadgeTemplate"/> property.
		/// </summary>
		public static readonly BindableProperty BadgeTemplateProperty =
			BindableProperty.Create(nameof(BadgeTemplate), typeof(DataTemplate), typeof(TabNavigationHost), default(DataTemplate), propertyChanged: OnBadgeTemplatePropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="SelectedIndex"/> property.
		/// </summary>
		public static readonly BindableProperty SelectedIndexProperty =
			BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(TabNavigationHost), 0, BindingMode.TwoWay, propertyChanged: OnSelectedIndexPropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="SelectedItem"/> property.
		/// </summary>
		public static readonly BindableProperty SelectedItemProperty =
			BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(TabNavigationHost), default(object), BindingMode.TwoWay, propertyChanged: OnSelectedItemPropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="TabPlacement"/> property.
		/// </summary>
		public static readonly BindableProperty TabPlacementProperty =
			BindableProperty.Create(nameof(TabPlacement), typeof(TabPlacement), typeof(TabNavigationHost), TabPlacement.Bottom, propertyChanged: OnTabPlacementPropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="IsScrollable"/> property.
		/// </summary>
		public static readonly BindableProperty IsScrollableProperty =
			BindableProperty.Create(nameof(IsScrollable), typeof(bool), typeof(TabNavigationHost), false, propertyChanged: OnIsScrollablePropertyChanged);

		/// <summary>
		/// Backing store for the <see cref="TouchEffectType"/> property.
		/// </summary>
		public static readonly BindableProperty TouchEffectTypeProperty =
			BindableProperty.Create(nameof(TouchEffectType), typeof(TabTouchEffectType), typeof(TabNavigationHost), TabTouchEffectType.None);

		/// <summary>
		/// Backing store for the <see cref="AnimationStyle"/> property.
		/// </summary>
		public static readonly BindableProperty AnimationStyleProperty =
			BindableProperty.Create(nameof(AnimationStyle), typeof(TabAnimationStyle), typeof(TabNavigationHost), TabAnimationStyle.None);

		private readonly Grid fixedGrid;
		private readonly ScrollView scrollView;
		private readonly StackLayout scrollLayout;
		private readonly List<TabNavigationItemContainer> itemContainers;
		private readonly List<object?> items;
		private int currentSelectedIndex = -1;

		private INotifyCollectionChanged? itemsSourceNotifier;
		private bool isInternalSelectionChange;
		private bool isApplyingSelection;

		/// <summary>
		/// Initializes a new instance of the <see cref="TabNavigationHost"/> class.
		/// </summary>
		public TabNavigationHost()
		{
			this.fixedGrid = new Grid
			{
				RowSpacing = 0,
				ColumnSpacing = 0,
				RowDefinitions = { new RowDefinition { Height = GridLength.Star } },
				ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } }
			};

			this.scrollLayout = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				Spacing = 0
			};

			this.scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = this.scrollLayout
			};

			this.itemContainers = new List<TabNavigationItemContainer>();
			this.items = new List<object?>();

			this.Content = this.fixedGrid;
		}

		/// <summary>
		/// Gets or sets the collection of items rendered as tabs.
		/// </summary>
		public IEnumerable? ItemsSource
		{
			get => (IEnumerable?)this.GetValue(ItemsSourceProperty);
			set => this.SetValue(ItemsSourceProperty, value);
		}

		/// <summary>
		/// Gets or sets the template used to create tab visuals for each item.
		/// </summary>
		public DataTemplate? ItemTemplate
		{
			get => (DataTemplate?)this.GetValue(ItemTemplateProperty);
			set => this.SetValue(ItemTemplateProperty, value);
		}

		/// <summary>
		/// Gets or sets an optional badge template used by the built-in tab templates.
		/// </summary>
		public DataTemplate? BadgeTemplate
		{
			get => (DataTemplate?)this.GetValue(BadgeTemplateProperty);
			set => this.SetValue(BadgeTemplateProperty, value);
		}

		/// <summary>
		/// Gets or sets the zero-based index of the selected tab.
		/// </summary>
		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, value);
		}

		/// <summary>
		/// Gets or sets the currently selected item.
		/// </summary>
		public object? SelectedItem
		{
			get => this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}

		/// <summary>
		/// Gets or sets the placement of the tab host (horizontal or vertical layout).
		/// </summary>
		public TabPlacement TabPlacement
		{
			get => (TabPlacement)this.GetValue(TabPlacementProperty);
			set => this.SetValue(TabPlacementProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the tab list should be scrollable instead of distributing items evenly.
		/// </summary>
		public bool IsScrollable
		{
			get => (bool)this.GetValue(IsScrollableProperty);
			set => this.SetValue(IsScrollableProperty, value);
		}

		/// <summary>
		/// Gets or sets the touch effect applied to tab interactions.
		/// </summary>
		public TabTouchEffectType TouchEffectType
		{
			get => (TabTouchEffectType)this.GetValue(TouchEffectTypeProperty);
			set => this.SetValue(TouchEffectTypeProperty, value);
		}

		/// <summary>
		/// Gets or sets the animation style used when the selected tab changes.
		/// </summary>
		public TabAnimationStyle AnimationStyle
		{
			get => (TabAnimationStyle)this.GetValue(AnimationStyleProperty);
			set => this.SetValue(AnimationStyleProperty, value);
		}

		private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			Host.OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable);
		}

		private static void OnItemTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (!Equals(oldValue, newValue))
			{
				Host.RebuildItems();
			}
		}

		private static void OnBadgeTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (!Equals(oldValue, newValue))
			{
				Host.RebuildItems();
			}
		}

		private static void OnSelectedIndexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (Host.isInternalSelectionChange)
			{
				return;
			}

			int OldIndex = (int)oldValue;
			int NewIndex = (int)newValue;

			if (OldIndex == NewIndex && Host.isApplyingSelection)
			{
				return;
			}

			Host.ApplySelectionFromProperty(NewIndex);
		}

		private static void OnSelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (Host.isInternalSelectionChange)
			{
				return;
			}

			if (ReferenceEquals(oldValue, newValue))
			{
				return;
			}

			Host.ApplySelectionFromSelectedItem(newValue);
		}

		private static void OnTabPlacementPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (!Equals(oldValue, newValue))
			{
				Host.UpdatePresenterRoot();
				Host.RebuildItems();
			}
		}

		private static void OnIsScrollablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TabNavigationHost Host = (TabNavigationHost)bindable;
			if (!Equals(oldValue, newValue))
			{
				Host.UpdatePresenterRoot();
				Host.RebuildItems();
			}
		}

		private void OnItemsSourceChanged(IEnumerable? OldItems, IEnumerable? NewItems)
		{
			this.DetachItemsSourceNotifier(OldItems as INotifyCollectionChanged);
			this.AttachItemsSourceNotifier(NewItems as INotifyCollectionChanged);
			this.RebuildItems();
		}

		private void AttachItemsSourceNotifier(INotifyCollectionChanged? Notifier)
		{
			if (Notifier is null)
			{
				this.itemsSourceNotifier = null;
				return;
			}

			this.itemsSourceNotifier = Notifier;
			this.itemsSourceNotifier.CollectionChanged += this.OnItemsSourceCollectionChanged;
		}

		private void DetachItemsSourceNotifier(INotifyCollectionChanged? Notifier)
		{
			if (Notifier is null)
			{
				return;
			}

			Notifier.CollectionChanged -= this.OnItemsSourceCollectionChanged;
			if (ReferenceEquals(this.itemsSourceNotifier, Notifier))
			{
				this.itemsSourceNotifier = null;
			}
		}

		private void OnItemsSourceCollectionChanged(object? Sender, NotifyCollectionChangedEventArgs E)
		{
			if (MainThread.IsMainThread)
			{
				this.RebuildItems();
			}
			else
			{
				MainThread.BeginInvokeOnMainThread(this.RebuildItems);
			}
		}

		private void RebuildItems()
		{
			this.isApplyingSelection = true;

			try
			{
				this.ClearPresenter();

				this.items.Clear();
				foreach (TabNavigationItemContainer Container in this.itemContainers)
				{
					Container.Dispose();
				}

				this.itemContainers.Clear();

				if (this.ItemsSource is null)
				{
					this.SetSelectionInternal(-1, null);
					return;
				}

				int Index = 0;
				foreach (object? Item in this.ItemsSource)
				{
					this.items.Add(Item);
					TabNavigationItemContainer Container = this.CreateContainer(Item, Index);
					this.itemContainers.Add(Container);
					this.AddContainerToPresenter(Container);
					Index++;
				}

				this.EnsureSelectionConsistency();
			}
			finally
			{
				this.isApplyingSelection = false;
			}
		}

		private void ClearPresenter()
		{
			if (this.IsScrollable)
			{
				this.scrollLayout.Children.Clear();
			}
			else
			{
				this.fixedGrid.Children.Clear();
				this.fixedGrid.RowDefinitions.Clear();
				this.fixedGrid.ColumnDefinitions.Clear();

				if (this.IsHorizontalPlacement())
				{
					this.fixedGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
				}
				else
				{
					this.fixedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
				}
			}
		}

		private void AddContainerToPresenter(TabNavigationItemContainer Container)
		{
			if (this.IsScrollable)
			{
				if (!this.scrollLayout.Children.Contains(Container))
				{
					this.scrollLayout.Children.Add(Container);
				}
			}
			else
			{
				if (this.IsHorizontalPlacement())
				{
					this.fixedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
					int ColumnIndex = this.fixedGrid.ColumnDefinitions.Count - 1;
					Grid.SetColumn(Container, ColumnIndex);
					Grid.SetRow(Container, 0);
				}
				else
				{
					this.fixedGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
					int RowIndex = this.fixedGrid.RowDefinitions.Count - 1;
					Grid.SetRow(Container, RowIndex);
					Grid.SetColumn(Container, 0);
				}

				if (!this.fixedGrid.Children.Contains(Container))
				{
					this.fixedGrid.Children.Add(Container);
				}
			}
		}

		private TabNavigationItemContainer CreateContainer(object? Item, int Index)
		{
			DataTemplate Template = this.ItemTemplate ?? this.ResolveDefaultTemplate();
			View ContentView = this.CreateContentView(Template, Item);

			TabNavigationItemContainer Container = new(this, Item, ContentView, Index);

			if (!this.IsScrollable)
			{
				if (this.IsHorizontalPlacement())
				{
					Container.HorizontalOptions = LayoutOptions.Fill;
				}
				else
				{
					Container.VerticalOptions = LayoutOptions.Fill;
				}
			}

			this.ApplyBadgeTemplate(Container);

			return Container;
		}

		private DataTemplate ResolveDefaultTemplate()
		{
			string ResourceKey = this.TabPlacement switch
			{
				TabPlacement.Top => "DefaultTopUnderlineTabTemplate",
				TabPlacement.Left => "DefaultVerticalTabTemplate",
				TabPlacement.Right => "DefaultVerticalTabTemplate",
				_ => "DefaultBottomPillTabTemplate"
			};

			if (Application.Current?.Resources.TryGetValue(ResourceKey, out object? ResourceValue) ?? false)
			{
				if (ResourceValue is DataTemplate Template)
				{
					return Template;
				}
			}

			return new DataTemplate(() =>
			{
				Label TitleLabel = new()
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					FontSize = 14
				};

				TitleLabel.SetBinding(Label.TextProperty, nameof(TabDefinition.Title));
				TitleLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
				return TitleLabel;
			});
		}

		private View CreateContentView(DataTemplate Template, object? BindingContext)
		{
			object? Created = Template.CreateContent();

			if (Created is ViewCell CreatedCell && CreatedCell.View is not null)
			{
				CreatedCell.View.BindingContext = BindingContext;
				return CreatedCell.View;
			}

			if (Created is View CreatedView)
			{
				CreatedView.BindingContext = BindingContext;
				return CreatedView;
			}

			throw new InvalidOperationException("Tab item template must produce a View.");
		}

		private void ApplyBadgeTemplate(TabNavigationItemContainer Container)
		{
			if (this.BadgeTemplate is null)
			{
				Container.SetCustomBadge(null);
				return;
			}

			View BadgeView = this.CreateContentView(this.BadgeTemplate, Container.Item);
			Container.SetCustomBadge(BadgeView);
		}

		private void EnsureSelectionConsistency()
		{
			if (this.items.Count == 0)
			{
				this.SetSelectionInternal(-1, null);
				return;
			}

			int ExistingIndex = this.SelectedIndex;

			if (ExistingIndex >= 0 && ExistingIndex < this.items.Count)
			{
				object? ExistingItem = this.items[ExistingIndex];
				this.SetSelectionInternal(ExistingIndex, ExistingItem);
				return;
			}

			if (this.SelectedItem is not null)
			{
				int LocatedIndex = this.GetIndexOfItem(this.SelectedItem);
				if (LocatedIndex >= 0)
				{
					object? LocatedItem = this.items[LocatedIndex];
					this.SetSelectionInternal(LocatedIndex, LocatedItem);
					return;
				}
			}

			this.SetSelectionInternal(0, this.items[0]);
		}

		private void SetSelectionInternal(int Index, object? Item)
		{
			this.isInternalSelectionChange = true;

			try
			{
				this.SetValue(SelectedIndexProperty, Index);
				this.SetValue(SelectedItemProperty, Item);
			}
			finally
			{
				this.isInternalSelectionChange = false;
			}

			this.ApplySelectionStates(Index);
		}

		private void ApplySelectionStates(int SelectedIndex)
		{
			int PreviousIndex = this.currentSelectedIndex;
			this.currentSelectedIndex = SelectedIndex;

			for (int i = 0; i < this.itemContainers.Count; i++)
			{
				TabNavigationItemContainer Container = this.itemContainers[i];
				bool IsSelected = i == SelectedIndex && SelectedIndex >= 0;
				Container.ApplySelectionState(IsSelected);

				if (Container.Item is TabDefinition Definition)
				{
					Definition.IsSelected = IsSelected;
				}
			}

			this.RunSelectionAnimation(PreviousIndex, SelectedIndex);
		}

		private void ApplySelectionFromProperty(int NewIndex)
		{
			if (NewIndex < 0 || NewIndex >= this.items.Count)
			{
				this.SetSelectionInternal(-1, null);
				return;
			}

			object? Item = this.items[NewIndex];
			this.SetSelectionInternal(NewIndex, Item);
		}

		private void ApplySelectionFromSelectedItem(object? SelectedItem)
		{
			if (SelectedItem is null)
			{
				this.SetSelectionInternal(-1, null);
				return;
			}

			int FoundIndex = this.GetIndexOfItem(SelectedItem);
			if (FoundIndex >= 0)
			{
				object? Item = this.items[FoundIndex];
				this.SetSelectionInternal(FoundIndex, Item);
			}
		}

		private int GetIndexOfItem(object? Item)
		{
			for (int i = 0; i < this.items.Count; i++)
			{
				object? Candidate = this.items[i];
				if (ReferenceEquals(Candidate, Item) || Equals(Candidate, Item))
				{
					return i;
				}
			}

			return -1;
		}

		private async Task HandleItemTappedAsync(TabNavigationItemContainer Container)
		{
			if (Container.Item is TabDefinition Definition && !Definition.IsEnabled)
			{
				return;
			}

			await this.ApplyTouchFeedbackAsync(Container);

			this.SetSelectionInternal(Container.Index, Container.Item);

			if (Container.Item is TabDefinition Descriptor && Descriptor.Command is not null)
			{
				if (Descriptor.Command.CanExecute(Descriptor.CommandParameter ?? Descriptor))
				{
					Descriptor.Command.Execute(Descriptor.CommandParameter ?? Descriptor);
				}
			}
		}

		private Task ApplyTouchFeedbackAsync(TabNavigationItemContainer Container)
		{
			switch (this.TouchEffectType)
			{
				case TabTouchEffectType.OpacityHighlight:
					return this.AnimateOpacityAsync(Container);
				case TabTouchEffectType.Scale:
					return this.AnimateScaleAsync(Container);
				default:
					return Task.CompletedTask;
			}
		}

		private async Task AnimateOpacityAsync(VisualElement Element)
		{
			const double TargetOpacity = 0.6;
			const uint Duration = 80;

			double OriginalOpacity = Element.Opacity;
			await Element.FadeToAsync(TargetOpacity, Duration, Easing.SinOut);
			await Element.FadeToAsync(OriginalOpacity, Duration, Easing.SinIn);
		}

		private async Task AnimateScaleAsync(VisualElement Element)
		{
			const double TargetScale = 0.92;
			const uint Duration = 80;

			double OriginalScale = Element.Scale;
			await Element.ScaleToAsync(TargetScale, Duration, Easing.CubicOut);
			await Element.ScaleToAsync(OriginalScale, Duration, Easing.CubicIn);
		}

		private void UpdatePresenterRoot()
		{
			bool IsHorizontal = this.IsHorizontalPlacement();

			if (this.IsScrollable)
			{
				this.scrollLayout.Orientation = IsHorizontal ? StackOrientation.Horizontal : StackOrientation.Vertical;
				this.scrollView.Orientation = IsHorizontal ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical;

				if (!ReferenceEquals(this.Content, this.scrollView))
				{
					this.Content = this.scrollView;
				}
			}
			else
			{
				if (IsHorizontal)
				{
					this.fixedGrid.RowDefinitions.Clear();
					this.fixedGrid.ColumnDefinitions.Clear();
					this.fixedGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
				}
				else
				{
					this.fixedGrid.RowDefinitions.Clear();
					this.fixedGrid.ColumnDefinitions.Clear();
					this.fixedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
				}

				if (!ReferenceEquals(this.Content, this.fixedGrid))
				{
					this.Content = this.fixedGrid;
				}
			}
		}

		private bool IsHorizontalPlacement()
		{
			return this.TabPlacement == TabPlacement.Top || this.TabPlacement == TabPlacement.Bottom;
		}

		private void RunSelectionAnimation(int PreviousIndex, int SelectedIndex)
		{
			if (this.AnimationStyle == TabAnimationStyle.None)
			{
				return;
			}

			if (SelectedIndex < 0 || SelectedIndex >= this.itemContainers.Count)
			{
				return;
			}

			TabNavigationItemContainer TargetContainer = this.itemContainers[SelectedIndex];

			switch (this.AnimationStyle)
			{
				case TabAnimationStyle.FadeScale:
					_ = this.AnimateFadeScaleAsync(TargetContainer);
					break;
				case TabAnimationStyle.UnderlineSlide:
					this.AnimateUnderlineTransition(PreviousIndex, SelectedIndex);
					break;
			}
		}

		private void AnimateUnderlineTransition(int PreviousIndex, int SelectedIndex)
		{
			if (PreviousIndex == SelectedIndex)
			{
				return;
			}

			// Default templates rely on visual state transitions to animate underline elements.
		}

		private async Task AnimateFadeScaleAsync(VisualElement Element)
		{
			const double TargetScale = 1.05;
			const uint ScaleInDuration = 120;
			const uint ScaleOutDuration = 140;

			double OriginalScale = Element.Scale;

			await Element.ScaleToAsync(TargetScale, ScaleInDuration, Easing.CubicOut);
			await Element.ScaleToAsync(OriginalScale, ScaleOutDuration, Easing.CubicIn);
		}

		private sealed class TabNavigationItemContainer : ContentView, IDisposable
		{
			private readonly TabNavigationHost host;
			private readonly TapGestureRecognizer tapRecognizer;
			private readonly INotifyPropertyChanged? notifier;
			private bool disposed;
			private View? customBadgeView;

			public TabNavigationItemContainer(TabNavigationHost Host, object? Item, View Content, int Index)
			{
				this.host = Host;
				this.Item = Item;
				this.Index = Index;
				this.tapRecognizer = new TapGestureRecognizer();
				this.tapRecognizer.Tapped += this.OnTapped;
				this.GestureRecognizers.Add(this.tapRecognizer);

				this.Content = Content;
				this.BindingContext = Item;
				Content.BindingContext = Item;

				if (Item is TabDefinition Definition)
				{
					this.IsEnabled = Definition.IsEnabled;
				}

				if (Item is INotifyPropertyChanged NotifyPropertyChanged)
				{
					this.notifier = NotifyPropertyChanged;
					this.notifier.PropertyChanged += this.OnItemPropertyChanged;
				}

				this.Padding = new Thickness(0);
				this.Margin = new Thickness(0);
			}

			public int Index { get; }

			public object? Item { get; }

			public void ApplySelectionState(bool isSelected)
			{
				VisualStateManager.GoToState(this, isSelected ? "Selected" : "Normal");
			}

			public void SetCustomBadge(View? badgeView)
			{
				this.customBadgeView = badgeView;

				Layout? BadgeHost = this.Content.FindByName<Layout>("CustomBadgeHost");
				if (BadgeHost is null)
				{
					return;
				}

				BadgeHost.Children.Clear();

				if (badgeView is null)
				{
					return;
				}

				badgeView.BindingContext = this.BindingContext;
				BadgeHost.Children.Add(badgeView);
			}

			protected override void OnBindingContextChanged()
			{
				base.OnBindingContextChanged();

				if (this.customBadgeView is not null)
				{
					this.customBadgeView.BindingContext = this.BindingContext;
				}
			}

			private void OnItemPropertyChanged(object? Sender, PropertyChangedEventArgs E)
			{
				if (string.Equals(E.PropertyName, nameof(TabDefinition.IsEnabled), StringComparison.Ordinal) && this.Item is TabDefinition Definition)
				{
					this.IsEnabled = Definition.IsEnabled;
				}
			}

			private void OnTapped(object? Sender, TappedEventArgs E)
			{
				_ = this.host.HandleItemTappedAsync(this);
			}

			public void Dispose()
			{
				if (this.disposed)
				{
					return;
				}

				this.disposed = true;
				this.tapRecognizer.Tapped -= this.OnTapped;
				this.GestureRecognizers.Remove(this.tapRecognizer);

				if (this.notifier is not null)
				{
					this.notifier.PropertyChanged -= this.OnItemPropertyChanged;
				}
			}
		}
	}
}
