namespace NeuroAccessMaui.UI.Behaviors
{
	/// <summary>
	/// SettableIsFocusedBehavior synchronizes the value of <see cref="IsFocusedProperty"/> with the value of <see cref="VisualElement.IsFocusedProperty"/>
	/// in both directions.
	/// <para>
	/// Unlike <see cref="VisualElement.IsFocusedProperty"/>, which is read-only, <see cref="IsFocusedProperty"/> is writable
	/// and supports <see cref="BindingMode.TwoWay"/> data binding, which allows setting focus from a view model using properties
	/// and avoiding using callbacks from a view model.
	/// </para>
	/// </summary>
	public class SettableIsFocusedBehavior : Behavior<VisualElement>
	{
		/// <summary>
		/// Implements the attached bindable property which allows both reading and setting the focus state of a <see cref="VisualElement"/>.
		/// </summary>
		public static readonly BindableProperty IsFocusedProperty = BindableProperty.CreateAttached("IsFocused", typeof(bool), typeof(SettableIsFocusedBehavior),
			defaultValue: false,
			defaultBindingMode: BindingMode.TwoWay,
			propertyChanged: (Bindable, _, NewValue) => OnSettableIsFocusedChanged(Bindable, (bool)NewValue));

		/// <summary>
		/// Gets the value indicating if the element is focused.
		/// </summary>
		public static bool GetIsFocused(BindableObject Bindable)
		{
			return (bool)Bindable.GetValue(IsFocusedProperty);
		}

		/// <summary>
		/// Tries to set focus on the element if <paramref name="Value"/> is <c>true</c> or clears focus if <paramref name="Value"/>
		/// is <c>false</c>. Setting focus might fail, in which case <see cref="IsFocusedProperty"/> will be reset to <c> false</c>.
		/// </summary>
		public static void SetIsFocused(BindableObject? Bindable, bool Value)
		{
			Bindable?.SetValue(IsFocusedProperty, Value);
		}

		private static void OnSettableIsFocusedChanged(BindableObject Bindable, bool IsFocused)
		{
			if (Bindable is VisualElement VisualElement)
			{
				if (IsFocused && !VisualElement.IsFocused)
				{
					SetIsFocused(VisualElement, VisualElement.Focus());
				}

				if (!IsFocused && VisualElement.IsFocused)
				{
					VisualElement.Unfocus();
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnAttachedTo(VisualElement VisualElement)
		{
			base.OnAttachedTo(VisualElement);
			VisualElement.Focused += this.OnElementFocused;
			VisualElement.Unfocused += this.OnElementUnfocused;
			SetIsFocused(VisualElement, VisualElement.IsFocused);
		}

		/// <inheritdoc/>
		protected override void OnDetachingFrom(VisualElement VisualElement)
		{
			VisualElement.Unfocused -= this.OnElementUnfocused;
			VisualElement.Focused -= this.OnElementFocused;
			VisualElement.ClearValue(IsFocusedProperty);
			base.OnDetachingFrom(VisualElement);
		}

		private void OnElementFocused(object? Sender, FocusEventArgs EventArgs)
		{
			SetIsFocused((BindableObject?)Sender, EventArgs.IsFocused);
		}

		private void OnElementUnfocused(object? Sender, FocusEventArgs EventArgs)
		{
			SetIsFocused((BindableObject?)Sender, EventArgs.IsFocused);
		}
	}
}
