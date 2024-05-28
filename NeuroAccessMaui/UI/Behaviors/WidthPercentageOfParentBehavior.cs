using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Behaviors
{
    public class WidthPercentageBehavior : Behavior<View>
    {
        /// <summary>
		/// A BindableProperty for <see cref="Percentage"/> property.
		/// </summary>
        public static readonly BindableProperty PercentageProperty = BindableProperty.Create(
            nameof(Percentage),
            typeof(double),
            typeof(WidthPercentageBehavior),
            0.0,
            propertyChanged: OnPercentagePropertyChanged);

		/// <summary>
		/// A BindableProperty for <see cref="AdditionalPadding"/> property.
		/// </summary>
        public static readonly BindableProperty AdditionalPaddingProperty = BindableProperty.Create(
            nameof(AdditionalPadding),
            typeof(double),
            typeof(WidthPercentageBehavior),
            0.0,
            propertyChanged: OnPercentagePropertyChanged);

        /// <summary>
        /// The percentage of the parent's width that the associated object should occupy.
        /// </summary>
        public double Percentage
        {
            get => (double)this.GetValue(PercentageProperty);
            set => this.SetValue(PercentageProperty, value);
        }

        /// <summary>
        /// Additional padding to be added to the parent's width before calculating the percentage.
        /// This can be used to account for virtual padding for example when you offset the edges with a radius, which are not accounted for in the bounds.
        /// </summary>
        public double AdditionalPadding
        {
            get => (double)this.GetValue(AdditionalPaddingProperty);
            set => this.SetValue(AdditionalPaddingProperty, value);
        }



        private static void OnPercentagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WidthPercentageBehavior behavior)
                behavior.UpdateWidth();
        }

        private View? associatedObject;

        private void Bindable_BindingContextChanged(object? sender, EventArgs e)
        {
            base.OnBindingContextChanged();

            this.BindingContext = this.associatedObject?.BindingContext;
        }

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            this.associatedObject = bindable;
            bindable.PropertyChanged += this.OnBindablePropertyChanged;
            this.AttachToParent();

            if (bindable.BindingContext is not null)
                this.BindingContext = bindable.BindingContext;

            bindable.BindingContextChanged += this.Bindable_BindingContextChanged;
        }




        protected override void OnDetachingFrom(View bindable)
        {
            bindable.PropertyChanged -= this.OnBindablePropertyChanged;
            bindable.BindingContextChanged -= this.Bindable_BindingContextChanged;

            this.DetachFromParent();
            this.associatedObject = null;
            base.OnDetachingFrom(bindable);

        }

        private void OnBindablePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(sender is null)
                return;

            if (e.PropertyName == "Parent")
            {
                this.DetachFromParent();
                this.AttachToParent();
            }
        }

        private void AttachToParent()
        {
            if (this.associatedObject?.Parent is Layout parent)
            {
                parent.SizeChanged += this.OnParentSizeChanged;
                this.UpdateWidth();  // Update width immediately in case the parent is already sized
            }
            else if (this.associatedObject?.Parent is Border border)
            {
                border.SizeChanged += this.OnParentSizeChanged;
                this.UpdateWidth();  // Update width immediately in case the parent is already sized
            }

        }

        private void DetachFromParent()
        {
            if (this.associatedObject?.Parent is Layout parent)
            {
                parent.SizeChanged -= this.OnParentSizeChanged;
            }
            else if (this.associatedObject?.Parent is Border border)
            {
                border.SizeChanged -= this.OnParentSizeChanged;
            }
        }

        private void OnParentSizeChanged(object? sender, EventArgs e)
        {
            this.UpdateWidth();
        }

        private void UpdateWidth()
        {
            if (this.associatedObject == null || this.associatedObject.Parent == null) return;

            double parentWidth = 0;
            double horizontalPadding = 0;

            // Determine the type of the parent and extract the relevant measurements.
            if (this.associatedObject.Parent is Border border)
            {
                parentWidth = border.Bounds.Width;
                horizontalPadding = border.Padding.HorizontalThickness;
            }
            else if (this.associatedObject.Parent is Layout layout)
            {
                parentWidth = layout.Bounds.Width;
                horizontalPadding = layout.Padding.HorizontalThickness; 
            }

            // Subtract the horizontal padding from the parent's width to get the usable width.
            double usableWidth = parentWidth - horizontalPadding - this.AdditionalPadding;

            if (usableWidth > 0)
            {
				// Apply the percentage to the usable width.
				this.associatedObject.WidthRequest = usableWidth * (this.Percentage / 100);
            }
        }


    }
}
