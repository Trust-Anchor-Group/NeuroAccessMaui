using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// WORK IN PROGRESS
	/// </summary>

	[ContentProperty(nameof(Views))]
	public class ViewSwitcher : Grid, IDisposable
	{
		public static readonly BindableProperty SelectedIndexProperty =
			 BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ViewSwitcher), 0, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);

		public static readonly BindableProperty ItemsSourceProperty =
			 BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ViewSwitcher), default(IEnumerable));

		public static readonly BindableProperty ContentItemTemplateProperty =
			 BindableProperty.Create(nameof(ContentItemTemplate), typeof(DataTemplate), typeof(ViewSwitcher), default(DataTemplate));

		public static readonly BindableProperty AnimateProperty =
			 BindableProperty.Create(nameof(Animate), typeof(bool), typeof(ViewSwitcher), true);

		public static readonly BindableProperty TransitionDurationProperty =
			 BindableProperty.Create(nameof(TransitionDuration), typeof(uint), typeof(ViewSwitcher), (uint)250);

		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, value);
		}

		public IEnumerable ItemsSource
		{
			get => (IEnumerable)this.GetValue(ItemsSourceProperty);
			set => this.SetValue(ItemsSourceProperty, value);
		}

		public DataTemplate ContentItemTemplate
		{
			get => (DataTemplate)this.GetValue(ContentItemTemplateProperty);
			set => this.SetValue(ContentItemTemplateProperty, value);
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

		// Inline mode: use this collection when ItemsSource is null.
		public IList<View> Views { get; } = new List<View>();

		// Container that displays the selected view.
		private readonly ContentView contentContainer;

		// Flag to prevent overlapping transitions.
		private bool isTransitioning;

		// Flag to indicate that an update was requested during a transition.
		private bool pendingUpdate;

		// Disposal flag.
		private bool disposed;

		public ViewSwitcher()
		{
			this.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			this.contentContainer = new ContentView
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};
			this.Children.Add(this.contentContainer);
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			this.contentContainer.BindingContext = this.BindingContext;
			foreach (View View in this.Views)
			{
				View.BindingContext = this.BindingContext;
			}
		}

		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			if (propertyName == ContentItemTemplateProperty.PropertyName ||
				 propertyName == ItemsSourceProperty.PropertyName ||
				 propertyName == nameof(this.Views) ||
				 propertyName == SelectedIndexProperty.PropertyName)
			{
				this.UpdateContent();
			}
		}

		private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is ViewSwitcher ViewSwitcher)
			{
				ViewSwitcher.UpdateContent();
			}
		}

		// Extracts the effective view from an inline item.
		private View GetEffectiveView(object item)
		{
			if (item is ViewSwitcherItem ViewItem)
			{
				return ViewItem.Content ?? ViewItem;
			}
			if (item is View View)
			{
				return View;
			}
			return new Label { Text = item?.ToString() };
		}

		// Updates the displayed content based on SelectedIndex.
		private async void UpdateContent()
		{
			IEnumerable Source = this.ItemsSource ?? this.Views;
			if (Source is null)
				return;

			if (this.isTransitioning)
			{
				this.pendingUpdate = true;
				return;
			}

			do
			{
				this.pendingUpdate = false;
				this.isTransitioning = true;
				View OldContent = this.contentContainer.Content;

				try
				{
					object? SelectedItem = null;
					int Count = 0;
					foreach (object? Item in Source)
					{
						if (Count == this.SelectedIndex)
						{
							SelectedItem = Item;
							break;
						}
						Count++;
					}
					if (SelectedItem is null)
						break;

					// For inline mode, check if the effective view is the same instance.
					if (this.ItemsSource is null)
					{
						View EffectiveView = this.GetEffectiveView(SelectedItem);
						if (ReferenceEquals(this.contentContainer.Content, EffectiveView))
						{
							this.isTransitioning = false;
							break;
						}
					}
					else if (this.contentContainer.Content is not null &&
								this.contentContainer.Content.BindingContext is not null &&
								Equals(this.contentContainer.Content.BindingContext, SelectedItem))
					{
						this.isTransitioning = false;
						break;
					}

					View NewContent;
					if (this.ContentItemTemplate is not null)
					{
						if (this.ContentItemTemplate.CreateContent() is not View TemplatedView)
							break;
						NewContent = TemplatedView;
						NewContent.BindingContext = SelectedItem;
					}
					else
					{
						NewContent = this.GetEffectiveView(SelectedItem);
					}

					if (this.Animate && OldContent is VisualElement OldVisual)
					{
						await OldVisual.FadeTo(0, this.TransitionDuration / 2);
					}

					this.contentContainer.Content = NewContent;

					if (this.Animate && NewContent is VisualElement NewVisual)
					{
						NewVisual.Opacity = 0;
						await NewVisual.FadeTo(1, this.TransitionDuration / 2);
					}
				}
				catch (Exception Ex)
				{
					Debug.WriteLine($"ViewSwitcher.UpdateContent Exception: {Ex}");
				}
				finally
				{
					if (OldContent is not null &&
						 OldContent != this.contentContainer.Content &&
						 OldContent is IDisposable Disposable)
					{
						try
						{
							Disposable.Dispose();
						}
						catch (Exception Ex)
						{
							Debug.WriteLine($"Error disposing old content: {Ex}");
						}
					}
					this.isTransitioning = false;
				}
			} while (this.pendingUpdate);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.contentContainer.Content is IDisposable DisposableContent)
					{
						try
						{
							DisposableContent.Dispose();
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"Error disposing content: {ex}");
						}
					}
					foreach (View View in this.Views)
					{
						if (View is IDisposable DisposableView)
						{
							try
							{
								DisposableView.Dispose();
							}
							catch (Exception Ex)
							{
								Debug.WriteLine($"Error disposing view: {Ex}");
							}
						}
					}
				}
				this.disposed = true;
			}
		}
	}

	public class ViewSwitcherItem : ContentView
	{
		// A thin wrapper for inline declared view items.
	}
}
