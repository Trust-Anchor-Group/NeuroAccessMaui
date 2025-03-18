using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
	internal class ConditionView : ContentView
	{
		public static readonly BindableProperty FalseProperty =
			BindableProperty.Create(nameof(False), typeof(View), typeof(ConditionView), null, propertyChanged: OnViewChanged);

		public static readonly BindableProperty TrueProperty =
			BindableProperty.Create(nameof(True), typeof(View), typeof(ConditionView), null, propertyChanged: OnViewChanged);

		public static readonly BindableProperty ConditionProperty =
			BindableProperty.Create(nameof(Condition), typeof(bool), typeof(ConditionView), false, propertyChanged: OnConditionChanged);

		private static void OnConditionChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((ConditionView)bindable).UpdateContent();
		}

		private static void OnViewChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((ConditionView)bindable).UpdateContent();
		}

		private void UpdateContent()
		{
			this.Content = this.Condition ? this.True : this.False;
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			if (True != null)
				True.BindingContext = BindingContext;
			if (False != null)
				False.BindingContext = BindingContext;
		}

		public bool Condition
		{
			get => (bool)this.GetValue(ConditionProperty);
			set => this.SetValue(ConditionProperty, value);
		}

		public View True
		{
			get => (View)this.GetValue(TrueProperty);
			set => this.SetValue(TrueProperty, value);
		}

		public View False
		{
			get => (View)this.GetValue(FalseProperty);
			set => this.SetValue(FalseProperty, value);
		}
	}

}
