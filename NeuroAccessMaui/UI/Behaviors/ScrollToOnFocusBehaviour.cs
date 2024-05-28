using System.ComponentModel;
using System.Diagnostics;
using NeuroAccessMaui.UI.Controls;
using Waher.Script.Constants;

namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// Used for moving focus to the next UI component when a button has been clicked.
	/// </summary>
	public class ScrollToOnFocusBehavior : Behavior<VisualElement>
	{
        /// <summary>
		/// The ScrollView to scroll. If null, the first parent ScrollView will be used.
		/// </summary>
		[TypeConverter(typeof(ReferenceTypeConverter))]
		public ScrollView? TargetScrollView { get; set; }

        /// <summary>
        /// The element to scroll to. If null, the element that has focus will be scrolled to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public VisualElement? TargetElement { get; set; }

        /// <summary>
        /// The scroll behavior to use when scrolling to the element.
        /// </summary>
        public ScrollToPosition ScrollToPosition { get; set; } = ScrollToPosition.Start;

		/// <summary>
		/// A BindableProperty for <see cref="IsEnabled"/> property.
		/// </summary>
		public static readonly BindableProperty IsEnabledProperty =
			BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(ScrollToClickedBehavior), true);

		/// <summary>
		/// Gets or sets a value indicating if behavior is enabled or disabled
		/// </summary>
		public bool IsEnabled
		{
			get => (bool)this.GetValue(IsEnabledProperty);
			set => this.SetValue(IsEnabledProperty, value);
		}

        protected override void OnAttachedTo(VisualElement bindable)
        {

            base.OnAttachedTo(bindable);

            if(bindable is CompositeEntry entry)
                entry.Entry.Focused += this.OnElementFocused;
            else
                bindable.Focused += this.OnElementFocused;
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            base.OnDetachingFrom(bindable);
            if(bindable is CompositeEntry entry)
                entry.Entry.Focused -= this.OnElementFocused;
            else
                bindable.Focused -= this.OnElementFocused;
        }

        private async void OnElementFocused(object? sender, FocusEventArgs e)
        {
            if(sender is null or not VisualElement)
            {
                return;
            }
            if(sender is CompositeEntry or Entry)
                await Task.Delay(200); //allow keyboard to show if there is any

            VisualElement? target = this.TargetElement ?? (VisualElement)sender;

            if (this.TargetScrollView is not null)
            {
                await this.TargetScrollView.ScrollToAsync(target, ScrollToPosition.Start, true);
            }
            else
            {
                Element Loop = ((VisualElement)sender).Parent;

                while (Loop is not null && Loop is not ScrollView)
                    Loop = Loop.Parent;

                (Loop as ScrollView)?.ScrollToAsync(target, this.ScrollToPosition, true);
            }
        }
    }
}