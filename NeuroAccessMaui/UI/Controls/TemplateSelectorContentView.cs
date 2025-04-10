using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A custom ContentView control that selects and applies a DataTemplate based on the provided item using a DataTemplateSelector.
	/// This control listens to changes in the bound item and updates its displayed content accordingly.
	/// </summary>
	public class TemplateSelectorContentView : ContentView
	{
		/// <summary>
		/// Identifies the <see cref="Item"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ItemProperty =
			 BindableProperty.Create(
				  nameof(Item),
				  typeof(object),
				  typeof(TemplateSelectorContentView),
				  defaultValue: null,
				  propertyChanged: OnItemChanged);

		/// <summary>
		/// Identifies the <see cref="TemplateSelector"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TemplateSelectorProperty =
			 BindableProperty.Create(
				  nameof(TemplateSelector),
				  typeof(DataTemplateSelector),
				  typeof(TemplateSelectorContentView),
				  defaultValue: null,
				  propertyChanged: OnItemChanged);

		/// <summary>
		/// Gets or sets the data item used to determine which template to apply.
		/// </summary>
		/// <remarks>
		/// When the item changes, the control reevaluates the template selection.
		/// </remarks>
		public object Item
		{
			get => (object)this.GetValue(ItemProperty);
			set => this.SetValue(ItemProperty, value);
		}

		/// <summary>
		/// Gets or sets the <see cref="DataTemplateSelector"/> that will be used to select the appropriate <see cref="DataTemplate"/> based on the current <see cref="Item"/>.
		/// </summary>
		/// <remarks>
		/// When the template selector changes, the control reevaluates the template selection.
		/// </remarks>
		public DataTemplateSelector TemplateSelector
		{
			get => (DataTemplateSelector)this.GetValue(TemplateSelectorProperty);
			set => this.SetValue(TemplateSelectorProperty, value);
		}

		/// <summary>
		/// Invoked when either the <see cref="Item"/> or the <see cref="TemplateSelector"/> properties change.
		/// Re-applies the selected template to the view's content.
		/// </summary>
		/// <param name="bindable">The bindable object which triggered the property change.</param>
		/// <param name="oldValue">The previous value of the changed property.</param>
		/// <param name="newValue">The new value of the changed property.</param>
		private static void OnItemChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is TemplateSelectorContentView View)
			{
				View.ApplyTemplate();
			}
		}

		/// <summary>
		/// Selects and applies the appropriate <see cref="DataTemplate"/> based on the current <see cref="Item"/> and <see cref="TemplateSelector"/>.
		/// If no template can be selected, clears the current content.
		/// </summary>
		private void ApplyTemplate()
		{
			if (this.TemplateSelector is null || this.Item is null)
			{
				this.Content = null;
				return;
			}

			DataTemplate? Template = this.TemplateSelector.SelectTemplate(this.Item, this);
			if (Template is not null)
			{
				this.Content = (View)Template.CreateContent();
			}
			else
			{
				this.Content = null;
			}
		}
	}

}
