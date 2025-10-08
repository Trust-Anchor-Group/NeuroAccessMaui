using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	[ContentProperty(nameof(Content))]
	public partial class ViewSwitcherStateView : ContentView
	{
		public static readonly BindableProperty StateKeyProperty = BindableProperty.Create(
			nameof(StateKey),
			typeof(string),
			typeof(ViewSwitcherStateView),
			default(string),
			defaultValueCreator: _ => string.Empty);

		public static readonly BindableProperty ViewTypeProperty = BindableProperty.Create(
			nameof(ViewType),
			typeof(Type),
			typeof(ViewSwitcherStateView),
			default(Type));

		public static readonly BindableProperty ViewModelTypeProperty = BindableProperty.Create(
			nameof(ViewModelType),
			typeof(Type),
			typeof(ViewSwitcherStateView),
			default(Type));

		public string StateKey
		{
			get => (string)this.GetValue(StateKeyProperty);
			set => this.SetValue(StateKeyProperty, value);
		}

		public new View? Content
		{
			get => base.Content;
			set => base.Content = value;
		}

		[TypeConverter(typeof(TypeTypeConverter))]
		public Type? ViewType
		{
			get => (Type?)this.GetValue(ViewTypeProperty);
			set => this.SetValue(ViewTypeProperty, value);
		}

		[TypeConverter(typeof(TypeTypeConverter))]
		public Type? ViewModelType
		{
			get => (Type?)this.GetValue(ViewModelTypeProperty);
			set => this.SetValue(ViewModelTypeProperty, value);
		}
	}
}
