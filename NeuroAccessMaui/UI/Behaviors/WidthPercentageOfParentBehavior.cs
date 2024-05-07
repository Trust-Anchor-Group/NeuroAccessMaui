using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Behaviors
{
    public class WidthPercentageBehavior : Behavior<View>
    {
        public static BindableProperty PercentageProperty = BindableProperty.Create(
            nameof(Percentage),
            typeof(double),
            typeof(WidthPercentageBehavior),
            0.0,
            propertyChanged: OnPercentagePropertyChanged);

        public double Percentage
        {
            get => (double)this.GetValue(PercentageProperty);
            set => this.SetValue(PercentageProperty, value);
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
            if (associatedObject == null || associatedObject.Parent == null) return;

            double parentWidth = 0;
            if (associatedObject.Parent is Border border)
            {
                parentWidth = border.Width - border.Padding.HorizontalThickness;
            }
            else if (associatedObject.Parent is Layout layout)
            {
                parentWidth = layout.Width;
            }

            if (parentWidth > 0)
            {
                associatedObject.WidthRequest = parentWidth * Percentage / 100;
            }
        }

        private static void OnPercentagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WidthPercentageBehavior behavior)
            {
                behavior.UpdateWidth();
            }
        }
    }
}
