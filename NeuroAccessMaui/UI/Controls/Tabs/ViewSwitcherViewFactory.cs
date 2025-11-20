using System;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	internal sealed class ViewSwitcherViewFactory
	{
		private readonly ViewSwitcher host;

		public ViewSwitcherViewFactory(ViewSwitcher host)
		{
			this.host = host;
		}

		public DataTemplate? ItemTemplate { get; set; }

		public DataTemplateSelector? ItemTemplateSelector { get; set; }

		public Func<object?, View>? ItemViewFactory { get; set; }

		public View CreateView(ViewSwitcherItemDescriptor descriptor)
		{
			if (descriptor.StateView is not null)
			{
				return this.CreateFromStateView(descriptor.StateView);
			}

			if (descriptor.InlineView is not null)
			{
				return descriptor.InlineView;
			}

			if (descriptor.Item is View existingView)
			{
				return existingView;
			}

			if (descriptor.Item is ViewSwitcherItem wrapper)
			{
				return this.ResolveInlineWrapper(wrapper);
			}

			if (this.ItemViewFactory is not null)
			{
				View customView = this.ItemViewFactory.Invoke(descriptor.Item);
				if (customView is null)
					throw new InvalidOperationException("Custom item view factory returned null.");

				customView.BindingContext = descriptor.Item;
				return customView;
			}

			DataTemplate? template = this.ResolveTemplate(descriptor.Item, descriptor.Index);
			if (template is not null)
			{
				object? created = template.CreateContent();
				View? templatedView = created as View;
				if (templatedView is not null)
				{
					templatedView.BindingContext = descriptor.Item;
					return templatedView;
				}
				if (created is ViewCell cell && cell.View is not null)
				{
					View cellView = cell.View;
					cellView.BindingContext = descriptor.Item;
					return cellView;
				}
			}

			Label fallbackLabel = new Label
			{
				Text = descriptor.Item?.ToString() ?? string.Empty
			};
			fallbackLabel.BindingContext = descriptor.Item;
			return fallbackLabel;
		}

		private View CreateFromStateView(ViewSwitcherStateView stateView)
		{
			View? content = stateView.Content;
			if (content is not null)
			{
				return content;
			}

			if (stateView.ViewType is not null)
			{
				object? created = Activator.CreateInstance(stateView.ViewType);
				if (created is View viewInstance)
				{
					this.AttachViewModel(stateView, viewInstance);
					return viewInstance;
				}
			}

			ContentView placeholder = new ContentView();
			this.AttachViewModel(stateView, placeholder);
			return placeholder;
		}

		private void AttachViewModel(ViewSwitcherStateView stateView, View viewInstance)
		{
			if (viewInstance.BindingContext is not null)
				return;

			if (stateView.ViewModelType is null)
				return;

			object? viewModel = Activator.CreateInstance(stateView.ViewModelType);
			if (viewModel is not null)
			{
				viewInstance.BindingContext = viewModel;
			}
		}

		private View ResolveInlineWrapper(ViewSwitcherItem wrapper)
		{
			if (wrapper.Content is not null)
			{
				return wrapper.Content;
			}

			return wrapper;
		}

		private DataTemplate? ResolveTemplate(object? item, int index)
		{
			if (this.ItemTemplateSelector is not null)
			{
				return this.ItemTemplateSelector.SelectTemplate(item, this.host);
			}

			return this.ItemTemplate;
		}
	}
}
