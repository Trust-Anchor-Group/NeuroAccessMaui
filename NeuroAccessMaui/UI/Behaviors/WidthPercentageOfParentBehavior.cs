using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Behaviors
{
    public class WidthPercentageBehavior : Behavior<View>
    {
        public static readonly BindableProperty PercentageProperty = BindableProperty.Create(
            nameof(Percentage),
            typeof(double),
            typeof(WidthPercentageBehavior),
            0.0,
            propertyChanged: OnPercentagePropertyChanged);

        public static readonly BindableProperty AdditionalPaddingProperty = BindableProperty.Create(
            nameof(AdditionalPadding),
            typeof(double),
            typeof(WidthPercentageBehavior),
            0.0,
            propertyChanged: OnPercentagePropertyChanged);

        public double Percentage
        {
            get => (double)this.GetValue(PercentageProperty);
            set => this.SetValue(PercentageProperty, value);
        }

        public double AdditionalPadding
        {
            get => (double)this.GetValue(AdditionalPaddingProperty);
            set => this.SetValue(AdditionalPaddingProperty, value);
        }

        private View? associatedObject;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            associatedObject = bindable;
            // Monitor changes to the parent property
            bindable.PropertyChanged += OnBindablePropertyChanged;
        }

        protected override void OnDetachingFrom(View bindable)
        {
            bindable.PropertyChanged -= OnBindablePropertyChanged;
            DetachFromParent();
            associatedObject = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnBindablePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Parent")
            {
                // When the parent property changes, adjust the event subscription
                DetachFromParent();
                AttachToParent();
            }
        }

        private void AttachToParent()
        {
            Percentage = 100;
            if (associatedObject?.Parent is Layout parent)
            {
                parent.SizeChanged += OnParentSizeChanged;
                UpdateWidth();  // Update width immediately in case the parent is already sized
            }
            else if (associatedObject?.Parent is Border border)
            {
                border.SizeChanged += OnParentSizeChanged;
                UpdateWidth();  // Update width immediately in case the parent is already sized
            }
        }

        private void DetachFromParent()
        {
            if (associatedObject?.Parent is Layout parent)
            {
                parent.SizeChanged -= OnParentSizeChanged;
            }
        }

        private void OnParentSizeChanged(object? sender, EventArgs e)
        {
            UpdateWidth();
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

            Console.WriteLine($"Parent Width: {parentWidth}, Usable Width: {usableWidth}");
            if (usableWidth > 0)
            {
				// Apply the percentage to the usable width.
				this.associatedObject.WidthRequest = usableWidth * (this.Percentage / 100);
            }
        }

        private static void OnPercentagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Console.WriteLine("Percentage changed");
            if (bindable is WidthPercentageBehavior behavior)
            {
                behavior.UpdateWidth();
            }
        }
    }
}
