using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A custom ContentView that displays one of multiple views based on a matching case value.
	/// </summary>
	/// <typeparam name="T">The type of the switch value. Must be a non-nullable type.</typeparam>
	internal class SwitchCaseView<T> : ContentView where T : notnull
	{
		/// <summary>
		/// Identifies the <see cref="Cases"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CasesProperty = BindableProperty.Create(
			 nameof(Cases),
			 typeof(ICollection<CaseView<T>>),
			 typeof(SwitchCaseView<T>),
			 new List<CaseView<T>>(),
			 propertyChanged: SwitchChanged);

		/// <summary>
		/// Identifies the <see cref="Default"/> bindable property.
		/// </summary>
		public static readonly BindableProperty DefaultProperty = BindableProperty.Create(
			 nameof(Default),
			 typeof(View),
			 typeof(SwitchCaseView<T>),
			 null,
			 propertyChanged: SwitchChanged);

		/// <summary>
		/// Identifies the <see cref="Switch"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SwitchProperty = BindableProperty.Create(
			 nameof(Switch),
			 typeof(T),
			 typeof(SwitchCaseView<T>),
			 default(T),
			 propertyChanged: SwitchChanged);

		/// <summary>
		/// Called when any of the switch-related properties change.
		/// Updates the displayed content based on the current switch value.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The previous value of the property.</param>
		/// <param name="newValue">The new value of the property.</param>
		private static void SwitchChanged(BindableObject bindable, object oldValue, object newValue)
		{
			SwitchCaseView<T> SwitchCaseView = (SwitchCaseView<T>)bindable;
			// Attempt to find a matching case. If none is found, use the default view.
			SwitchCaseView.Content = SwitchCaseView.Cases
				 .Where(x => x.Case.Equals(SwitchCaseView.Switch))
				 .Select(x => x.Content)
				 .SingleOrDefault() ?? SwitchCaseView.Default;
		}

		/// <summary>
		/// Gets or sets the switch value that is used to determine which case to display.
		/// </summary>
		public T Switch
		{
			get => (T)this.GetValue(SwitchProperty);
			set => this.SetValue(SwitchProperty, value);
		}

		/// <summary>
		/// Gets or sets the default view to display when no matching case is found.
		/// </summary>
		public View Default
		{
			get => (View)this.GetValue(DefaultProperty);
			set => this.SetValue(DefaultProperty, value);
		}

		/// <summary>
		/// Gets or sets the collection of case views.
		/// </summary>
		public ICollection<CaseView<T>> Cases
		{
			get => (ICollection<CaseView<T>>)this.GetValue(CasesProperty);
			set => this.SetValue(CasesProperty, value);
		}
	}

	/// <summary>
	/// A custom ContentView representing a single case with a specific value.
	/// </summary>
	/// <typeparam name="T">The type of the case value.</typeparam>
	internal class CaseView<T> : ContentView
	{
		/// <summary>
		/// Identifies the <see cref="Case"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CaseProperty = BindableProperty.Create(
			 nameof(Case),
			 typeof(T),
			 typeof(CaseView<T>),
			 default(T));

		/// <summary>
		/// Gets or sets the case value for this view.
		/// </summary>
		public T Case
		{
			get => (T)this.GetValue(CaseProperty);
			set => this.SetValue(CaseProperty, value);
		}
	}
}
