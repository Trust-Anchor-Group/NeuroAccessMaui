namespace NeuroAccessMaui.Helpers
{
	/// <summary>
	/// Provides an attached State property for any BindableObject,
	/// usable in DataTriggers in place of VisualState.
	/// Controls or ViewModels can set this property,
	/// or controls can update it internally to reflect state.
	/// </summary>
	public static class StateProperties
	{
		/// <summary>
		/// Attached State property (e.g. string or enum).
		/// </summary>
		public static readonly BindableProperty StateProperty =
			BindableProperty.CreateAttached(
				propertyName: "State",
				returnType: typeof(object),
				declaringType: typeof(StateProperties),
				defaultValue: null,
				propertyChanged: OnStateChanged);

		/// <summary>
		/// Gets the State attached property.
		/// </summary>
		public static object? GetState(BindableObject View)
			=> View.GetValue(StateProperty);

		/// <summary>
		/// Sets the State attached property.
		/// When set, optionally you could call VisualStateManager.GoToState(view, newValue.ToString()).
		/// </summary>
		public static void SetState(BindableObject View, object? Value)
			=> View.SetValue(StateProperty, Value);

		static void OnStateChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			Console.WriteLine($"New state for {Bindable.GetType().Name} : Value: {NewValue}");
			// If you want controls to transition VisualState, uncomment below:
			// if (bindable is VisualElement ve && newValue != null)
			//     VisualStateManager.GoToState(ve, newValue.ToString());

			// Else, this attached property solely reflects state for DataTriggers.
		}
	}
}
