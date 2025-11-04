using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Resilience;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.Animations;

namespace NeuroAccessMaui.UI.Controls
{
	[ContentProperty(nameof(Views))]
	public partial class ViewSwitcher : Grid, IDisposable
	{
		public static readonly BindableProperty SelectedIndexProperty =
			BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ViewSwitcher), -1, BindingMode.TwoWay, propertyChanged: OnSelectedIndexPropertyChanged);

		public static readonly BindableProperty SelectedItemProperty =
			BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ViewSwitcher), default(object), BindingMode.TwoWay, propertyChanged: OnSelectedItemPropertyChanged);

		public static readonly BindableProperty SelectedStateKeyProperty =
			BindableProperty.Create(nameof(SelectedStateKey), typeof(string), typeof(ViewSwitcher), default(string), BindingMode.TwoWay, propertyChanged: OnSelectedStateKeyPropertyChanged);

		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ViewSwitcher), default(IEnumerable), propertyChanged: OnItemsSourcePropertyChanged);

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(ViewSwitcher), default(DataTemplate), propertyChanged: OnItemTemplatePropertyChanged);

		public static readonly BindableProperty ItemTemplateSelectorProperty =
			BindableProperty.Create(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(ViewSwitcher), default(DataTemplateSelector), propertyChanged: OnItemTemplateSelectorPropertyChanged);

		public static readonly BindableProperty CacheViewsProperty =
			BindableProperty.Create(nameof(CacheViews), typeof(bool), typeof(ViewSwitcher), true, propertyChanged: OnCacheViewsPropertyChanged);

		public static readonly BindableProperty AnimateProperty =
			BindableProperty.Create(nameof(Animate), typeof(bool), typeof(ViewSwitcher), true, propertyChanged: OnAnimatePropertyChanged);

		public static readonly BindableProperty TransitionDurationProperty =
			BindableProperty.Create(nameof(TransitionDuration), typeof(uint), typeof(ViewSwitcher), (uint)250, propertyChanged: OnTransitionDurationPropertyChanged);

		public static readonly BindableProperty TransitionEasingProperty =
			BindableProperty.Create(nameof(TransitionEasing), typeof(Easing), typeof(ViewSwitcher), Easing.Linear, propertyChanged: OnTransitionEasingPropertyChanged);

		public static readonly BindableProperty TransitionProperty =
			BindableProperty.Create(nameof(Transition), typeof(IViewTransition), typeof(ViewSwitcher), new CrossFadeViewTransition(), propertyChanged: OnTransitionPropertyChanged);

		public static readonly BindableProperty SelectedIndexBehaviorProperty =
			BindableProperty.Create(nameof(SelectedIndexBehavior), typeof(ViewSwitcherSelectedIndexBehavior), typeof(ViewSwitcher), ViewSwitcherSelectedIndexBehavior.Clamp, propertyChanged: OnSelectedIndexBehaviorPropertyChanged);

		public static readonly BindableProperty AutomationDescriptionTemplateProperty =
			BindableProperty.Create(nameof(AutomationDescriptionTemplate), typeof(string), typeof(ViewSwitcher), default(string));

		private readonly ObservableCollection<View> views;
		private readonly ObservableCollection<ViewSwitcherStateView> stateViews;
		private readonly List<ViewSwitcherItemDescriptor> descriptors;
		private readonly Dictionary<string, ViewSwitcherItemDescriptor> stateKeyLookup;
		private readonly ViewSwitcherSelectionState selectionState;
		private readonly ViewSwitcherViewFactory viewFactory;
		private readonly ViewSwitcherViewCache viewCache;
		private readonly Dictionary<View, ViewSwitcherItemDescriptor> viewDescriptorMap;
		private readonly HashSet<object> initializedLifecycleTargets;
		private readonly ViewSwitcherTransitionCoordinator transitionCoordinator;
		private readonly Grid presenter;
		private readonly Command nextCommand;
		private readonly Command previousCommand;

		private INotifyCollectionChanged? itemsSourceNotifier;
		private readonly ObservableTask<int> selectionOperation;
		private bool disposed;
		private bool isInitialLoad;

		public ViewSwitcher()
		{
			RowDefinition rootRow = new RowDefinition { Height = GridLength.Star };
			this.RowDefinitions.Add(rootRow);

			this.views = new ObservableCollection<View>();
			this.views.CollectionChanged += this.OnViewsCollectionChanged;

			this.stateViews = new ObservableCollection<ViewSwitcherStateView>();
			this.stateViews.CollectionChanged += this.OnStateViewsCollectionChanged;

			this.descriptors = new List<ViewSwitcherItemDescriptor>();
			this.stateKeyLookup = new Dictionary<string, ViewSwitcherItemDescriptor>(StringComparer.Ordinal);

			this.selectionState = new ViewSwitcherSelectionState
			{
				IndexBehavior = this.SelectedIndexBehavior
			};

			this.viewFactory = new ViewSwitcherViewFactory(this)
			{
				ItemTemplate = this.ItemTemplate,
				ItemTemplateSelector = this.ItemTemplateSelector
			};

			this.viewCache = new ViewSwitcherViewCache
			{
				IsEnabled = this.CacheViews
			};

			this.viewDescriptorMap = new Dictionary<View, ViewSwitcherItemDescriptor>(ReferenceEqualityComparer.Instance);
			this.initializedLifecycleTargets = new HashSet<object>(ReferenceEqualityComparer.Instance);

			this.selectionOperation = new ObservableTask<int>();

			this.presenter = new Grid();
			IAnimationCoordinator? AnimationCoordinator = null;
			try
			{
				AnimationCoordinator = ServiceHelper.GetService<IAnimationCoordinator>();
			}
			catch (ArgumentException)
			{
				AnimationCoordinator = null;
			}
			this.transitionCoordinator = new ViewSwitcherTransitionCoordinator(this.presenter, AnimationCoordinator)
			{
				Animate = this.Animate,
				Duration = this.TransitionDuration,
				Easing = this.TransitionEasing,
				Transition = this.Transition
			};
			this.Children.Add(this.presenter);

			this.nextCommand = new Command(this.ExecuteNextCommand, this.CanExecuteNextCommand);
			this.previousCommand = new Command(this.ExecutePreviousCommand, this.CanExecutePreviousCommand);

			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;

			this.isInitialLoad = true;
		}

		public event EventHandler<ViewSwitcherSelectionChangingEventArgs>? SelectionChanging;

		public event EventHandler<ViewSwitcherSelectionChangedEventArgs>? SelectionChanged;

		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, value);
		}

		public object? SelectedItem
		{
			get => this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}

		public string? SelectedStateKey
		{
			get => (string?)this.GetValue(SelectedStateKeyProperty);
			set => this.SetValue(SelectedStateKeyProperty, value);
		}

		public IEnumerable? ItemsSource
		{
			get => (IEnumerable?)this.GetValue(ItemsSourceProperty);
			set => this.SetValue(ItemsSourceProperty, value);
		}

		public DataTemplate? ItemTemplate
		{
			get => (DataTemplate?)this.GetValue(ItemTemplateProperty);
			set => this.SetValue(ItemTemplateProperty, value);
		}

		public DataTemplateSelector? ItemTemplateSelector
		{
			get => (DataTemplateSelector?)this.GetValue(ItemTemplateSelectorProperty);
			set => this.SetValue(ItemTemplateSelectorProperty, value);
		}

		public bool CacheViews
		{
			get => (bool)this.GetValue(CacheViewsProperty);
			set => this.SetValue(CacheViewsProperty, value);
		}

		public bool Animate
		{
			get => (bool)this.GetValue(AnimateProperty);
			set => this.SetValue(AnimateProperty, value);
		}

		public uint TransitionDuration
		{
			get => (uint)this.GetValue(TransitionDurationProperty);
			set => this.SetValue(TransitionDurationProperty, value);
		}

		public Easing? TransitionEasing
		{
			get => (Easing?)this.GetValue(TransitionEasingProperty);
			set => this.SetValue(TransitionEasingProperty, value);
		}

		public IViewTransition Transition
		{
			get => (IViewTransition)this.GetValue(TransitionProperty);
			set => this.SetValue(TransitionProperty, value);
		}

		public ViewSwitcherSelectedIndexBehavior SelectedIndexBehavior
		{
			get => (ViewSwitcherSelectedIndexBehavior)this.GetValue(SelectedIndexBehaviorProperty);
			set => this.SetValue(SelectedIndexBehaviorProperty, value);
		}

		public string? AutomationDescriptionTemplate
		{
			get => (string?)this.GetValue(AutomationDescriptionTemplateProperty);
			set => this.SetValue(AutomationDescriptionTemplateProperty, value);
		}

		public ObservableCollection<View> Views => this.views;

		public ObservableCollection<ViewSwitcherStateView> StateViews => this.stateViews;

		public ICommand NextCommand => this.nextCommand;

		public ICommand PreviousCommand => this.previousCommand;

		public ObservableTask<int> SelectionOperation => this.selectionOperation;

		public Task SwitchToAsync(int index, bool animate = true, CancellationToken cancellationToken = default)
		{
			Task task = this.RunSelectionTaskAsync(
				token => this.ApplySelectionAsync(index, null, null, ViewSwitcherSelectionChangeSource.Index, animate, token),
				cancellationToken);
			this.ObserveSelectionTask(task);
			return task;
		}

		public Task SwitchToStateAsync(string stateKey, bool animate = true, CancellationToken cancellationToken = default)
		{
			Task task = this.RunSelectionTaskAsync(
				token => this.ApplySelectionAsync(null, null, stateKey, ViewSwitcherSelectionChangeSource.StateKey, animate, token),
				cancellationToken);
			this.ObserveSelectionTask(task);
			return task;
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			object? bindingContext = this.BindingContext;
			foreach (View view in this.views)
			{
				if (!ReferenceEquals(view.BindingContext, bindingContext))
				{
					view.BindingContext = bindingContext;
				}
			}

			foreach (ViewSwitcherStateView stateView in this.stateViews)
			{
				if (stateView.Content is not null && stateView.Content.BindingContext is null)
				{
					stateView.Content.BindingContext = bindingContext;
				}
			}
		}

		private static void OnSelectedIndexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				int requestedIndex = (int)newValue;
				viewSwitcher.RequestSelection(ViewSwitcherSelectionChangeSource.Index, requestedIndex, null, null);
			}
		}

		private static void OnSelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.RequestSelection(ViewSwitcherSelectionChangeSource.Item, null, newValue, null);
			}
		}

		private static void OnSelectedStateKeyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				string? requestedKey = newValue as string;
				viewSwitcher.RequestSelection(ViewSwitcherSelectionChangeSource.StateKey, null, null, requestedKey);
			}
		}

		private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable);
			}
		}

		private static void OnItemTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.viewFactory.ItemTemplate = newValue as DataTemplate;
				viewSwitcher.RefreshDescriptors();
			}
		}

		private static void OnItemTemplateSelectorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.viewFactory.ItemTemplateSelector = newValue as DataTemplateSelector;
				viewSwitcher.RefreshDescriptors();
			}
		}

		private static void OnCacheViewsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				bool isEnabled = (bool)newValue;
				viewSwitcher.viewCache.IsEnabled = isEnabled;
				if (!isEnabled)
				{
					viewSwitcher.viewCache.Clear();
					viewSwitcher.viewDescriptorMap.Clear();
				}
			}
		}

		private static void OnAnimatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.transitionCoordinator.Animate = (bool)newValue;
			}
		}

		private static void OnTransitionDurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.transitionCoordinator.Duration = (uint)newValue;
			}
		}

		private static void OnTransitionEasingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher)
			{
				viewSwitcher.transitionCoordinator.Easing = newValue as Easing;
			}
		}

		private static void OnTransitionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher && newValue is IViewTransition transition)
			{
				viewSwitcher.transitionCoordinator.Transition = transition;
			}
		}

		private static void OnSelectedIndexBehaviorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher viewSwitcher && newValue is ViewSwitcherSelectedIndexBehavior behavior)
			{
				viewSwitcher.selectionState.IndexBehavior = behavior;
				viewSwitcher.UpdateNavigationCommands();
			}
		}

		private void RequestSelection(ViewSwitcherSelectionChangeSource source, int? index, object? item, string? stateKey)
		{
			if (this.selectionState.ShouldIgnore(source))
				return;

			Task task = this.RunSelectionTaskAsync(
				token => this.ApplySelectionAsync(index, item, stateKey, source, null, token),
				CancellationToken.None);
			this.ObserveSelectionTask(task);
		}

		private readonly struct SelectionWorkItem
		{
			public SelectionWorkItem(Func<CancellationToken, Task> factory, CancellationToken externalToken)
			{
				this.Factory = factory ?? throw new ArgumentNullException(nameof(factory));
				this.ExternalToken = externalToken;
			}

			public Func<CancellationToken, Task> Factory { get; }

			public CancellationToken ExternalToken { get; }
		}

		private Task RunSelectionTaskAsync(Func<CancellationToken, Task> taskFactory, CancellationToken externalToken)
		{
			SelectionWorkItem workItem = new SelectionWorkItem(taskFactory, externalToken);
			this.selectionOperation.Run<SelectionWorkItem>(async (item, context) =>
			{
				CancellationToken effectiveToken = context.CancellationToken;
				if (item.ExternalToken.CanBeCanceled)
				{
					using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(effectiveToken, item.ExternalToken);
					effectiveToken = linked.Token;
					await item.Factory.Invoke(effectiveToken).ConfigureAwait(false);
				}
				else
				{
					await item.Factory.Invoke(effectiveToken).ConfigureAwait(false);
				}
			}, workItem);

			Task? task = this.selectionOperation.CurrentTask;
			return task ?? Task.CompletedTask;
		}

		private void ObserveSelectionTask(Task task)
		{
			task.ContinueWith(t =>
			{
				if (t.Exception is not null)
				{
					this.LogException(t.Exception);
				}
			}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
		}

		private async Task ApplySelectionAsync(
			int? requestedIndex,
			object? requestedItem,
			string? requestedStateKey,
			ViewSwitcherSelectionChangeSource source,
			bool? animateOverride,
			CancellationToken cancellationToken)
		{
			await this.Dispatcher.DispatchAsync(async () =>
			{
				// Instead of throwing (causing unhandled exception surfaced to user), abort gracefully if canceled.
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				this.RefreshDescriptors();

				int itemCount = this.descriptors.Count;
				int targetIndex = this.ResolveTargetIndex(requestedIndex, requestedItem, requestedStateKey, source, itemCount);
				int oldIndex = this.selectionState.SelectedIndex;
				object? oldItem = this.selectionState.SelectedItem;
				string? oldStateKey = this.selectionState.SelectedStateKey;

				if (targetIndex >= itemCount)
				{
					targetIndex = this.selectionState.NormalizeIndex(targetIndex, itemCount);
				}

				ViewSwitcherItemDescriptor? descriptor = null;
				object? newItem = null;
				string? newStateKey = null;

				if (targetIndex >= 0 && targetIndex < itemCount)
				{
					descriptor = this.descriptors[targetIndex];
					newItem = descriptor.Item;
					newStateKey = descriptor.StateKey;
				}

				ViewSwitcherSelectionChangingEventArgs changingArgs = new ViewSwitcherSelectionChangingEventArgs(
					oldIndex,
					oldItem,
					oldStateKey,
					targetIndex,
					newItem,
					newStateKey);

				this.SelectionChanging?.Invoke(this, changingArgs);
				if (changingArgs.Cancel)
				{
					this.RevertSelectionProperties();
					return;
				}

				this.selectionState.UpdateSnapshot(targetIndex, newItem, newStateKey);

				using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.Index))
				{
					this.SelectedIndex = targetIndex;
				}

				using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.Item))
				{
					this.SelectedItem = newItem;
				}

				using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.StateKey))
				{
					this.SelectedStateKey = newStateKey;
				}

				this.UpdateAutomationSemantics(newItem, newStateKey);
				this.UpdateNavigationCommands();

				View? oldViewInstance = this.transitionCoordinator.CurrentView;
				View? viewToPresent = descriptor is not null ? this.ResolveView(descriptor, targetIndex) : null;

				if (!ReferenceEquals(oldViewInstance, viewToPresent))
				{
					await this.InvokeLifecycleDisappearingAsync(oldViewInstance, cancellationToken).ConfigureAwait(false);
				}

				if (viewToPresent is not null)
				{
					await this.EnsureLifecycleInitializedAsync(viewToPresent, cancellationToken).ConfigureAwait(false);
				}

				bool previousAnimate = this.transitionCoordinator.Animate;
				uint previousDuration = this.transitionCoordinator.Duration;
				Easing? previousEasing = this.transitionCoordinator.Easing;

				if (animateOverride.HasValue)
				{
					this.transitionCoordinator.Animate = animateOverride.Value;
				}

				this.transitionCoordinator.Duration = this.TransitionDuration;
				this.transitionCoordinator.Easing = this.TransitionEasing;

				try
				{
					await PolicyRunner.RunAsync(
						ct => this.transitionCoordinator.SwitchAsync(this, viewToPresent, this.isInitialLoad, ct),
						cancellationToken).ConfigureAwait(false);
					if (!ReferenceEquals(oldViewInstance, viewToPresent))
					{
						await this.InvokeLifecycleAppearingAsync(viewToPresent, cancellationToken).ConfigureAwait(false);
					}
					await this.DisposeLifecycleIfRequiredAsync(oldViewInstance, viewToPresent).ConfigureAwait(false);
				}
				finally
				{
					this.transitionCoordinator.Animate = previousAnimate;
					this.transitionCoordinator.Duration = previousDuration;
					this.transitionCoordinator.Easing = previousEasing;
				}

				this.isInitialLoad = false;
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				ViewSwitcherSelectionChangedEventArgs changedArgs = new ViewSwitcherSelectionChangedEventArgs(
					oldIndex,
					oldItem,
					oldStateKey,
					targetIndex,
					newItem,
					newStateKey);
				this.SelectionChanged?.Invoke(this, changedArgs);
			}).ConfigureAwait(false);
		}

		private void RevertSelectionProperties()
		{
			using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.Index))
			{
				this.SelectedIndex = this.selectionState.SelectedIndex;
			}

			using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.Item))
			{
				this.SelectedItem = this.selectionState.SelectedItem;
			}

			using (this.selectionState.BeginUpdate(ViewSwitcherSelectionChangeSource.StateKey))
			{
				this.SelectedStateKey = this.selectionState.SelectedStateKey;
			}
		}

		private int ResolveTargetIndex(
			int? requestedIndex,
			object? requestedItem,
			string? requestedStateKey,
			ViewSwitcherSelectionChangeSource source,
			int itemCount)
		{
			if (!string.IsNullOrWhiteSpace(requestedStateKey))
			{
				if (this.stateKeyLookup.TryGetValue(requestedStateKey, out ViewSwitcherItemDescriptor descriptor))
				{
					return descriptor.Index;
				}
				return this.selectionState.SelectedIndex;
			}

			if (requestedItem is not null)
			{
				for (int i = 0; i < this.descriptors.Count; i++)
				{
					ViewSwitcherItemDescriptor descriptor = this.descriptors[i];
					if (ReferenceEquals(descriptor.Item, requestedItem))
						return descriptor.Index;
					if (descriptor.StateView is not null && ReferenceEquals(descriptor.StateView, requestedItem))
						return descriptor.Index;
				}

				return this.selectionState.SelectedIndex;
			}

			if (requestedIndex.HasValue)
			{
				return this.selectionState.NormalizeIndex(requestedIndex.Value, itemCount);
			}

			return this.selectionState.SelectedIndex;
		}

		private View? ResolveView(ViewSwitcherItemDescriptor descriptor, int index)
		{
			if (!string.IsNullOrWhiteSpace(descriptor.StateKey))
			{
				if (this.viewCache.TryGetByStateKey(descriptor.StateKey!, out View? cachedByKey))
				{
					this.ApplyBindingContext(cachedByKey, descriptor);
					this.viewDescriptorMap[cachedByKey] = descriptor;
					return cachedByKey;
				}
			}

			if (this.viewCache.TryGetByIndex(index, out View? cachedByIndex))
			{
				this.ApplyBindingContext(cachedByIndex, descriptor);
				this.viewDescriptorMap[cachedByIndex] = descriptor;
				return cachedByIndex;
			}

			View view = this.viewFactory.CreateView(descriptor);
			this.ApplyBindingContext(view, descriptor);
			this.viewDescriptorMap[view] = descriptor;

			if (this.viewCache.IsEnabled)
			{
				if (!string.IsNullOrWhiteSpace(descriptor.StateKey))
				{
					this.viewCache.StoreByStateKey(descriptor.StateKey!, view);
				}
				else
				{
					this.viewCache.StoreByIndex(index, view);
				}
			}

			return view;
		}

		private void ApplyBindingContext(View view, ViewSwitcherItemDescriptor descriptor)
		{
			if (descriptor.StateView is not null)
			{
				if (descriptor.StateView.ViewModelType is not null)
					return;

				if (descriptor.StateView.Content is not null && descriptor.StateView.Content.BindingContext is not null)
					return;
			}

			if (descriptor.Item is not null && descriptor.Item is not View)
			{
				view.BindingContext = descriptor.Item;
				return;
			}

			if (view.BindingContext is null)
			{
				view.BindingContext = this.BindingContext;
			}
		}

		private void RefreshDescriptors()
		{
			this.descriptors.Clear();
			this.stateKeyLookup.Clear();

			if (this.stateViews.Count > 0)
			{
				int index = 0;
				foreach (ViewSwitcherStateView stateView in this.stateViews)
				{
					ViewSwitcherItemDescriptor descriptor = new ViewSwitcherItemDescriptor
					{
						Index = index,
						StateView = stateView,
						StateKey = stateView.StateKey,
						InlineView = stateView.Content,
						Item = stateView
					};
					this.descriptors.Add(descriptor);
					if (!string.IsNullOrWhiteSpace(stateView.StateKey))
					{
						this.stateKeyLookup[stateView.StateKey] = descriptor;
					}
					index++;
				}
				return;
			}

			if (this.ItemsSource is not null)
			{
				int index = 0;
				foreach (object? item in this.ItemsSource)
				{
					ViewSwitcherItemDescriptor descriptor = new ViewSwitcherItemDescriptor
					{
						Index = index,
						Item = item,
						InlineView = item as View
					};
					this.descriptors.Add(descriptor);
					index++;
				}
				return;
			}

			if (this.views.Count > 0)
			{
				int index = 0;
				foreach (View view in this.views)
				{
					ViewSwitcherItemDescriptor descriptor = new ViewSwitcherItemDescriptor
					{
						Index = index,
						Item = view,
						InlineView = view
					};
					this.descriptors.Add(descriptor);
					index++;
				}
			}
		}

		private void OnViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.RefreshDescriptors();
			this.viewCache.Clear();
			this.viewDescriptorMap.Clear();
			this.RequestSelection(ViewSwitcherSelectionChangeSource.Index, this.SelectedIndex, null, null);
		}

		private void OnStateViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.RefreshDescriptors();
			this.viewCache.Clear();
			this.viewDescriptorMap.Clear();
			this.RequestSelection(ViewSwitcherSelectionChangeSource.StateKey, null, null, this.SelectedStateKey);
		}

		private void OnItemsSourceChanged(IEnumerable? oldSource, IEnumerable? newSource)
		{
			if (this.itemsSourceNotifier is not null)
			{
				this.itemsSourceNotifier.CollectionChanged -= this.OnItemsSourceCollectionChanged;
				this.itemsSourceNotifier = null;
			}

			if (oldSource != newSource && newSource is INotifyCollectionChanged notifier)
			{
				this.itemsSourceNotifier = notifier;
				notifier.CollectionChanged += this.OnItemsSourceCollectionChanged;
			}

			this.RefreshDescriptors();
			this.viewCache.Clear();
			this.viewDescriptorMap.Clear();
			this.RequestSelection(ViewSwitcherSelectionChangeSource.Index, this.SelectedIndex, null, null);
		}

		private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			this.RefreshDescriptors();
			this.viewCache.Clear();
			this.viewDescriptorMap.Clear();
			this.RequestSelection(ViewSwitcherSelectionChangeSource.Index, this.SelectedIndex, null, null);
		}

		private void OnLoaded(object? sender, EventArgs e)
		{
			if (this.isInitialLoad)
			{
				this.RequestSelection(ViewSwitcherSelectionChangeSource.Index, this.SelectedIndex, null, null);
			}
		}

		private void OnUnloaded(object? sender, EventArgs e)
		{
		}

		private bool CanExecuteNextCommand()
		{
			if (this.descriptors.Count == 0)
				return false;

			if (this.selectionState.IndexBehavior == ViewSwitcherSelectedIndexBehavior.Wrap)
				return true;

			return this.selectionState.SelectedIndex < this.descriptors.Count - 1;
		}

		private bool CanExecutePreviousCommand()
		{
			if (this.descriptors.Count == 0)
				return false;

			if (this.selectionState.IndexBehavior == ViewSwitcherSelectedIndexBehavior.Wrap)
				return true;

			return this.selectionState.SelectedIndex > 0;
		}

		private void ExecuteNextCommand()
		{
			int currentIndex = this.selectionState.SelectedIndex;
			int targetIndex = currentIndex >= 0 ? currentIndex + 1 : 0;
			this.ObserveSelectionTask(this.SwitchToAsync(targetIndex));
		}

		private void ExecutePreviousCommand()
		{
			int currentIndex = this.selectionState.SelectedIndex;
			int targetIndex = currentIndex > 0 ? currentIndex - 1 : -1;
			this.ObserveSelectionTask(this.SwitchToAsync(targetIndex));
		}

		private void UpdateNavigationCommands()
		{
			this.nextCommand.ChangeCanExecute();
			this.previousCommand.ChangeCanExecute();
		}

		private void UpdateAutomationSemantics(object? item, string? stateKey)
		{
			string? template = this.AutomationDescriptionTemplate;
			if (string.IsNullOrWhiteSpace(template))
			{
				SemanticProperties.SetDescription(this, null);
				return;
			}

			string placeholder = stateKey ?? item?.ToString() ?? string.Empty;
			string description;
			if (template.Contains("{0}", StringComparison.Ordinal))
			{
				description = template.Replace("{0}", placeholder, StringComparison.Ordinal);
			}
			else
			{
				description = string.Format(CultureInfo.CurrentCulture, template, placeholder);
			}

			SemanticProperties.SetDescription(this, description);
		}

		private async Task EnsureLifecycleInitializedAsync(View? view, CancellationToken token)
		{
			if (view is null)
				return;

			await this.EnsureLifecycleInitializedCoreAsync(view, token).ConfigureAwait(false);
			object? bindingContext = view.BindingContext;
			if (bindingContext is not null && !ReferenceEquals(bindingContext, view))
			{
				await this.EnsureLifecycleInitializedCoreAsync(bindingContext, token).ConfigureAwait(false);
			}
		}

		private async Task EnsureLifecycleInitializedCoreAsync(object target, CancellationToken token)
		{
			if (target is not ILifeCycleView lifecycleView)
				return;

			if (this.initializedLifecycleTargets.Contains(target))
				return;

			await PolicyRunner.RunAsync(_ => lifecycleView.OnInitializeAsync(), token).ConfigureAwait(false);
			this.initializedLifecycleTargets.Add(target);
		}

		private async Task InvokeLifecycleAppearingAsync(View? view, CancellationToken token)
		{
			if (view is null)
				return;

			await this.InvokeLifecycleAppearingCoreAsync(view, token).ConfigureAwait(false);
			object? bindingContext = view.BindingContext;
			if (bindingContext is not null && !ReferenceEquals(bindingContext, view))
			{
				await this.InvokeLifecycleAppearingCoreAsync(bindingContext, token).ConfigureAwait(false);
			}
		}

		private Task InvokeLifecycleAppearingCoreAsync(object target, CancellationToken token)
		{
			if (target is not ILifeCycleView lifecycleView)
				return Task.CompletedTask;

			return PolicyRunner.RunAsync(_ => lifecycleView.OnAppearingAsync(), token);
		}

		private async Task InvokeLifecycleDisappearingAsync(View? view, CancellationToken token)
		{
			if (view is null)
				return;

			await this.InvokeLifecycleDisappearingCoreAsync(view, token).ConfigureAwait(false);
			object? bindingContext = view.BindingContext;
			if (bindingContext is not null && !ReferenceEquals(bindingContext, view))
			{
				await this.InvokeLifecycleDisappearingCoreAsync(bindingContext, token).ConfigureAwait(false);
			}
		}

		private Task InvokeLifecycleDisappearingCoreAsync(object target, CancellationToken token)
		{
			if (target is not ILifeCycleView lifecycleView)
			 return Task.CompletedTask;

			return PolicyRunner.RunAsync(_ => lifecycleView.OnDisappearingAsync(), token);
		}

		private async Task InvokeLifecycleDisposeAsync(View? view)
		{
			if (view is null)
				return;

			await this.InvokeLifecycleDisposeCoreAsync(view).ConfigureAwait(false);
			object? bindingContext = view.BindingContext;
			if (bindingContext is not null && !ReferenceEquals(bindingContext, view))
			{
				await this.InvokeLifecycleDisposeCoreAsync(bindingContext).ConfigureAwait(false);
			}
		}

		private async Task InvokeLifecycleDisposeCoreAsync(object target)
		{
			if (target is not ILifeCycleView lifecycleView)
				return;

			await PolicyRunner.RunAsync(_ => lifecycleView.OnDisposeAsync(), CancellationToken.None).ConfigureAwait(false);
			this.initializedLifecycleTargets.Remove(target);
		}

		private async Task DisposeLifecycleIfRequiredAsync(View? oldView, View? newView)
		{
			if (oldView is null)
				return;

			if (ReferenceEquals(oldView, newView))
				return;

			if (!this.ShouldDisposeView(oldView))
				return;

			await this.InvokeLifecycleDisposeAsync(oldView).ConfigureAwait(false);
			this.viewDescriptorMap.Remove(oldView);
		}

		private bool ShouldDisposeView(View view)
		{
			if (this.viewCache.IsEnabled)
				return false;

			if (this.viewDescriptorMap.TryGetValue(view, out ViewSwitcherItemDescriptor descriptor))
			{
				if (descriptor.InlineView is not null && ReferenceEquals(descriptor.InlineView, view) && this.views.Contains(view))
					return false;

				if (descriptor.StateView is not null && ReferenceEquals(descriptor.StateView.Content, view))
					return false;
			}

			return true;
		}

		private void LogException(Exception exception)
		{
			try
			{
				ServiceRef.LogService.LogException(exception);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				this.Loaded -= this.OnLoaded;
				this.Unloaded -= this.OnUnloaded;

				this.views.CollectionChanged -= this.OnViewsCollectionChanged;
				this.stateViews.CollectionChanged -= this.OnStateViewsCollectionChanged;

				if (this.itemsSourceNotifier is not null)
				{
					this.itemsSourceNotifier.CollectionChanged -= this.OnItemsSourceCollectionChanged;
					this.itemsSourceNotifier = null;
				}

				this.transitionCoordinator.Dispose();
				this.selectionOperation.Dispose();
				this.viewDescriptorMap.Clear();
				this.initializedLifecycleTargets.Clear();
			}

			this.disposed = true;
		}
	}

	public class ViewSwitcherItem : ContentView
	{
	}
}
