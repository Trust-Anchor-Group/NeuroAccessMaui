using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
    /// <summary>
    /// A custom ContentView that displays one of two views based on a boolean condition.
    /// </summary>
    internal class ConditionView : ContentView
    {
        /// <summary>
        /// Identifies the <see cref="False"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FalseProperty = BindableProperty.Create(nameof(False), typeof(View), typeof(ConditionView), null);

        /// <summary>
        /// Identifies the <see cref="True"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TrueProperty = BindableProperty.Create(nameof(True), typeof(View), typeof(ConditionView), null);

        /// <summary>
        /// Identifies the <see cref="Condition"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ConditionProperty = BindableProperty.Create(nameof(Condition), typeof(bool), typeof(ConditionView), false, propertyChanged: ConditionChanged);

        /// <summary>
        /// Called when the <see cref="Condition"/> property changes.
        /// </summary>
        /// <param name="Bindable">The bindable object.</param>
        /// <param name="OldValue">The old value of the property.</param>
        /// <param name="NewValue">The new value of the property.</param>
        private static void ConditionChanged(BindableObject Bindable, object OldValue, object NewValue)
        {
            ConditionView ConditionView = (ConditionView)Bindable;
            ConditionView.Content = (bool)NewValue ? ConditionView.True : ConditionView.False;
        }

        /// <summary>
        /// Gets or sets the condition that determines which view to display.
        /// </summary>
        public bool Condition
        {
            get => (bool)this.GetValue(ConditionProperty);
            set => this.SetValue(ConditionProperty, value);
        }

        /// <summary>
        /// Gets or sets the view to display when <see cref="Condition"/> is true.
        /// </summary>
        public View True
        {
            get => (View)this.GetValue(TrueProperty);
            set => this.SetValue(TrueProperty, value);
        }

        /// <summary>
        /// Gets or sets the view to display when <see cref="Condition"/> is false.
        /// </summary>
        public View False
        {
            get => (View)this.GetValue(FalseProperty);
            set => this.SetValue(FalseProperty, value);
        }
    }
}
