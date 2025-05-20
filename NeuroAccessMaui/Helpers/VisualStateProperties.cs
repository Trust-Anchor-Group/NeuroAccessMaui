using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Helpers
{

	/// <summary>
	/// Creates Attached property in order to set the state of a <see cref="VisualElement"/> base on a binded string or enum.
	/// </summary>
	public static class VisualStateProperties
	{
		public static readonly BindableProperty StateProperty =
			BindableProperty.CreateAttached(
				"State",
				typeof(object),
				typeof(VisualStateProperties),
				default,
				propertyChanged: OnStateChanged);

		public static object GetState(BindableObject view)
			=> view.GetValue(StateProperty);

		public static void SetState(BindableObject view, object? value)
			=> view.SetValue(StateProperty, value);

		static void OnStateChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is VisualElement Element && newValue is not null)
			{
				VisualStateManager.GoToState(Element, newValue.ToString());
			}
		}
	}
}
